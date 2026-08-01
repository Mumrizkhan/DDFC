using DDFC.Application.Interfaces;
using DDFC.Domain.Entities;
using DDFC.Domain.Enums;
using DDFC.Infrastructure.Data;
using DDFC.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Application.Interfaces;

namespace DDFC.Infrastructure.Services;

public class PossessionRequestService : IPossessionRequestService
{
    private readonly DDFCDbContext _db;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly ILogger<PossessionRequestService> _logger;

    private static Guid _ddFCProcessId = Guid.Empty;
    private static Guid _revisedPlanProcessId = Guid.Empty;
    private static Guid _asBuiltPlanProcessId = Guid.Empty;

    public static void SetDDFCProcessId(Guid id)         => _ddFCProcessId         = id;
    public static void SetRevisedPlanProcessId(Guid id)  => _revisedPlanProcessId  = id;
    public static void SetAsBuiltPlanProcessId(Guid id)  => _asBuiltPlanProcessId  = id;

    public PossessionRequestService(
        DDFCDbContext db,
        IWorkflowEngine workflowEngine,
        ILogger<PossessionRequestService> logger)
    {
        _db = db;
        _workflowEngine = workflowEngine;
        _logger = logger;
    }

    // -----------------------------------------------------------------------
    // Helper: generate Request ID with prefix based on workflow type
    //   PossessionDesign → DDFC-YYYY-NNNNN
    //   RevisedPlan      → RPLAN-YYYY-NNNNN
    //   AsBuiltPlan      → ASBUILT-YYYY-NNNNN
    // -----------------------------------------------------------------------
    private async Task<string> GenerateRequestIdAsync(RequestType requestType = RequestType.PossessionDesign)
    {
        var year   = DateTime.UtcNow.Year;
        var prefix = requestType switch
        {
            RequestType.RevisedPlan => "RPLAN",
            RequestType.AsBuiltPlan => "ASBUILT",
            _                       => "DDFC",
        };
        var count = await _db.PossessionRequests
            .CountAsync(r => r.RequestType == requestType && r.CreatedAt.Year == year);
        return $"{prefix}-{year}-{(count + 1):D5}";
    }

    // -----------------------------------------------------------------------
    // Helper: get or resolve DDFC process ID
    // -----------------------------------------------------------------------
    private async Task<Guid> GetDDFCProcessIdAsync()
    {
        if (_ddFCProcessId != Guid.Empty) return _ddFCProcessId;

        // The WorkflowEngine uses its own DbContext (same connection in production,
        // InMemory in tests). Read from the WE DbContext via the workflow engine repo.
        // Since IWorkflowRepository is not directly accessible here we use the seeder name.
        // In the DI setup both DbContexts point to the same database.
        // We'll resolve via a direct EF query on the WE DbContext passed at startup.
        throw new InvalidOperationException(
            "DDFC Process ID not set. Call SetDDFCProcessId() on startup after seeding.");
    }

    // -----------------------------------------------------------------------
    // Create
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> CreateRequestAsync(CreateRequestDto dto, Guid? createdByUserId = null)
    {
        var requestId = await GenerateRequestIdAsync(dto.RequestType);

        var req = new PossessionRequest
        {
            RequestId = requestId,
            CustomerId = dto.CustomerId,
            PlotId = dto.PlotId,
            FileNo = dto.FileNo,
            MembershipDPRNo = dto.MembershipDPRNo,
            OwnerTitle = dto.OwnerTitle,
            OwnerName = dto.OwnerName,
            SonDaughterWifeOf = dto.SonDaughterWifeOf,
            GuardianRelation = dto.GuardianRelation,
            AuthorizedPersonName = dto.AuthorizedPersonName,
            Contractor = dto.Contractor,
            RequestType = dto.RequestType,
            Status = PossessionRequestStatus.Submitted
        };

        _db.PossessionRequests.Add(req);
        await _db.SaveChangesAsync();

        // Start WorkflowEngine request using the correct process for this workflow type
        var processId = dto.RequestType switch
        {
            RequestType.RevisedPlan => _revisedPlanProcessId,
            RequestType.AsBuiltPlan => _asBuiltPlanProcessId,
            _                       => _ddFCProcessId,
        };
        var weRequest = await _workflowEngine.StartRequestAsync(processId);
        req.WorkflowRequestId = weRequest.Id;
        await _db.SaveChangesAsync();

        // Auto-select package from linked possession request (Revised/AsBuilt workflows)
        if (dto.LinkedPossessionRequestId.HasValue &&
            (dto.RequestType == RequestType.RevisedPlan || dto.RequestType == RequestType.AsBuiltPlan))
        {
            var linkedReq = await _db.PossessionRequests
                .Include(r => r.SelectedPackage)
                .Include(r => r.Plot)
                .FirstOrDefaultAsync(r => r.Id == dto.LinkedPossessionRequestId.Value);

            if (linkedReq?.SelectedPackage != null)
            {
                var targetCategory = dto.RequestType == RequestType.RevisedPlan
                    ? PackageCategory.RevisedPlan
                    : PackageCategory.AsBuiltPlan;

                // Match by plot size and design type from the linked possession package
                var matchedPkg = await _db.Packages
                    .FirstOrDefaultAsync(p =>
                        p.PackageCategory == targetCategory &&
                        p.PlotSize        == linkedReq.SelectedPackage.PlotSize &&
                        p.DesignType      == linkedReq.SelectedPackage.DesignType &&
                        p.IsActive);

                if (matchedPkg != null)
                    req.SelectedPackageId = matchedPkg.Id;
            }
        }

        await _db.SaveChangesAsync();

        await LogHistoryAsync(req.Id, null, "Submitted", "CreateRequest", createdByUserId, null);

        _logger.LogInformation("Created PossessionRequest {RequestId} → WE {WeId}", requestId, weRequest.Id);
        return req;
    }

    // -----------------------------------------------------------------------
    // Initiate: completes WE Step 1 and formally starts the process
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> InitiateRequestAsync(Guid requestId, Guid userId, string? comments)
    {
        var req = await GetRequestAsync(requestId);
        if (req.Status != PossessionRequestStatus.Submitted)
            throw new InvalidOperationException("Only Submitted requests can be initiated.");

        // Complete WE Step 1 — this activates the parallel Transfer + Finance branches
        await AdvanceWorkflowStepAsync(req,
            "Reception – Submit NOC/NDC Request",
            "Create Request (Form 1)",
            userId.ToString(), null);

        req.Status = PossessionRequestStatus.Initiated;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, "Submitted", "Initiated", "InitiateRequest", userId, comments);
        return req;
    }

    // -----------------------------------------------------------------------
    // Query helpers
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> GetRequestAsync(Guid requestId) =>
        await _db.PossessionRequests
            .Include(r => r.Customer)
            .Include(r => r.Plot)
            .Include(r => r.SelectedPackage).ThenInclude(p => p!.LineItems)
            .Include(r => r.SelectedInteriorDesignPackage).ThenInclude(p => p!.LineItems)
            .Include(r => r.SelectedSupervisionPackage).ThenInclude(p => p!.LineItems)
            .Include(r => r.ArchitecturalPlans).ThenInclude(p => p.RevisionRequests)
            .Include(r => r.ThreeDVisualizations)
            .Include(r => r.CadFiles)
            .Include(r => r.StructuralReports)
            .Include(r => r.MEPReports)
            .Include(r => r.SurveyObservations)
            .Include(r => r.SoilTestReport)
            .Include(r => r.PossessionCertificate)
            .Include(r => r.DelayUndertaking)
            .Include(r => r.Payments)
            .Include(r => r.WorkflowHistory)
            .FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new InvalidOperationException("Request not found");

    public async Task<PossessionRequest> GetRequestByStringIdAsync(string requestId) =>
        await _db.PossessionRequests
            .Include(r => r.Customer).Include(r => r.Plot)
            .FirstOrDefaultAsync(r => r.RequestId == requestId)
            ?? throw new InvalidOperationException("Request not found");

    public async Task<IEnumerable<PossessionRequest>> GetRequestsByCustomerAsync(Guid customerId) =>
        await _db.PossessionRequests
            .Include(r => r.Plot)
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    // -----------------------------------------------------------------------
    // Workflow step: Transfer Branch approval
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> ApproveTransferAsync(Guid requestId, Guid userId, string? comments)
    {
        var req = await GetRequestAsync(requestId);
        req.TransferApproved = true;

        var newStatus = req.FinanceApproved
            ? PossessionRequestStatus.BothBranchesCleared   // Building Control will issue cert
            : PossessionRequestStatus.TransferApproved;

        await AdvanceWorkflowStepAsync(req, "Transfer Branch – NOC/NDC Review",
            "Approve Transfer", userId.ToString(), $"{{\"approval\":\"approved\"}}");

        req.Status = newStatus;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, req.Status.ToString(), newStatus.ToString(),
            "ApproveTransfer", userId, comments);

        return req;
    }

    // -----------------------------------------------------------------------
    // Workflow step: Transfer Branch rejection
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> RejectTransferAsync(Guid requestId, Guid userId, string? comments)
    {
        var req = await GetRequestAsync(requestId);
        var fromStatus = req.Status.ToString();
        req.Status = PossessionRequestStatus.Rejected;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, fromStatus, "Rejected", "TransferReject", userId, comments);
        return req;
    }

    // -----------------------------------------------------------------------
    // Workflow step: Transfer Branch requests clarification (sends back to Reception)
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> RequestTransferClarificationAsync(Guid requestId, Guid userId, string? comments)
    {
        var req = await GetRequestAsync(requestId);
        var fromStatus = req.Status.ToString();
        req.Status = PossessionRequestStatus.Submitted;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, fromStatus, "Submitted", "TransferClarificationRequested", userId, comments);
        return req;
    }

    // -----------------------------------------------------------------------
    // Workflow step: Finance Branch approval
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> ApproveFinanceAsync(Guid requestId, Guid userId, string? comments, decimal? adcAmount = null)
    {
        var req = await GetRequestAsync(requestId);
        req.FinanceApproved = true;
        if (adcAmount.HasValue)
            req.AdcAmount = adcAmount;

        var newStatus = req.TransferApproved
            ? PossessionRequestStatus.BothBranchesCleared
            : PossessionRequestStatus.FinanceApproved;

        await AdvanceWorkflowStepAsync(req, "Finance Branch – Dues Clearance",
            "Approve Finance", userId.ToString(), $"{{\"approval\":\"approved\"}}");

        req.Status = newStatus;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, req.Status.ToString(), newStatus.ToString(),
            "ApproveFinance", userId, comments);

        return req;
    }

    // -----------------------------------------------------------------------
    // Workflow step: Finance Branch rejection
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> RejectFinanceAsync(Guid requestId, Guid userId, string? comments)
    {
        var req = await GetRequestAsync(requestId);
        var fromStatus = req.Status.ToString();

        await AdvanceWorkflowStepAsync(req, "Finance Branch – Dues Clearance",
            "Reject Finance", userId.ToString(), null);

        req.Status = PossessionRequestStatus.Rejected;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, fromStatus, "Rejected", "FinanceReject", userId, comments);
        return req;
    }

    // -----------------------------------------------------------------------
    // DDFC Admin: sign the possession letter (creates the cert record)
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> DdfcAdminSignAsync(Guid requestId, Guid userId, IssueCertDto dto)
    {
        var req = await GetRequestAsync(requestId);

        if (!req.TransferApproved || !req.FinanceApproved)
            throw new InvalidOperationException("Both Transfer and Finance must be approved first.");

        // Create possession certificate if not already created
        if (req.PossessionCertificate == null)
        {
            var cert = new PossessionCertificate
            {
                RequestId         = req.Id,
                GeneratedAt       = DateTime.UtcNow,
                ValidUntil        = DateTime.UtcNow.AddMonths(6),
                HandedOverBy      = dto.HandedOverBy,
                HandedOverDate    = dto.HandedOverDate,
                TakenOverBy       = dto.TakenOverBy,
                TakenOverDate     = dto.TakenOverDate,
                ChiefSurveyorName = dto.ChiefSurveyorName,
                AdTpBcdName       = dto.AdTpBcdName,
            };
            _db.PossessionCertificates.Add(cert);
        }

        var fromStatus = req.Status.ToString();

        await AdvanceWorkflowStepAsync(req,
            "DDFC Admin \u2013 Sign Possession Letter",
            "Sign Possession Letter", userId.ToString(), null);

        req.Status = PossessionRequestStatus.PossessionLetterSigned;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, fromStatus, "PossessionLetterSigned",
            "DdfcAdminSign", userId, null);
        return req;
    }

    // -----------------------------------------------------------------------
    // Delay Undertaking: DDFC requests customer to sign
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> RequestDelayUndertakingAsync(
        Guid requestId, Guid userId, string? delayReason, int? expectedDelayDays, string? notes)
    {
        var req = await GetRequestAsync(requestId);
        if (req.Status != PossessionRequestStatus.PossessionLetterSigned)
            throw new InvalidOperationException(
                "Delay undertaking can only be requested after the possession letter is signed.");

        if (req.DelayUndertaking != null)
            throw new InvalidOperationException("A delay undertaking already exists for this request.");

        _db.DelayUndertakings.Add(new Domain.Entities.DelayUndertaking
        {
            RequestId         = req.Id,
            InitiatedBy       = Domain.Entities.DelayUndertakingInitiator.DDFC,
            RequestedByUserId = userId,
            RequestedAt       = DateTime.UtcNow,
            DelayReason       = delayReason,
            ExpectedDelayDays = expectedDelayDays,
            Notes             = notes,
        });

        req.Status = PossessionRequestStatus.DelayUndertakingRequested;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, "PossessionLetterSigned", "DelayUndertakingRequested",
            "RequestDelayUndertaking", userId, delayReason);
        return req;
    }

    // -----------------------------------------------------------------------
    // Delay Undertaking: customer signs (completes WE step)
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> SignDelayUndertakingAsync(
        Guid requestId, Guid customerId, SignDelayUndertakingDto dto)
    {
        var req = await GetRequestAsync(requestId);

        var validStatuses = new[]
        {
            PossessionRequestStatus.PossessionLetterSigned,
            PossessionRequestStatus.DelayUndertakingRequested,
        };
        if (!validStatuses.Contains(req.Status))
            throw new InvalidOperationException(
                "Delay undertaking can only be signed after the possession letter is signed.");

        // If no undertaking record yet, create a customer-initiated one
        if (req.DelayUndertaking == null)
        {
            _db.DelayUndertakings.Add(new Domain.Entities.DelayUndertaking
            {
                RequestId   = req.Id,
                InitiatedBy = Domain.Entities.DelayUndertakingInitiator.Customer,
            });
            await _db.SaveChangesAsync();
            // Reload to get the navigation property
            req = await GetRequestAsync(requestId);
        }

        req.DelayUndertaking!.SignedByCustomerId    = customerId;
        req.DelayUndertaking.SignedAt               = DateTime.UtcNow;
        req.DelayUndertaking.UndertakingDocumentUrl = dto.UndertakingDocumentUrl;
        if (!string.IsNullOrWhiteSpace(dto.Notes))
            req.DelayUndertaking.Notes = dto.Notes;

        // Pick the correct WE action name based on how it was initiated
        var actionName = req.DelayUndertaking.InitiatedBy == Domain.Entities.DelayUndertakingInitiator.DDFC
            ? "DDFC-Requested: Customer Signs Delay Undertaking"
            : "Customer Signs Delay Undertaking";

        await AdvanceWorkflowStepAsync(req,
            "Customer \u2013 Delay Undertaking Signing",
            actionName, customerId.ToString(), null);

        req.Status = PossessionRequestStatus.DelayUndertakingSigned;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, req.Status.ToString(), "DelayUndertakingSigned",
            "SignDelayUndertaking", customerId, dto.Notes);
        return req;
    }

    // -----------------------------------------------------------------------
    // Delay Undertaking: DDFC staff skip the step entirely
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> SkipDelayUndertakingAsync(Guid requestId, Guid userId)
    {
        var req = await GetRequestAsync(requestId);

        var validStatuses = new[]
        {
            PossessionRequestStatus.PossessionLetterSigned,
            PossessionRequestStatus.DelayUndertakingRequested,
        };
        if (!validStatuses.Contains(req.Status))
            throw new InvalidOperationException(
                "Delay undertaking can only be skipped after the possession letter is signed.");

        await AdvanceWorkflowStepAsync(req,
            "Customer \u2013 Delay Undertaking Signing",
            "Skip Delay Undertaking", userId.ToString(), null);

        req.Status = PossessionRequestStatus.DelayUndertakingSigned;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, req.Status.ToString(), "DelayUndertakingSigned",
            "SkipDelayUndertaking", userId, "Step skipped by DDFC staff");
        return req;
    }

    // -----------------------------------------------------------------------
    // Get issued certificate (for preview)
    // -----------------------------------------------------------------------
    public async Task<PossessionCertificate?> GetPossessionCertificateAsync(Guid requestId)
        => await _db.PossessionCertificates
            .FirstOrDefaultAsync(c => c.RequestId == requestId);

    // -----------------------------------------------------------------------
    // Package selection
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> SelectPackageAsync(Guid requestId, Guid packageId, Guid? interiorDesignPackageId, Guid? supervisionPackageId, Guid userId)
    {
        var req = await GetRequestAsync(requestId);
        req.SelectedPackageId = packageId;
        req.SelectedInteriorDesignPackageId = interiorDesignPackageId;
        req.SelectedSupervisionPackageId    = supervisionPackageId;
        req.Status = PossessionRequestStatus.PackageSelected;

        await AdvanceWorkflowStepAsync(req, "Reception – Package Selection",
            "Select Design Package", userId.ToString(), null);

        // Build combined payment total across all selected packages
        var totalAmount = 0m;
        var challanNo = $"CHN-{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var pkgIds = new[] { packageId }
            .Concat(interiorDesignPackageId.HasValue ? new[] { interiorDesignPackageId.Value } : Array.Empty<Guid>())
            .Concat(supervisionPackageId.HasValue    ? new[] { supervisionPackageId.Value }    : Array.Empty<Guid>())
            .Distinct();

        foreach (var pid in pkgIds)
        {
            var p = await _db.Packages.Include(p => p.LineItems).FirstOrDefaultAsync(p => p.Id == pid);
            if (p != null) totalAmount += p.LineItems.Where(li => !li.IsFree).Sum(li => li.AmountDDFC);
        }

        _db.Payments.Add(new Payment
        {
            RequestId   = req.Id,
            ChallanNo   = challanNo,
            TotalAmount = totalAmount,
            Status      = PaymentStatus.Pending,
        });

        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, "PossessionIssued", "PackageSelected",
            "SelectPackage", userId, null);
        return req;
    }

    // -----------------------------------------------------------------------
    // Payment confirmation
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> ConfirmPaymentAsync(Guid requestId, Guid userId, decimal amountPaid, string? challanNo = null, string? scannedFileUrl = null)
    {
        var req = await GetRequestAsync(requestId);

        var payment = await _db.Payments
            .Include(p => p.Challan)
            .FirstOrDefaultAsync(p => p.RequestId == req.Id && p.Status == PaymentStatus.Pending);

        if (payment != null)
        {
            payment.PaidAmount = amountPaid;
            payment.Status = PaymentStatus.Paid;
            payment.PaidAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(challanNo))
                payment.ChallanNo = challanNo;
            // Store scanned paid challan file URL
            if (!string.IsNullOrWhiteSpace(scannedFileUrl))
            {
                if (payment.Challan != null)
                    payment.Challan.FileUrl = scannedFileUrl;
                else
                    _db.PaymentChallans.Add(new PaymentChallan
                    {
                        PaymentId = payment.Id,
                        ChallanNumber = payment.ChallanNo,
                        FileUrl = scannedFileUrl,
                    });
            }
        }

        req.Status = PossessionRequestStatus.PackagePaid;

        await AdvanceWorkflowStepAsync(req, "Finance Branch – Payment Confirmation",
            "Confirm Payment", userId.ToString(), null);

        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, "PackageSelected", "PackagePaid",
            "ConfirmPayment", userId, null);
        return req;
    }

    // -----------------------------------------------------------------------
    // Architecture – upload plan
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> UploadPlanAsync(Guid requestId, string fileUrl, Guid userId, string? notes)
    {
        var req = await GetRequestAsync(requestId);

        var version = req.ArchitecturalPlans.Count + 1;
        var plan = new ArchitecturalPlan
        {
            RequestId = req.Id,
            Version = version,
            FileUrl = fileUrl,
            UploadedBy = userId,
            Notes = notes
        };
        _db.ArchitecturalPlans.Add(plan);
        await _db.SaveChangesAsync();
        return req;
    }

    // -----------------------------------------------------------------------
    // Architecture – customer approves plan
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> ApprovePlanAsync(Guid requestId, Guid planId, Guid customerId)
    {
        var req = await GetRequestAsync(requestId);
        var plan = req.ArchitecturalPlans.FirstOrDefault(p => p.Id == planId)
            ?? throw new InvalidOperationException("Plan not found");

        plan.CustomerApproved = true;
        plan.ApprovedAt = DateTime.UtcNow;
        req.CustomerApprovedPlan = true;
        req.Status = PossessionRequestStatus.ArchitectureApproved;

        await AdvanceWorkflowStepAsync(req,
            "Architecture Department – House Plan Design",
            "Customer Approves Plan", customerId.ToString(), $"{{\"approved\":true}}");

        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, "ArchitectAssigned", "ArchitectureApproved",
            "ApprovePlan", customerId, null);
        return req;
    }

    // -----------------------------------------------------------------------
    // Principal Architect – initial review: upload soil test + assign architect
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> PAInitialReviewAsync(
        Guid requestId, Guid assignedArchitectId, SoilTestDto? soilTest, string? notes, Guid userId)
    {
        var req = await GetRequestAsync(requestId);

        if (req.Status != PossessionRequestStatus.PackagePaid)
            throw new InvalidOperationException("Request must be in PackagePaid status.");

        // Assign architect
        req.AssignedArchitectId = assignedArchitectId;

        // Optionally attach soil test
        if (soilTest != null)
        {
            if (req.SoilTestReport != null)
            {
                req.SoilTestReport.ReportFileUrl   = soilTest.ReportFileUrl;
                req.SoilTestReport.TestDate        = soilTest.TestDate;
                req.SoilTestReport.LabName         = soilTest.LabName;
                req.SoilTestReport.SoilBearingCapacity = soilTest.SoilBearingCapacity;
                req.SoilTestReport.ResultSummary   = soilTest.ResultSummary;
            }
            else
            {
                _db.SoilTestReports.Add(new SoilTestReport
                {
                    RequestId            = req.Id,
                    TestDate             = soilTest.TestDate,
                    LabName              = soilTest.LabName,
                    SoilBearingCapacity  = soilTest.SoilBearingCapacity,
                    ResultSummary        = soilTest.ResultSummary,
                    ReportFileUrl        = soilTest.ReportFileUrl,
                    UploadedBy           = userId,
                });
                req.SoilTestReportUrl = soilTest.ReportFileUrl;
            }
        }

        await AdvanceWorkflowStepAsync(req,
            "Principal Architect \u2013 Initial Review",
            "Assign Architect", userId.ToString(), null);

        req.Status = PossessionRequestStatus.ArchitectAssigned;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, "PackagePaid", "ArchitectAssigned",
            "PAInitialReview", userId, notes);
        return req;
    }

    // -----------------------------------------------------------------------
    // Architecture – customer requests revision
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> RequestPlanRevisionAsync(
        Guid requestId, Guid planId, Guid customerId, string comments, string? markupUrl)
    {
        var req = await GetRequestAsync(requestId);
        var plan = req.ArchitecturalPlans.FirstOrDefault(p => p.Id == planId)
            ?? throw new InvalidOperationException("Plan not found");

        _db.PlanRevisionRequests.Add(new PlanRevisionRequest
        {
            PlanId = plan.Id,
            RequestedBy = customerId,
            Comments = comments,
            MarkupFileUrl = markupUrl
        });
        await _db.SaveChangesAsync();
        return req;
    }

    // -----------------------------------------------------------------------
    // 3D Visualisation – upload a file (.skp / .jpeg / .jpg / .png)
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> UploadThreeDFileAsync(Guid requestId, ThreeDFileDto dto, Guid userId)
    {
        var req = await GetRequestAsync(requestId);
        _db.ThreeDVisualizations.Add(new Domain.Entities.ThreeDVisualization
        {
            RequestId  = requestId,
            FileUrl    = dto.FileUrl,
            FileName   = dto.FileName,
            FileType   = dto.FileType,
            UploadedBy = userId,
            Notes      = dto.Notes,
        });
        await _db.SaveChangesAsync();
        return req;
    }

    // -----------------------------------------------------------------------
    // 3D Visualisation – mark step complete
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> CompleteThreeDAsync(Guid requestId, Guid userId)
    {
        var req = await GetRequestAsync(requestId);
        if (req.ThreeDVisualizations.Count == 0)
            throw new InvalidOperationException("At least one 3D file must be uploaded before completing this step.");

        await AdvanceWorkflowStepAsync(req,
            "Architecture Department \u2013 3D Visualization",
            "Upload 3D Visualization Files", userId.ToString(), null);

        var fromStatus = req.Status.ToString();
        req.Status = PossessionRequestStatus.ThreeDCompleted;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, fromStatus, "ThreeDCompleted", "CompleteThreeD", userId, null);
        return req;
    }

    // -----------------------------------------------------------------------
    // CAD Files – upload a file (.dwg / .pdf)
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> UploadCadFileAsync(Guid requestId, CadFileDto dto, Guid userId)
    {
        var req = await GetRequestAsync(requestId);
        _db.CadFiles.Add(new Domain.Entities.CadFile
        {
            RequestId  = requestId,
            FileUrl    = dto.FileUrl,
            FileName   = dto.FileName,
            FileType   = dto.FileType,
            UploadedBy = userId,
            Notes      = dto.Notes,
        });
        await _db.SaveChangesAsync();
        return req;
    }

    // -----------------------------------------------------------------------
    // CAD Files – mark step complete
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> CompleteCadAsync(Guid requestId, Guid userId)
    {
        var req = await GetRequestAsync(requestId);
        if (req.CadFiles.Count == 0)
            throw new InvalidOperationException("At least one CAD file must be uploaded before completing this step.");

        await AdvanceWorkflowStepAsync(req,
            "Architecture Department \u2013 CAD Files",
            "Upload CAD Files", userId.ToString(), null);

        var fromStatus = req.Status.ToString();
        req.Status = PossessionRequestStatus.CadCompleted;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, fromStatus, "CadCompleted", "CompleteCad", userId, null);
        return req;
    }

    // -----------------------------------------------------------------------
    // Structure completed
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> CompleteStructureAsync(
        Guid requestId, string reportFileUrl, string? observations, Guid userId)
    {
        var req = await GetRequestAsync(requestId);

        _db.StructuralReports.Add(new StructuralReport
        {
            RequestId = req.Id,
            FileUrl = reportFileUrl,
            Observations = observations,
            CompletedBy = userId
        });

        req.StructureCompleted = true;
        if (req.MEPCompleted)
            req.Status = PossessionRequestStatus.PrincipalArchitectApproved;
        else
            req.Status = PossessionRequestStatus.StructureCompleted;

        await AdvanceWorkflowStepAsync(req, "Structure Department – Structural Design",
            "Generate Structural Report", userId.ToString(), null);

        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, "ArchitectureApproved", req.Status.ToString(),
            "CompleteStructure", userId, null);
        return req;
    }

    // -----------------------------------------------------------------------
    // MEP completed
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> CompleteMEPAsync(
        Guid requestId, string reportFileUrl, string? observations, Guid userId)
    {
        var req = await GetRequestAsync(requestId);

        _db.MEPReports.Add(new MEPReport
        {
            RequestId = req.Id,
            FileUrl = reportFileUrl,
            Observations = observations,
            CompletedBy = userId
        });

        req.MEPCompleted = true;
        if (req.StructureCompleted)
            req.Status = PossessionRequestStatus.PrincipalArchitectApproved;
        else
            req.Status = PossessionRequestStatus.MEPCompleted;

        await AdvanceWorkflowStepAsync(req, "MEP Department – MEP Design",
            "Generate MEP Report", userId.ToString(), null);

        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, "ArchitectureApproved", req.Status.ToString(),
            "CompleteMEP", userId, null);
        return req;
    }

    // -----------------------------------------------------------------------
    // Principal Architect approve/send back
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> PrincipalApproveAsync(
        Guid requestId, Guid userId, bool approved, string? comments)
    {
        var req = await GetRequestAsync(requestId);

        if (approved)
        {
            req.Status = PossessionRequestStatus.PrincipalArchitectApproved;
            await AdvanceWorkflowStepAsync(req, "Principal Architect – Design Review",
                "Approve Designs", userId.ToString(), $"{{\"action\":\"Approve Designs\"}}");
        }
        else
        {
            // Send back — use ReturnToPreviousStep in the WE (to Architecture step)
            req.Status = PossessionRequestStatus.PackagePaid;
        }

        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, "PrincipalArchitectApproved", req.Status.ToString(),
            approved ? "PrincipalApprove" : "SendBackForRevision", userId, comments);
        return req;
    }

    public async Task<PossessionRequest> UploadSoilTestAsync(
        Guid requestId, SoilTestDto dto, Guid userId)
    {
        var req = await GetRequestAsync(requestId);

        if (req.SoilTestReport != null)
        {
            req.SoilTestReport.ReportFileUrl = dto.ReportFileUrl;
            req.SoilTestReport.TestDate = dto.TestDate;
        }
        else
        {
            _db.SoilTestReports.Add(new SoilTestReport
            {
                RequestId = req.Id,
                TestDate = dto.TestDate,
                LabName = dto.LabName,
                SoilBearingCapacity = dto.SoilBearingCapacity,
                ResultSummary = dto.ResultSummary,
                ReportFileUrl = dto.ReportFileUrl,
                UploadedBy = userId
            });
            req.SoilTestReportUrl = dto.ReportFileUrl;
        }

        await _db.SaveChangesAsync();
        return req;
    }

    // -----------------------------------------------------------------------
    // Building Control survey
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> SubmitBuildingControlAsync(
        Guid requestId, BuildingControlDto dto, Guid userId)
    {
        var req = await GetRequestAsync(requestId);

        var dept = await _db.Departments
            .FirstOrDefaultAsync(d => d.DepartmentCode == "BC")
            ?? throw new InvalidOperationException("Building Control department not found");

        _db.SurveyObservations.Add(new SurveyObservation
        {
            RequestId = req.Id,
            DepartmentId = dept.Id,
            OfficerName = dto.OfficerName,
            SurveyDate = dto.SurveyDate,
            Observations = dto.Observations,
            Violations = dto.Violations,
            Suggestions = dto.Suggestions,
            PhotoUrls = dto.PhotoUrlsJson
        });

        req.BuildingControlCompleted = true;
        req.Status = PossessionRequestStatus.BuildingControlCompleted;

        await AdvanceWorkflowStepAsync(req,
            "Building Control – Physical Survey",
            "Conduct Building Control Survey", userId.ToString(), null);

        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, "PrincipalArchitectApproved", req.Status.ToString(),
            "SubmitBuildingControl", userId, null);
        return req;
    }

    // -----------------------------------------------------------------------
    // Final approval
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> FinalApproveAsync(
        Guid requestId, Guid userId, string? comments)
    {
        var req = await GetRequestAsync(requestId);
        req.Status = PossessionRequestStatus.FinalApproved;

        await AdvanceWorkflowStepAsync(req, "DHA Design Head – Final Approval",
            "Final Approve", userId.ToString(), $"{{\"action\":\"Final Approve\"}}");

        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, "TownPlanningCompleted", "FinalApproved",
            "FinalApprove", userId, comments);
        return req;
    }

    // -----------------------------------------------------------------------
    // Admin Review: attach documents then Initiate or Reject
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> AdminReviewAsync(Guid requestId, AdminReviewDto dto, Guid userId)
    {
        var req = await GetRequestAsync(requestId);
        if (req.Status != PossessionRequestStatus.Submitted)
            throw new InvalidOperationException("Only Submitted requests can be reviewed by admin.");

        req.AllotmentLetterUrl = dto.AllotmentLetterUrl;
        req.CnicUrl = dto.CnicUrl;
        req.MessageScreenshotUrl = dto.MessageScreenshotUrl;
        req.EStampPaperUrl = dto.EStampPaperUrl;
        req.AuthorizedPersonCnicUrl = dto.AuthorizedPersonCnicUrl;
        req.AuthorizedPersonPhone = dto.AuthorizedPersonPhone;
        req.AdminReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (dto.Action == "Initiate")
        {
            // Complete WE Step 1 (Reception – Submit NOC/NDC Request)
            await AdvanceWorkflowStepAsync(req,
                "Reception – Submit NOC/NDC Request",
                "Create Request (Form 1)",
                userId.ToString(), null);

            // Complete WE Step 2 (Admin – Document Review) → triggers parallel Transfer + Finance
            await AdvanceWorkflowStepAsync(req,
                "Admin – Document Review",
                "Admin Review – Initiate",
                userId.ToString(), null);

            // For Revised/AsBuilt workflows with a pre-selected package, auto-advance
            // the Package Selection step so the workflow lands on Payment Confirmation.
            if (req.SelectedPackageId.HasValue &&
                (req.RequestType == RequestType.RevisedPlan || req.RequestType == RequestType.AsBuiltPlan))
            {
                await AdvanceWorkflowStepAsync(req,
                    "Reception – Package Selection",
                    "Select Design Package",
                    userId.ToString(), null);

                // Create the pending payment record (mirrors SelectPackageAsync logic)
                var pkg = await _db.Packages
                    .Include(p => p.LineItems)
                    .FirstOrDefaultAsync(p => p.Id == req.SelectedPackageId);

                if (pkg != null && !await _db.Payments.AnyAsync(p => p.RequestId == req.Id))
                {
                    var totalAmount = pkg.LineItems.Where(li => !li.IsFree).Sum(li => li.AmountDDFC);
                    var challanNo = $"CHN-{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
                    _db.Payments.Add(new Payment
                    {
                        RequestId   = req.Id,
                        ChallanNo   = challanNo,
                        TotalAmount = totalAmount,
                        Status      = PaymentStatus.Pending,
                    });
                }

                req.Status = PossessionRequestStatus.PackageSelected;
                await _db.SaveChangesAsync();
                await LogHistoryAsync(req.Id, "Submitted", "PackageSelected", "AdminReview-AutoPackage", userId, null);
            }
            else
            {
                req.Status = PossessionRequestStatus.Initiated;
                await _db.SaveChangesAsync();
                await LogHistoryAsync(req.Id, "Submitted", "Initiated", "AdminReview-Initiate", userId, null);
            }
            return req;
        }

        req.AdminRejectionReason = dto.RejectionReason;
        await _db.SaveChangesAsync();
        return await RejectRequestAsync(requestId, userId, dto.RejectionReason);
    }

    // -----------------------------------------------------------------------
    // Reject (from any stage)
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> RejectRequestAsync(
        Guid requestId, Guid userId, string? comments)
    {
        var req = await GetRequestAsync(requestId);
        var fromStatus = req.Status.ToString();
        req.Status = PossessionRequestStatus.Rejected;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, fromStatus, "Rejected",
            "RejectRequest", userId, comments);
        return req;
    }

    // -----------------------------------------------------------------------
    // Document delivery
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> DeliverDocumentsAsync(
        Guid requestId, Guid userId, SurveyLanguage surveyLanguage)
    {
        var req = await GetRequestAsync(requestId);
        req.Status = PossessionRequestStatus.Delivered;

        await AdvanceWorkflowStepAsync(req, "Reception – Document Delivery",
            "Print & Hand Over Documents", userId.ToString(), null);

        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, "FinalApproved", "Delivered",
            "DeliverDocuments", userId, $"Survey language: {surveyLanguage}");
        return req;
    }

    // -----------------------------------------------------------------------
    // History
    // -----------------------------------------------------------------------
    public async Task<IEnumerable<RequestWorkflowHistory>> GetWorkflowHistoryAsync(Guid requestId) =>
        await _db.RequestWorkflowHistories
            .Include(h => h.ActionByUser)
            .Where(h => h.RequestId == requestId)
            .OrderBy(h => h.Timestamp)
            .ToListAsync();

    public async Task LogHistoryAsync(
        Guid requestId, string? fromStatus, string toStatus,
        string? action, Guid? userId, string? comments)
    {
        try
        {
            _db.RequestWorkflowHistories.Add(new RequestWorkflowHistory
            {
                RequestId = requestId,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                Action = action,
                ActionByUserId = userId,
                Comments = comments,
                Timestamp = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
        catch (Exception) { }
    }

    // -----------------------------------------------------------------------
    // Active workflow step names from the WorkflowEngine
    // -----------------------------------------------------------------------
    public async Task<IEnumerable<string>> GetActiveWorkflowStepNamesAsync(Guid requestId)
    {
        var workflowRequestId = await _db.PossessionRequests
            .Where(r => r.Id == requestId)
            .Select(r => r.WorkflowRequestId)
            .FirstOrDefaultAsync();

        if (!workflowRequestId.HasValue)
            return Enumerable.Empty<string>();

        try
        {
            var weReq = await _workflowEngine.GetRequestAsync(workflowRequestId.Value);
            return weReq.Steps
                .Where(s => s.Status == WorkflowEngine.Domain.Enums.RequestStepStatus.Active)
                .Select(s => s.ProcessStep.Name)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get active WE steps for request {RequestId}", requestId);
            return Enumerable.Empty<string>();
        }
    }

    // -----------------------------------------------------------------------
    // Internal: advance the WE step that matches the given step/action name
    // -----------------------------------------------------------------------
    private async Task AdvanceWorkflowStepAsync(
        PossessionRequest req, string stepName, string actionName,
        string performedBy, string? actionData)
    {
        if (req.WorkflowRequestId == null) return;

        try
        {
            var weReq = await _workflowEngine.GetRequestAsync(req.WorkflowRequestId.Value);
            var activeStep = weReq.Steps
                .FirstOrDefault(s =>
                    s.Status == WorkflowEngine.Domain.Enums.RequestStepStatus.Active &&
                    s.ProcessStep.Name == stepName);

            if (activeStep == null) return;

            var action = activeStep.Actions
                .FirstOrDefault(a =>
                    a.Status == WorkflowEngine.Domain.Enums.RequestActionStatus.Pending &&
                    a.StepAction.Name == actionName);

            if (action != null)
                await _workflowEngine.CompleteActionAsync(action.Id, performedBy, actionData);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WE step advance failed for {Step}/{Action}", stepName, actionName);
        }
    }
}
