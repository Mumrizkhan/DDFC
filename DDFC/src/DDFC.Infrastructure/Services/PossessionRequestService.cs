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
    private readonly ITaskAssignmentService _taskAssignmentService;
    private readonly INotificationService _notificationService;
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
        ITaskAssignmentService taskAssignmentService,
        INotificationService notificationService,
        ILogger<PossessionRequestService> logger)
    {
        _db = db;
        _workflowEngine = workflowEngine;
        _taskAssignmentService = taskAssignmentService;
        _notificationService = notificationService;
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

        // Auto-complete the submission step so a new request starts at Documents Verification.
        await AdvanceWorkflowStepAsync(req,
            "Reception \u2013 Submit NOC/NDC Request",
            "Create Request (Form 1)",
            "system", null);
        req.Status = PossessionRequestStatus.DocumentsVerification;

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

        // Complete Documents Verification — this activates the Transfer branch.
        await AdvanceWorkflowStepAsync(req,
            "Reception – Documents Verification",
            "Verify Documents",
            userId.ToString(), null);

        req.Status = PossessionRequestStatus.DocumentsVerification;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, "DocumentsVerification", "DocumentsVerification", "VerifyDocuments", userId, comments);
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
            .Include(r => r.CadAssignments).ThenInclude(a => a.AssignedUser)
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
    // Workflow step: AD Coordinator approval (runs after Finance, before DDFC Admin)
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> ApproveAdCoordAsync(Guid requestId, Guid userId, string? comments)
    {
        var req = await GetRequestAsync(requestId);
        var fromStatus = req.Status.ToString();

        await AdvanceWorkflowStepAsync(req, "AD Coordinator \u2013 Review",
            "Approve AD Coord", userId.ToString(), $"{{\"approval\":\"approved\"}}");

        req.Status = PossessionRequestStatus.AdCoordApproved;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, fromStatus, "AdCoordApproved",
            "ApproveAdCoord", userId, comments);
        return req;
    }

    // -----------------------------------------------------------------------
    // Workflow step: AD Coordinator rejection
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> RejectAdCoordAsync(Guid requestId, Guid userId, string? comments)
    {
        var req = await GetRequestAsync(requestId);
        var fromStatus = req.Status.ToString();

        await AdvanceWorkflowStepAsync(req, "AD Coordinator \u2013 Review",
            "Reject AD Coord", userId.ToString(), null);

        req.Status = PossessionRequestStatus.Rejected;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, fromStatus, "Rejected", "AdCoordReject", userId, comments);
        return req;
    }

    // -----------------------------------------------------------------------
    // Workflow step: BCD uploads possession letter (after AD Coordinator)
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> UploadBcdPossessionLetterAsync(
        Guid requestId, Guid userId, string possessionLetterFileUrl, string? comments)
    {
        var req = await GetRequestAsync(requestId);

        if (req.Status != PossessionRequestStatus.AdCoordApproved)
            throw new InvalidOperationException("AD Coordinator must approve first.");

        var normalizedUrl = possessionLetterFileUrl?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedUrl))
            throw new InvalidOperationException("Possession letter file URL is required.");

        var exists = await _db.Documents.AnyAsync(d =>
            d.RequestId == req.Id &&
            d.DocType == "Possession Letter" &&
            d.FileUrl == normalizedUrl &&
            !d.IsArchived);

        if (!exists)
        {
            _db.Documents.Add(new Document
            {
                RequestId = req.Id,
                StepName = "BCD – Upload Possession Letter",
                DocType = "Possession Letter",
                FileUrl = normalizedUrl,
                UploadedBy = userId,
                UploadedAt = DateTime.UtcNow,
                Version = 1,
            });
        }

        var fromStatus = req.Status.ToString();

        await AdvanceWorkflowStepAsync(req,
            "BCD \u2013 Upload Possession Letter",
            "Upload Possession Letter", userId.ToString(), null);

        req.Status = PossessionRequestStatus.BcdLetterUploaded;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, fromStatus, "BcdLetterUploaded",
            "BcdUploadPossessionLetter", userId, comments);
        return req;
    }

    // -----------------------------------------------------------------------
    // DDFC Admin: sign the possession letter (creates the cert record)
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> DdfcAdminSignAsync(Guid requestId, Guid userId, IssueCertDto dto)
    {
        var req = await GetRequestAsync(requestId);

        if (req.Status != PossessionRequestStatus.BcdLetterUploaded)
            throw new InvalidOperationException("BCD must upload the possession letter first.");

        var possessionLetter = await _db.Documents
            .Where(document => document.RequestId == req.Id &&
                document.DocType == "Possession Letter" &&
                document.StepName == "BCD – Upload Possession Letter" &&
                !document.IsArchived)
            .OrderByDescending(document => document.UploadedAt)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("No BCD possession letter is attached.");

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
                FileUrl           = possessionLetter.FileUrl,
            };
            _db.PossessionCertificates.Add(cert);
        }
        else
        {
            req.PossessionCertificate.FileUrl = possessionLetter.FileUrl;
        }

        var fromStatus = req.Status.ToString();

        await AdvanceWorkflowStepAsync(req,
            "DDFC Admin \u2013 Sign Possession Letter",
            "Sign Possession Letter", userId.ToString(), null);

        possessionLetter.IsSignedByAdmin = true;
        req.Status = PossessionRequestStatus.PossessionLetterSigned;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, fromStatus, "PossessionLetterSigned",
            "DdfcAdminSign", userId, "Possession letter signed and package selection opened");
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

        await AdvanceWorkflowStepAsync(req, "Reception \u2013 Payment Confirmation",
            "Confirm Payment", userId.ToString(), null);

        req.Status = PossessionRequestStatus.AdminReviewPending;

        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, "PackageSelected", "AdminReviewPending",
            "ConfirmPayment", userId, null);
        return req;
    }

    // -----------------------------------------------------------------------
    // Payment: Reception uploads the paid challan, awaiting Possession Admin approval
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> SubmitPaymentChallanAsync(Guid requestId, Guid userId, string challanNo, string scannedFileUrl)
    {
        var req = await GetRequestAsync(requestId);

        var payment = await _db.Payments
            .Include(p => p.Challan)
            .FirstOrDefaultAsync(p => p.RequestId == req.Id && p.Status == PaymentStatus.Pending);

        if (payment == null)
            throw new InvalidOperationException("No pending payment found for this request.");

        payment.ChallanNo = challanNo;
        payment.Status = PaymentStatus.PendingApproval;

        if (payment.Challan != null)
            payment.Challan.FileUrl = scannedFileUrl;
        else
            _db.PaymentChallans.Add(new PaymentChallan
            {
                PaymentId     = payment.Id,
                ChallanNumber = challanNo,
                FileUrl       = scannedFileUrl,
            });

        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, "PackageSelected", "PackageSelected",
            "SubmitPaymentChallan", userId, null);
        return req;
    }

    // -----------------------------------------------------------------------
    // Payment: Possession Admin approves the uploaded challan, advances the workflow
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> ApprovePaymentAsync(Guid requestId, Guid userId, decimal? amountPaid = null)
    {
        var req = await GetRequestAsync(requestId);

        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.RequestId == req.Id &&
                (p.Status == PaymentStatus.PendingApproval || p.Status == PaymentStatus.Pending));

        if (payment == null)
            throw new InvalidOperationException("No payment awaiting approval for this request.");

        payment.PaidAmount = amountPaid ?? payment.TotalAmount;
        payment.Status = PaymentStatus.Paid;
        payment.PaidAt = DateTime.UtcNow;

        req.Status = PossessionRequestStatus.PackagePaid;

        await AdvanceWorkflowStepAsync(req, "Reception \u2013 Payment Confirmation",
            "Confirm Payment", userId.ToString(), null);

        req.Status = PossessionRequestStatus.AdminReviewPending;

        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, "PackageSelected", "AdminReviewPending",
            "ApprovePayment", userId, null);
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
        await CompleteArchitectUploadActionAsync(req, userId);
        return req;
    }

    private async Task CompleteArchitectUploadActionAsync(PossessionRequest request, Guid uploadedBy)
    {
        if (request.WorkflowRequestId is not { } workflowId) return;
        var workflow = await _workflowEngine.GetRequestAsync(workflowId);
        var uploadAction = workflow.Steps
            .FirstOrDefault(step => step.Status == WorkflowEngine.Domain.Enums.RequestStepStatus.Active &&
                step.ProcessStep.Name == "Architect Department \u2013 House Plan Design")?
            .Actions.FirstOrDefault(action => action.StepAction.Name == "Upload House Plan" &&
                (action.Status == WorkflowEngine.Domain.Enums.RequestActionStatus.Pending ||
                 action.Status == WorkflowEngine.Domain.Enums.RequestActionStatus.InProgress));
        if (uploadAction != null)
            await _workflowEngine.CompleteActionAsync(uploadAction.Id, uploadedBy.ToString());
    }

    // -----------------------------------------------------------------------
    // Architecture – customer approves plan
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> ApprovePlanAsync(Guid requestId, Guid planId, Guid customerId)
    {
        var req = await GetRequestAsync(requestId);
        var plan = req.ArchitecturalPlans.FirstOrDefault(p => p.Id == planId)
            ?? throw new InvalidOperationException("Plan not found");

        await CompleteArchitectUploadActionAsync(req, plan.UploadedBy);

        plan.CustomerApproved = true;
        plan.ApprovedAt = DateTime.UtcNow;
        req.CustomerApprovedPlan = true;
        req.Status = PossessionRequestStatus.ArchitectureApproved;

        await AdvanceWorkflowStepAsync(req,
            "Architect Department \u2013 House Plan Design",
            "Customer Approves Plan", customerId.ToString(), "{\"action\":\"Customer Approves Plan\",\"approved\":true}");

        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, "ArchitectAssigned", "ArchitectureApproved",
            "ApprovePlan", customerId, null);
        return req;
    }

    // -----------------------------------------------------------------------
    // Soil Test – standalone step before Principal Architect Initial Review
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> SubmitInitialSoilTestAsync(Guid requestId, SoilTestDto dto, Guid userId)
    {
        var req = await GetRequestAsync(requestId);
        if (req.Status != PossessionRequestStatus.PackagePaid)
            throw new InvalidOperationException("Request must be in PackagePaid status.");

        if (req.SoilTestReport != null)
        {
            req.SoilTestReport.ReportFileUrl = dto.ReportFileUrl;
            req.SoilTestReport.TestDate = dto.TestDate;
            req.SoilTestReport.LabName = dto.LabName;
            req.SoilTestReport.SoilBearingCapacity = dto.SoilBearingCapacity;
            req.SoilTestReport.ResultSummary = dto.ResultSummary;
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
                UploadedBy = userId,
            });
            req.SoilTestReportUrl = dto.ReportFileUrl;
        }

        await AdvanceWorkflowStepAsync(req, "Soil Test", "Upload Soil Test", userId.ToString(), null);
        req.Status = PossessionRequestStatus.SoilTestCompleted;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, "PackagePaid", "SoilTestCompleted", "SubmitInitialSoilTest", userId, null);
        return req;
    }

    // -----------------------------------------------------------------------
    // Principal Architect – initial review
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> PAInitialReviewAsync(
        Guid requestId, Guid assignedArchitectId, SoilTestDto? soilTest, string? notes, Guid userId)
    {
        var req = await GetRequestAsync(requestId);
        if (req.Status != PossessionRequestStatus.PackagePaid &&
            req.Status != PossessionRequestStatus.SoilTestCompleted)
            throw new InvalidOperationException("Request must be in PackagePaid or SoilTestCompleted status.");

        var fromStatus = req.Status.ToString();
        req.AssignedArchitectId = assignedArchitectId;

        if (soilTest != null)
        {
            if (req.SoilTestReport != null)
            {
                req.SoilTestReport.ReportFileUrl = soilTest.ReportFileUrl;
                req.SoilTestReport.TestDate = soilTest.TestDate;
                req.SoilTestReport.LabName = soilTest.LabName;
                req.SoilTestReport.SoilBearingCapacity = soilTest.SoilBearingCapacity;
                req.SoilTestReport.ResultSummary = soilTest.ResultSummary;
            }
            else
            {
                _db.SoilTestReports.Add(new SoilTestReport
                {
                    RequestId = req.Id,
                    TestDate = soilTest.TestDate,
                    LabName = soilTest.LabName,
                    SoilBearingCapacity = soilTest.SoilBearingCapacity,
                    ResultSummary = soilTest.ResultSummary,
                    ReportFileUrl = soilTest.ReportFileUrl,
                    UploadedBy = userId,
                });
                req.SoilTestReportUrl = soilTest.ReportFileUrl;
            }
        }

        await AdvanceWorkflowStepAsync(req,
            "Principal Architect \u2013 Initial Review", "Assign Architect", userId.ToString(), null);
        req.Status = PossessionRequestStatus.ArchitectAssigned;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, fromStatus, "ArchitectAssigned", "PAInitialReview", userId, notes);
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
        await EnsureThreeDStepIsActiveAsync(req);
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
        await EnsureThreeDStepIsActiveAsync(req);
        if (req.ThreeDVisualizations.Count == 0)
            throw new InvalidOperationException("At least one 3D file must be uploaded before completing this step.");

        await AdvanceWorkflowStepAsync(req,
            "Architecture Department \u2013 3D Visualization",
            "Upload 3D Visualization Files", userId.ToString(), null);

        var fromStatus = req.Status.ToString();
        req.Status = PossessionRequestStatus.ThreeDDraftPending;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, fromStatus, "ThreeDDraftPending", "CompleteThreeD", userId, null);
        return req;
    }

    private async Task EnsureThreeDStepIsActiveAsync(PossessionRequest request)
    {
        if (request.Status != PossessionRequestStatus.ArchitectureApproved)
            throw new InvalidOperationException("Architect must approve the plan before 3D actions are available.");
        if (request.WorkflowRequestId is not { } workflowId) return;
        var workflow = await _workflowEngine.GetRequestAsync(workflowId);
        if (!workflow.Steps.Any(step => step.Status == WorkflowEngine.Domain.Enums.RequestStepStatus.Active &&
            step.ProcessStep.Name == "Architecture Department \u2013 3D Visualization"))
            throw new InvalidOperationException("The 3D Visualization step is not active.");
    }

    // -----------------------------------------------------------------------
    // Architect – assign the drafter after 3D visualization is finalized
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> AssignThreeDDrafterAsync(Guid requestId, Guid userId, Guid drafterId)
    {
        var req = await GetRequestAsync(requestId);
        if (req.Status != PossessionRequestStatus.ThreeDDraftPending)
            throw new InvalidOperationException("3D visualization must be finalized before assigning a drafter.");

        var assignment = await _db.CadAssignments
            .FirstOrDefaultAsync(a => a.RequestId == req.Id && a.CadType == "ThreeD");
        if (assignment == null)
            throw new InvalidOperationException("A 3D drafter assignment must be saved first.");

        if (assignment.AssignedUserId != drafterId)
            throw new InvalidOperationException("The drafter does not match the saved assignment.");
        if (!req.CadAssignments.Any(item => item.CadType == "ArchitectDraft")) return req;
        if (req.WorkflowRequestId is { } workflowId)
        {
            var workflow = await _workflowEngine.GetRequestAsync(workflowId);
            var action = workflow.Steps.FirstOrDefault(step =>
                step.Status == WorkflowEngine.Domain.Enums.RequestStepStatus.Active &&
                step.ProcessStep.Name == "Architect \u2013 Assign 3D Drafter & Upload Draft")?
                .Actions.FirstOrDefault(item => item.StepAction.Name == "Assign 3D Drafter" &&
                    (item.Status == WorkflowEngine.Domain.Enums.RequestActionStatus.Pending ||
                     item.Status == WorkflowEngine.Domain.Enums.RequestActionStatus.InProgress));
            if (action != null) await _workflowEngine.CompleteActionAsync(action.Id, userId.ToString());
        }
        await _db.SaveChangesAsync();
        return req;
    }

    // -----------------------------------------------------------------------
    // Architect – manually submit the completed drafter 3D draft
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> CompleteThreeDDraftAsync(Guid requestId, Guid userId)
    {
        var req = await GetRequestAsync(requestId);
        if (req.Status != PossessionRequestStatus.ThreeDDraftPending)
            throw new InvalidOperationException("The request is not awaiting the 3D draft.");

        var assignment = req.CadAssignments.FirstOrDefault(a => a.CadType == "ThreeD");
        var architect = req.CadAssignments.FirstOrDefault(item => item.CadType == "ArchitectDraft");
        if (assignment?.CompletedAt == null || string.IsNullOrWhiteSpace(assignment.FileUrl) ||
            architect?.CompletedAt == null || string.IsNullOrWhiteSpace(architect.FileUrl))
            return req;

        await AdvanceWorkflowStepAsync(req,
            "Architect \u2013 Assign 3D Drafter & Upload Draft",
            "Upload 3D Draft", userId.ToString(), assignment.FileUrl);
        var fromStatus = req.Status.ToString();
        req.Status = PossessionRequestStatus.ThreeDDraftUploaded;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, fromStatus, "ThreeDDraftUploaded", "Upload3DDraft", userId, null);
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
        if (string.IsNullOrWhiteSpace(reportFileUrl))
            throw new InvalidOperationException("A structural report must be attached before submitting the review.");
        var fromStatus = req.Status.ToString();

        if (req.WorkflowRequestId is { } workflowId)
        {
            var workflow = await _workflowEngine.GetRequestAsync(workflowId);
            var structureStep = workflow.Steps.FirstOrDefault(step =>
                step.Status == WorkflowEngine.Domain.Enums.RequestStepStatus.Active &&
                step.ProcessStep.Name == "Structure Department \u2013 Structural Design")
                ?? throw new InvalidOperationException("The Structure step is not active.");
            var actions = structureStep.Actions.Where(action =>
                (action.StepAction.Name == "Upload Structural Design" || action.StepAction.Name == "Generate Structural Report") &&
                (action.Status == WorkflowEngine.Domain.Enums.RequestActionStatus.Pending ||
                 action.Status == WorkflowEngine.Domain.Enums.RequestActionStatus.InProgress))
                .OrderBy(action => action.StepAction.Name == "Upload Structural Design" ? 0 : 1).ToList();
            foreach (var action in actions)
                await _workflowEngine.CompleteActionAsync(action.Id, userId.ToString());

            var updatedWorkflow = await _workflowEngine.GetRequestAsync(workflowId);
            if (updatedWorkflow.Steps.First(step => step.Id == structureStep.Id).Status !=
                WorkflowEngine.Domain.Enums.RequestStepStatus.Completed)
                throw new InvalidOperationException("All Structure actions must be completed before continuing.");
        }

        _db.StructuralReports.Add(new StructuralReport
        {
            RequestId = req.Id,
            FileUrl = reportFileUrl,
            Observations = observations,
            CompletedBy = userId
        });

        req.StructureCompleted = true;
        if (req.MEPCompleted)
            req.Status = PossessionRequestStatus.PrincipalArchitectReviewPending;
        else
            req.Status = PossessionRequestStatus.StructureCompleted;

        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, fromStatus, req.Status.ToString(),
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
        if (string.IsNullOrWhiteSpace(reportFileUrl))
            throw new InvalidOperationException("An MEP report must be attached before submitting the review.");
        await GetActiveMepStepAsync(req);

        _db.MEPReports.Add(new MEPReport
        {
            RequestId = req.Id,
            FileUrl = reportFileUrl,
            Observations = observations,
            CompletedBy = userId
        });

        await _db.SaveChangesAsync();
        return await TryCompleteMEPAsync(requestId, userId);
    }

    public async Task<PossessionRequest> TryCompleteMEPAsync(Guid requestId, Guid userId)
    {
        var req = await GetRequestAsync(requestId);
        var step = await GetActiveMepStepAsync(req);
        var report = req.MEPReports.OrderByDescending(item => item.CompletedAt)
            .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.FileUrl));
        var cad = req.CadAssignments.FirstOrDefault(item => item.CadType == "MEP" &&
            item.CompletedAt.HasValue && !string.IsNullOrWhiteSpace(item.FileUrl));
        if (report == null || cad == null) return req;

        var fromStatus = req.Status.ToString();
        if (step != null)
        {
            var actions = step.Actions.Where(action =>
                (action.StepAction.Name == "Upload MEP Design" || action.StepAction.Name == "Generate MEP Report") &&
                (action.Status == WorkflowEngine.Domain.Enums.RequestActionStatus.Pending ||
                 action.Status == WorkflowEngine.Domain.Enums.RequestActionStatus.InProgress))
                .OrderBy(action => action.StepAction.Name == "Upload MEP Design" ? 0 : 1).ToList();
            foreach (var action in actions)
                await _workflowEngine.CompleteActionAsync(action.Id, userId.ToString());
            var workflow = await _workflowEngine.GetRequestAsync(req.WorkflowRequestId!.Value);
            if (workflow.Steps.First(item => item.Id == step.Id).Status != WorkflowEngine.Domain.Enums.RequestStepStatus.Completed)
                throw new InvalidOperationException("All MEP actions must be completed before continuing.");
        }

        req.MEPCompleted = true;
        if (req.StructureCompleted)
            req.Status = PossessionRequestStatus.PrincipalArchitectReviewPending;
        else
            req.Status = PossessionRequestStatus.MEPCompleted;

        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, fromStatus, req.Status.ToString(),
            "CompleteMEP", userId, null);
        return req;
    }

    private async Task<WorkflowEngine.Domain.Entities.RequestStep?> GetActiveMepStepAsync(PossessionRequest request)
    {
        if (request.WorkflowRequestId is not { } workflowId)
        {
            if (request.Status != PossessionRequestStatus.StructureCompleted && request.Status != PossessionRequestStatus.MEPCompleted)
                throw new InvalidOperationException("The request is not awaiting MEP review.");
            return null;
        }
        var workflow = await _workflowEngine.GetRequestAsync(workflowId);
        return workflow.Steps.FirstOrDefault(step =>
            step.Status == WorkflowEngine.Domain.Enums.RequestStepStatus.Active &&
            step.ProcessStep.Name == "MEP Department \u2013 MEP Design")
            ?? throw new InvalidOperationException("The MEP step is not active.");
    }

    // -----------------------------------------------------------------------
    // Principal Architect approve/send back
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> PrincipalApproveAsync(
        Guid requestId, Guid userId, bool approved, string? comments)
    {
        var req = await GetRequestAsync(requestId);
        var fromStatus = req.Status.ToString();

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
        await LogHistoryAsync(req.Id, fromStatus, req.Status.ToString(),
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

    public async Task<PossessionRequest> CompleteBuildingControlAsync(Guid requestId, Guid userId)
    {
        var req = await GetRequestAsync(requestId);
        var fromStatus = req.Status.ToString();
        if (req.WorkflowRequestId is { } workflowId)
        {
            var workflow = await _workflowEngine.GetRequestAsync(workflowId);
            var step = workflow.Steps.FirstOrDefault(item =>
                item.Status == WorkflowEngine.Domain.Enums.RequestStepStatus.Active &&
                item.ProcessStep.Name == "Building Control \u2013 Physical Survey")
                ?? throw new InvalidOperationException("The Building Control step is not active.");
            var actions = step.Actions.Where(action =>
                action.Status == WorkflowEngine.Domain.Enums.RequestActionStatus.Pending ||
                action.Status == WorkflowEngine.Domain.Enums.RequestActionStatus.InProgress).ToList();
            foreach (var action in actions)
                await _workflowEngine.CompleteActionAsync(action.Id, userId.ToString());
            var updated = await _workflowEngine.GetRequestAsync(workflowId);
            if (updated.Steps.First(item => item.Id == step.Id).Status != WorkflowEngine.Domain.Enums.RequestStepStatus.Completed)
                throw new InvalidOperationException("Building Control clearance could not be completed.");
        }
        else if (req.Status != PossessionRequestStatus.PrincipalArchitectApproved)
        {
            throw new InvalidOperationException("Principal Architect approval is required before Building Control clearance.");
        }

        req.BuildingControlCompleted = true;
        req.Status = PossessionRequestStatus.BuildingControlCompleted;
        await _db.SaveChangesAsync();
        await LogHistoryAsync(req.Id, fromStatus, req.Status.ToString(), "CompleteBuildingControl", userId,
            "Final documents cleared for DHA Design Head approval");
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
    // Admin Review: post-payment approve or reject
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> AdminReviewAsync(Guid requestId, AdminReviewDto dto, Guid userId)
    {
        var req = await GetRequestAsync(requestId);
        if (req.Status != PossessionRequestStatus.AdminReviewPending)
            throw new InvalidOperationException("Only paid requests can be reviewed by admin.");

        req.AdminReviewedAt = DateTime.UtcNow;
        if (dto.Action == "Approve")
        {
            await AdvanceWorkflowStepAsync(req,
                "Admin \u2013 Post-Payment Review", "Approve", userId.ToString(), "{\"action\":\"Approve\"}");
            req.Status = PossessionRequestStatus.PackagePaid;
            await _db.SaveChangesAsync();
            await LogHistoryAsync(req.Id, "AdminReviewPending", "PackagePaid", "AdminReview-Approve", userId, dto.RejectionReason);
            return req;
        }

        if (dto.Action == "Reject")
        {
            await AdvanceWorkflowStepAsync(req,
                "Admin \u2013 Post-Payment Review", "Reject", userId.ToString(), null);
            req.AdminRejectionReason = dto.RejectionReason;
            req.Status = PossessionRequestStatus.Rejected;
            await _db.SaveChangesAsync();
            await LogHistoryAsync(req.Id, "AdminReviewPending", "Rejected", "AdminReview-Reject", userId, dto.RejectionReason);
            return req;
        }

        throw new InvalidOperationException("Admin action must be Approve or Reject.");
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
    // Documents Verification: validate submitted documents before Transfer
    // -----------------------------------------------------------------------
    public async Task<PossessionRequest> VerifyDocumentsAsync(
        Guid requestId, Guid userId, string action, string? comments)
    {
        var req = await GetRequestAsync(requestId);
        if (req.Status != PossessionRequestStatus.DocumentsVerification)
            throw new InvalidOperationException("Only requests in Documents Verification can be reviewed.");

        if (string.Equals(action, "Approve", StringComparison.OrdinalIgnoreCase))
        {
            await AdvanceWorkflowStepAsync(req,
                "Reception \u2013 Documents Verification",
                "Verify Documents", userId.ToString(), "{\"action\":\"Approve\"}");
            await _db.SaveChangesAsync();
            await LogHistoryAsync(req.Id, "DocumentsVerification", "DocumentsVerification",
                "VerifyDocuments", userId, comments);
            return req;
        }

        if (string.Equals(action, "Incomplete", StringComparison.OrdinalIgnoreCase))
        {
            await LogHistoryAsync(req.Id, "DocumentsVerification", "DocumentsVerification",
                "DocumentsIncomplete", userId, comments);
            return req;
        }

        throw new InvalidOperationException("Document verification action must be Approve or Incomplete.");
    }

    // -----------------------------------------------------------------------
    // Attach typed document (CNIC, NOC/NDC Form, Allotment Letter, etc.)
    // -----------------------------------------------------------------------
    public async Task<Document> AttachDocumentAsync(
        Guid requestId, DocumentType documentType, string fileUrl, Guid uploadedBy)
    {
        _ = await _db.PossessionRequests.FindAsync(requestId)
            ?? throw new KeyNotFoundException("Request not found");

        var label = documentType switch
        {
            DocumentType.Cnic                 => "CNIC",
            DocumentType.NocNdcForm           => "NOC/NDC Form",
            DocumentType.AllotmentLetter      => "Allotment Letter",
            DocumentType.PlotFinanceStatement => "Plot Finance Statement",
            _                                 => documentType.ToString(),
        };

        var stepName = documentType switch
        {
            DocumentType.Cnic or DocumentType.NocNdcForm or DocumentType.AllotmentLetter
                => "Reception \u2013 Submit NOC/NDC Request",
            DocumentType.PlotFinanceStatement
                => "Finance Branch \u2013 Dues Clearance",
            _   => "General",
        };

        var count = await _db.Documents.CountAsync(
            d => d.RequestId == requestId && d.DocType == label && !d.IsArchived);

        if (count >= 2)
            throw new InvalidOperationException(
                $"A maximum of 2 documents is allowed for type '{label}'.");

        var doc = new Document
        {
            RequestId  = requestId,
            StepName   = stepName,
            DocType    = label,
            FileUrl    = fileUrl,
            UploadedBy = uploadedBy,
            UploadedAt = DateTime.UtcNow,
            Version    = 1,
        };

        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();
        return doc;
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
                    a.StepAction != null &&
                    a.StepAction.Name == actionName);

            if (action == null)
            {
                _logger.LogWarning(
                    "No matching workflow action found for step {StepName} and action {ActionName}. Completing the active step as a fallback.",
                    stepName,
                    actionName);
                await _workflowEngine.CompleteStepAsync(activeStep.Id, actionData);
                return;
            }

            if (action.Status == WorkflowEngine.Domain.Enums.RequestActionStatus.Pending)
            {
                await _workflowEngine.CompleteActionAsync(action.Id, performedBy, actionData);
                return;
            }

            if (action.Status == WorkflowEngine.Domain.Enums.RequestActionStatus.InProgress)
            {
                await _workflowEngine.CompleteActionAsync(action.Id, performedBy, actionData);
                return;
            }

            // If the matching action has already been completed, the step may still be active
            // because of stale data. Complete the step explicitly so the workflow can continue.
            if (action.Status == WorkflowEngine.Domain.Enums.RequestActionStatus.Completed &&
                activeStep.Status == WorkflowEngine.Domain.Enums.RequestStepStatus.Active)
            {
                await _workflowEngine.CompleteStepAsync(activeStep.Id, actionData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WE step advance failed for {Step}/{Action}", stepName, actionName);
        }
    }
}
