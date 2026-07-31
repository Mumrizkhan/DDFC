using DDFC.Application.Interfaces;
using DDFC.Domain.Entities;
using DDFC.Domain.Enums;
using DDFC.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDFC.API.Controllers;

[ApiController]
[Route("api/v1/requests")]
[Authorize]
public class RequestsController : ControllerBase
{
    private readonly DDFCDbContext           _db;
    private readonly IPossessionRequestService _svc;
    private readonly ITaskAssignmentService  _tasks;

    public RequestsController(DDFCDbContext db, IPossessionRequestService svc,
        ITaskAssignmentService tasks)
    {
        _db    = db;
        _svc   = svc;
        _tasks = tasks;
    }

    // ── Queries ───────────────────────────────────────────────────────────────
    [HttpGet]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] Guid? customerId, [FromQuery] int? requestType)
    {
        var q = _db.PossessionRequests
            .Include(r => r.Customer)
            .Include(r => r.Plot)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statuses = status
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Enum.Parse<PossessionRequestStatus>(s, ignoreCase: true))
                .ToArray();
            q = q.Where(r => statuses.Contains(r.Status));
        }

        if (customerId.HasValue)
            q = q.Where(r => r.CustomerId == customerId.Value);

        if (requestType.HasValue)
            q = q.Where(r => (int)r.RequestType == requestType.Value);

        var list = (await q.OrderByDescending(r => r.CreatedAt).ToListAsync())
            .Select(r => MapToDto(r));
        return Ok(list);
    }

    [HttpGet("my")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> GetMyRequests()
    {
        var customerId = Guid.Parse(User.FindFirst("customerId")!.Value);
        var list = await _db.PossessionRequests
            .Include(r => r.Plot)
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var req = await _db.PossessionRequests
            .Include(r => r.Customer)
            .Include(r => r.Plot)
            .Include(r => r.SelectedPackage).ThenInclude(p => p!.LineItems)
            .Include(r => r.WorkflowHistory).ThenInclude(h => h.ActionByUser)
            .Include(r => r.ArchitecturalPlans)
            .Include(r => r.StructuralReports)
            .Include(r => r.MEPReports)
            .Include(r => r.SoilTestReport)
            .Include(r => r.Documents)
            .Include(r => r.Payments)
            .Include(r => r.AssignedArchitect)
            .Include(r => r.DelayUndertaking)
            .Include(r => r.ArchitectUndertaking)
            .Include(r => r.PlotAnnexation)
            .Include(r => r.PlotMerging)
            .Include(r => r.CadAssignments).ThenInclude(ca => ca.AssignedUser)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (req is null) return NotFound();
        var activeSteps = await _svc.GetActiveWorkflowStepNamesAsync(id);
        return Ok(MapToDto(req, activeSteps));
    }

    [HttpGet("{requestId}")]
    public async Task<IActionResult> GetByRequestId(string requestId)
    {
        var req = await _db.PossessionRequests
            .Include(r => r.Customer)
            .Include(r => r.Plot)
            .Include(r => r.SelectedPackage).ThenInclude(p => p!.LineItems)
            .Include(r => r.WorkflowHistory).ThenInclude(h => h.ActionByUser)
            .Include(r => r.ArchitecturalPlans)
            .Include(r => r.StructuralReports)
            .Include(r => r.MEPReports)
            .Include(r => r.SoilTestReport)
            .Include(r => r.Documents)
            .Include(r => r.Payments)
            .Include(r => r.DelayUndertaking)
            .Include(r => r.ArchitectUndertaking)
            .Include(r => r.PlotAnnexation)
            .Include(r => r.PlotMerging)
            .Include(r => r.CadAssignments).ThenInclude(ca => ca.AssignedUser)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);
        if (req is null) return NotFound();
        var activeSteps = await _svc.GetActiveWorkflowStepNamesAsync(req.Id);
        return Ok(MapToDto(req, activeSteps));
    }

    // ── Create ────────────────────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateRequestDto dto)
    {
        try
        {
            var req = await _svc.CreateRequestAsync(dto, GetCurrentUserId());
            return CreatedAtAction(nameof(GetById), new { id = req.Id }, req);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── Step 2 / 3: Transfer & Finance ────────────────────────────────────────
    [HttpPost("{id:guid}/transfer/approve")]
    [Authorize(Policy = "CanApproveTransfer")]
    public async Task<IActionResult> ApproveTransfer(Guid id, [FromBody] ActionNoteDto note)
    {
        await _svc.ApproveTransferAsync(id, GetCurrentUserId(), note.Comments);
        return NoContent();
    }

    [HttpPost("{id:guid}/transfer/reject")]
    [Authorize(Policy = "CanApproveTransfer")]
    public async Task<IActionResult> RejectTransfer(Guid id, [FromBody] ActionNoteDto note)
    {
        await _svc.RejectTransferAsync(id, GetCurrentUserId(), note.Comments);
        return NoContent();
    }

    [HttpPost("{id:guid}/transfer/clarification")]
    [Authorize(Policy = "CanApproveTransfer")]
    public async Task<IActionResult> RequestTransferClarification(Guid id, [FromBody] ActionNoteDto note)
    {
        await _svc.RequestTransferClarificationAsync(id, GetCurrentUserId(), note.Comments);
        return NoContent();
    }

    [HttpPost("{id:guid}/finance/approve")]
    [Authorize(Policy = "CanApproveFinance")]
    public async Task<IActionResult> ApproveFinance(Guid id, [FromBody] FinanceApproveDto dto)
    {
        await _svc.ApproveFinanceAsync(id, GetCurrentUserId(), dto.Comments, dto.AdcAmount);
        return NoContent();
    }

    [HttpPost("{id:guid}/finance/reject")]
    [Authorize(Policy = "CanApproveFinance")]
    public async Task<IActionResult> RejectFinance(Guid id, [FromBody] ActionNoteDto note)
    {
        await _svc.RejectFinanceAsync(id, GetCurrentUserId(), note.Comments);
        return NoContent();
    }

    // ── Step 4: DDFC Admin – Sign Possession Letter ───────────────────────────
    [HttpPost("{id:guid}/ddfc-admin/sign")]
    [Authorize(Policy = "CanSignPossessionLetter")]
    public async Task<IActionResult> DdfcAdminSign(Guid id, [FromBody] IssueCertDto dto)
    {
        try
        {
            await _svc.DdfcAdminSignAsync(id, GetCurrentUserId(), dto);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("{id:guid}/possession-certificate/preview")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> PreviewPossessionCert(Guid id)
    {
        var req = await _db.PossessionRequests
            .Include(r => r.Customer)
            .Include(r => r.Plot)
            .Include(r => r.PossessionCertificate)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (req is null) return NotFound();

        var cert = req.PossessionCertificate;
        var plot = req.Plot;
        var customer = req.Customer;

        var keyPlanSvg = GenerateKeyPlanSvg(
            plotNo:  plot?.PlotNumber ?? "",
            north:   plot?.BoundedNorth ?? "—",
            south:   plot?.BoundedSouth ?? "—",
            east:    plot?.BoundedEast  ?? "—",
            west:    plot?.BoundedWest  ?? "—");

        var html = BuildCertificateHtml(req, cert, plot, customer, keyPlanSvg);
        return Content(html, "text/html");
    }

    private static string GenerateKeyPlanSvg(string plotNo, string north, string south, string east, string west)
    {
        return $$"""
        <svg width="280" height="220" viewBox="0 0 280 220" xmlns="http://www.w3.org/2000/svg" font-family="Arial" font-size="10">
          <!-- Compass rose -->
          <text x="260" y="14" font-size="9" text-anchor="middle" font-weight="bold">N</text>
          <line x1="260" y1="17" x2="260" y2="28" stroke="#333" stroke-width="1.5"/>
          <polygon points="255,28 265,28 260,18" fill="#333"/>

          <!-- Plot box -->
          <rect x="80" y="70" width="120" height="80" fill="#fff8e1" stroke="#d97706" stroke-width="2" rx="2"/>
          <text x="140" y="114" text-anchor="middle" font-weight="bold" fill="#92400e">{{plotNo}}</text>
          <text x="140" y="126" text-anchor="middle" font-size="8" fill="#78350f">SUBJECT PLOT</text>

          <!-- North label -->
          <text x="140" y="58" text-anchor="middle" fill="#1e40af" font-weight="bold">{{north}}</text>
          <!-- South label -->
          <text x="140" y="170" text-anchor="middle" fill="#1e40af" font-weight="bold">{{south}}</text>
          <!-- East label -->
          <text x="215" y="114" text-anchor="start" fill="#1e40af" font-weight="bold">{{east}}</text>
          <!-- West label -->
          <text x="65" y="114" text-anchor="end" fill="#1e40af" font-weight="bold">{{west}}</text>

          <!-- Border labels -->
          <text x="140" y="45" text-anchor="middle" font-size="8" fill="#374151">NORTH</text>
          <text x="140" y="185" text-anchor="middle" font-size="8" fill="#374151">SOUTH</text>
          <text x="235" y="125" text-anchor="start" font-size="8" fill="#374151">EAST</text>
          <text x="40" y="125" text-anchor="middle" font-size="8" fill="#374151">WEST</text>
        </svg>
        """;
    }

    private static string BuildCertificateHtml(
        PossessionRequest req,
        PossessionCertificate? cert,
        DDFC.Domain.Entities.Plot? plot,
        DDFC.Domain.Entities.Customer? customer,
        string keyPlanSvg)
    {
        var genDate   = cert?.GeneratedAt   ?? DateTime.UtcNow;
        var validUntil = cert?.ValidUntil   ?? DateTime.UtcNow.AddMonths(6);
        var handedBy   = cert?.HandedOverBy ?? "________________________";
        var handedDate = cert?.HandedOverDate?.ToString("dd MMM yyyy") ?? "________________";
        var takenBy    = cert?.TakenOverBy  ?? "________________________";
        var takenDate  = cert?.TakenOverDate?.ToString("dd MMM yyyy")  ?? "________________";
        var chiefSurveyor = cert?.ChiefSurveyorName ?? "Chief Surveyor";
        var adTpBcd       = cert?.AdTpBcdName       ?? "AD TP & BCD";

        var ls1 = plot?.LongerSide1.HasValue  == true ? FormattableString.Invariant($"{plot.LongerSide1:0.00}'") : "—";
        var ls2 = plot?.LongerSide2.HasValue  == true ? FormattableString.Invariant($"{plot.LongerSide2:0.00}'") : "—";
        var ss1 = plot?.ShorterSide1.HasValue == true ? FormattableString.Invariant($"{plot.ShorterSide1:0.00}'") : "—";
        var ss2 = plot?.ShorterSide2.HasValue == true ? FormattableString.Invariant($"{plot.ShorterSide2:0.00}'") : "—";

        return $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8"/>
          <title>Possession Certificate – {{req.RequestId}}</title>
          <style>
            @page { size: A4; margin: 1.5cm; }
            @media print { .no-print { display:none; } body { margin:0; } }
            * { box-sizing: border-box; }
            body { font-family: "Times New Roman", serif; font-size: 11pt; color: #111; background:#fff; margin:0; padding:16px; }
            .page { max-width: 19cm; margin: 0 auto; border: 2px solid #1e3a5f; padding: 20px; }
            .header { display:flex; align-items:center; justify-content:space-between; border-bottom: 2px solid #1e3a5f; padding-bottom: 10px; margin-bottom: 12px; }
            .header-center h1 { font-size: 14pt; font-weight:bold; text-align:center; color:#1e3a5f; margin:0; }
            .header-center h2 { font-size:11pt; text-align:center; margin:2px 0; color:#374151; }
            .header-center p  { font-size:9pt; text-align:center; margin:0; color:#6b7280; }
            .badge { background:#1e3a5f; color:#fff; padding:4px 12px; border-radius:4px; font-size:9pt; font-weight:bold; }
            .cert-title { text-align:center; font-size:15pt; font-weight:bold; letter-spacing:2px; text-transform:uppercase; color:#1e3a5f; margin:10px 0 4px; border:1px solid #1e3a5f; padding:6px; }
            .cert-sub { text-align:center; font-size:9pt; color:#6b7280; margin-bottom:14px; }
            table.info { width:100%; border-collapse:collapse; font-size:10.5pt; margin-bottom:12px; }
            table.info td { border:1px solid #d1d5db; padding:4px 8px; }
            table.info td:first-child { background:#f0f4ff; font-weight:bold; width:35%; }
            .section-title { font-weight:bold; color:#1e3a5f; font-size:11pt; margin:10px 0 4px; border-bottom:1px solid #93c5fd; }
            .dims-grid { display:grid; grid-template-columns:1fr 1fr 1fr 1fr; gap:6px; margin-bottom:12px; }
            .dim-box { border:1px solid #d1d5db; padding:6px 8px; text-align:center; border-radius:4px; }
            .dim-box .label { font-size:8pt; color:#6b7280; }
            .dim-box .value { font-size:12pt; font-weight:bold; color:#1e3a5f; }
            .key-plan { border:1px solid #d1d5db; padding:8px; text-align:center; margin-bottom:12px; background:#f9fafb; }
            .key-plan-title { font-weight:bold; font-size:9pt; color:#374151; margin-bottom:4px; }
            .sig-row { display:grid; grid-template-columns:1fr 1fr; gap:20px; margin-top:20px; }
            .sig-box { border-top:1px solid #374151; padding-top:6px; }
            .sig-box .name { font-weight:bold; font-size:10.5pt; }
            .sig-box .role { font-size:9pt; color:#6b7280; }
            .stamp-row { display:grid; grid-template-columns:1fr 1fr; gap:20px; margin-top:14px; }
            .stamp-box { border:1px dashed #9ca3af; padding:10px; min-height:60px; text-align:center; }
            .stamp-box .stamp-name { font-weight:bold; font-size:10pt; }
            .stamp-box .stamp-role { font-size:8.5pt; color:#6b7280; }
            .footer { margin-top:14px; border-top:1px solid #1e3a5f; padding-top:8px; display:flex; justify-content:space-between; font-size:8.5pt; color:#6b7280; }
            .no-print { text-align:center; margin-bottom:14px; }
            .no-print button { background:#1e3a5f; color:#fff; border:none; padding:8px 24px; font-size:11pt; cursor:pointer; border-radius:4px; }
          </style>
        </head>
        <body>
          <div class="no-print">
            <button onclick="window.print()">&#x1F5A8; Print / Save as PDF</button>
          </div>
          <div class="page">
            <!-- Header -->
            <div class="header">
              <div>
                <div style="font-size:10pt; font-weight:bold; color:#1e3a5f;">CDFC DWG</div>
                <div style="font-size:8pt; color:#6b7280;">Cantonment DHA Framework Corporation<br/>Defence Works Group</div>
              </div>
              <div class="header-center">
                <h1>DHA PESHAWAR</h1>
                <h2>Defence Housing Authority</h2>
                <p>Possession &amp; Housing Design Facilitation Centre</p>
              </div>
              <div style="text-align:right;">
                <div style="font-size:8pt; color:#6b7280;">Ref: {{req.RequestId}}</div>
                <div style="font-size:8pt; color:#6b7280;">Date: {{genDate:dd MMM yyyy}}</div>
                <div class="badge" style="margin-top:4px;">MEMBER'S COPY</div>
              </div>
            </div>

            <!-- Title -->
            <div class="cert-title">POSSESSION CERTIFICATE</div>
            <div class="cert-sub">This certificate is issued to confirm handing over of physical possession of the following plot.</div>

            <!-- Plot & Owner Info -->
            <div class="section-title">Plot &amp; Owner Details</div>
            <table class="info">
              <tr>
                <td>Plot No.</td><td>{{plot?.PlotNumber ?? "—"}}</td>
                <td>Sector No.</td><td>{{plot?.SectorNo ?? "—"}}</td>
              </tr>
              <tr>
                <td>Street No.</td><td>{{plot?.StreetNo ?? "—"}}</td>
                <td>Phase No.</td><td>{{plot?.PhaseNo ?? "—"}}</td>
              </tr>
              <tr>
                <td>Plot Type</td><td>{{plot?.PlotType.ToString() ?? "—"}}</td>
                <td>Plot Size</td><td>{{plot?.PlotSize.ToString() ?? "—"}}</td>
              </tr>
              <tr>
                <td>Owner Name</td><td>{{req.OwnerTitle}} {{req.OwnerName}}</td>
                <td>CNIC</td><td>{{customer?.CNIC ?? "—"}}</td>
              </tr>
              <tr>
                <td>Guardian / S/o / D/o</td><td colspan="3">{{req.GuardianRelation}}: {{req.GuardianName}}</td>
              </tr>
              <tr>
                <td>File No.</td><td>{{req.FileNo}}</td>
                <td>Membership / DPR No.</td><td>{{req.MembershipDPRNo}}</td>
              </tr>
            </table>

            <!-- Dimensions -->
            <div class="section-title">Plot Dimensions</div>
            <div class="dims-grid">
              <div class="dim-box"><div class="label">Longer Side (1)</div><div class="value">{{ls1}}</div></div>
              <div class="dim-box"><div class="label">Longer Side (2)</div><div class="value">{{ls2}}</div></div>
              <div class="dim-box"><div class="label">Shorter Side (1)</div><div class="value">{{ss1}}</div></div>
              <div class="dim-box"><div class="label">Shorter Side (2)</div><div class="value">{{ss2}}</div></div>
            </div>

            <!-- Boundaries -->
            <div class="section-title">Bounded By</div>
            <table class="info">
              <tr><td>North</td><td>{{plot?.BoundedNorth ?? "—"}}</td><td>South</td><td>{{plot?.BoundedSouth ?? "—"}}</td></tr>
              <tr><td>East</td><td>{{plot?.BoundedEast ?? "—"}}</td><td>West</td><td>{{plot?.BoundedWest ?? "—"}}</td></tr>
            </table>

            <!-- Key Plan -->
            <div class="key-plan">
              <div class="key-plan-title">KEY PLAN</div>
              {{keyPlanSvg}}
            </div>

            <!-- Handover signatures -->
            <div class="section-title">Handover Confirmation</div>
            <div class="sig-row">
              <div class="sig-box">
                <div class="name">{{handedBy}}</div>
                <div class="role">Possession Handed Over By</div>
                <div style="font-size:9pt; margin-top:4px;">Date: {{handedDate}}</div>
                <div style="font-size:9pt; color:#6b7280; margin-top:6px;">Signature &amp; Stamp</div>
              </div>
              <div class="sig-box">
                <div class="name">{{takenBy}}</div>
                <div class="role">Possession Taken Over By</div>
                <div style="font-size:9pt; margin-top:4px;">Date: {{takenDate}}</div>
                <div style="font-size:9pt; color:#6b7280; margin-top:6px;">Signature</div>
              </div>
            </div>

            <!-- Stamps -->
            <div class="stamp-row">
              <div class="stamp-box">
                <div class="stamp-name">{{chiefSurveyor}}</div>
                <div class="stamp-role">Chief Surveyor, DHA Peshawar</div>
              </div>
              <div class="stamp-box">
                <div class="stamp-name">{{adTpBcd}}</div>
                <div class="stamp-role">AD TP &amp; BCD, DHA Peshawar</div>
              </div>
            </div>

            <!-- Footer -->
            <div class="footer">
              <span>Valid Until: {{validUntil:dd MMM yyyy}}</span>
              <span>Ownership is certified and legally binding.</span>
              <span>Member's Copy &#x2013; Template v{{cert?.TemplateVersion ?? 1}}</span>
            </div>
          </div>
        </body>
        </html>
        """;
    }

    // ── Delay Undertaking ─────────────────────────────────────────────────────

    /// <summary>
    /// DDFC staff request the customer to sign a delay undertaking.
    /// Allowed when status = PossessionLetterSigned.
    /// </summary>
    [HttpPost("{id:guid}/delay-undertaking/request")]
    [Authorize(Policy = "CanSignPossessionLetter")]
    public async Task<IActionResult> RequestDelayUndertaking(Guid id, [FromBody] RequestDelayUndertakingDto dto)
    {
        try
        {
            var req = await _svc.RequestDelayUndertakingAsync(
                id, GetCurrentUserId(), dto.DelayReason, dto.ExpectedDelayDays, dto.Notes);
            return Ok(MapToDto(req));
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>
    /// Customer signs the delay undertaking.
    /// Accepted when status = PossessionLetterSigned (customer-initiated) or DelayUndertakingRequested (DDFC-requested).
    /// </summary>
    [HttpPost("{id:guid}/delay-undertaking/sign")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> SignDelayUndertaking(Guid id, [FromBody] SignDelayUndertakingRequestDto dto)
    {
        try
        {
            var req = await _svc.SignDelayUndertakingAsync(
                id, GetCurrentUserId(),
                new Application.Interfaces.SignDelayUndertakingDto(dto.UndertakingDocumentUrl, dto.Notes));
            return Ok(MapToDto(req));
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>
    /// DDFC staff skip the delay undertaking step and proceed directly to package selection.
    /// </summary>
    [HttpPost("{id:guid}/delay-undertaking/skip")]
    [Authorize(Policy = "CanSignPossessionLetter")]
    public async Task<IActionResult> SkipDelayUndertaking(Guid id)
    {
        try
        {
            var req = await _svc.SkipDelayUndertakingAsync(id, GetCurrentUserId());
            return Ok(MapToDto(req));
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // ── Architect Undertaking (architecture stage, steps 10-11) ───────────────

    /// <summary>Returns an HTML preview of the architecture-stage undertaking document.</summary>
    [HttpGet("{id:guid}/undertaking/preview")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> PreviewUndertaking(Guid id)
    {
        var req = await _db.PossessionRequests
            .Include(r => r.Customer)
            .Include(r => r.Plot)
            .Include(r => r.ArchitectUndertaking)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (req is null) return NotFound();

        var customer = req.Customer;
        var plot     = req.Plot;
        var u        = req.ArchitectUndertaking;
        var issueDate = u?.CreatedAt ?? DateTime.UtcNow;
        var holdDays  = u?.HoldDays ?? 30;
        var endDate   = issueDate.AddDays(holdDays);

        var html = $$"""
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8"/>
  <title>Architect Undertaking – {{req.RequestId}}</title>
  <style>
    @page { size: A4; margin: 2cm; }
    @media print { .no-print { display:none; } body { margin:0; } }
    body { font-family: "Times New Roman", serif; font-size: 12pt; color:#111; background:#fff; padding:20px; }
    .page { max-width:17cm; margin:0 auto; }
    .header { text-align:center; border-bottom:2px solid #1e3a5f; padding-bottom:10px; margin-bottom:16px; }
    .header h1 { font-size:16pt; color:#1e3a5f; margin:0; }
    .header h2 { font-size:12pt; color:#374151; margin:4px 0 0; }
    h3 { text-align:center; font-size:14pt; letter-spacing:2px; text-transform:uppercase; color:#1e3a5f; margin:16px 0; }
    p  { line-height:1.8; margin:8px 0; text-align:justify; }
    table.info { width:100%; border-collapse:collapse; margin:12px 0; font-size:11pt; }
    table.info td { border:1px solid #ccc; padding:5px 10px; }
    table.info td:first-child { background:#f0f4ff; font-weight:bold; width:38%; }
    .sig-row { display:grid; grid-template-columns:1fr 1fr; gap:40px; margin-top:40px; }
    .sig-box { border-top:1px solid #333; padding-top:6px; font-size:10pt; color:#555; }
    .no-print { text-align:center; margin-bottom:16px; }
    .no-print button { background:#1e3a5f; color:#fff; border:none; padding:8px 24px; font-size:11pt; cursor:pointer; border-radius:4px; }
  </style>
</head>
<body>
  <div class="no-print">
    <button onclick="window.print()">&#x1F5A8; Print / Save as PDF</button>
  </div>
  <div class="page">
    <div class="header">
      <h1>DHA PESHAWAR</h1>
      <h2>Defence Housing Authority — Design Facilitation Centre</h2>
    </div>
    <h3>Architect Undertaking</h3>
    <table class="info">
      <tr><td>Request Ref.</td><td>{{req.RequestId}}</td></tr>
      <tr><td>Owner Name</td><td>{{req.OwnerTitle}} {{req.OwnerName}}</td></tr>
      <tr><td>Plot No.</td><td>{{plot?.PlotNumber ?? "—"}}, Sector {{plot?.SectorNo ?? "—"}}, Phase {{plot?.PhaseNo ?? "—"}}</td></tr>
      <tr><td>File No.</td><td>{{req.FileNo}}</td></tr>
      <tr><td>Date of Undertaking</td><td>{{issueDate:dd MMM yyyy}}</td></tr>
      <tr><td>Hold Period</td><td>{{holdDays}} days (until {{endDate:dd MMM yyyy}})</td></tr>
    </table>
    <p>
      I, <strong>{{req.OwnerTitle}} {{req.OwnerName}}</strong>, the registered owner of the above-mentioned plot,
      hereby acknowledge that the architectural design, 3D visualisation, structural and MEP review process
      for my possession request is currently in progress with DHA Peshawar Design Facilitation Centre (DDFC).
    </p>
    <p>
      I understand and accept that the completion of the design review may require up to
      <strong>{{holdDays}} calendar days</strong> from the date of this undertaking (i.e., until
      <strong>{{endDate:dd MMM yyyy}}</strong>), during which my request will remain on hold.
    </p>
    <p>
      I undertake not to raise any objection or claim against DHA Peshawar or DDFC regarding any delay
      within the specified period and agree that this undertaking shall be binding upon me, my heirs,
      and legal successors.
    </p>
    <div class="sig-row">
      <div class="sig-box">
        <div>{{req.OwnerTitle}} {{req.OwnerName}}</div>
        <div>Owner / Applicant</div>
        <div style="margin-top:6px;">CNIC: {{customer?.CNIC ?? "—"}}</div>
        <div>Date: _______________</div>
      </div>
      <div class="sig-box">
        <div>Authorised Officer</div>
        <div>DDFC — DHA Peshawar</div>
        <div style="margin-top:6px;">Stamp &amp; Signature</div>
        <div>Date: _______________</div>
      </div>
    </div>
  </div>
</body>
</html>
""";
        return Content(html, "text/html");
    }

    /// <summary>Sends the undertaking PDF to the customer's registered email (stub).</summary>
    [HttpPost("{id:guid}/undertaking/send-email")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> SendUndertakingEmail(Guid id)
    {
        var req = await _db.PossessionRequests
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (req is null) return NotFound();
        // TODO: integrate email provider — for now just return success
        return Ok(new { message = $"Undertaking email queued for {req.Customer?.Email ?? "customer"}" });
    }

    /// <summary>Attaches the signed undertaking and places the request on hold for HoldDays.</summary>
    [HttpPost("{id:guid}/undertaking/attach")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> AttachSignedUndertaking(Guid id, [FromBody] AttachUndertakingDto dto)
    {
        var req = await _db.PossessionRequests
            .Include(r => r.ArchitectUndertaking)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (req is null) return NotFound();

        var now = DateTime.UtcNow;
        if (req.ArchitectUndertaking is null)
        {
            req.ArchitectUndertaking = new DDFC.Domain.Entities.ArchitectUndertaking
            {
                RequestId        = id,
                IssuedByUserId   = GetCurrentUserId(),
                SignedDocumentUrl = dto.SignedDocumentUrl,
                HoldDays         = dto.HoldDays,
                HoldStartDate    = now,
                HoldEndDate      = now.AddDays(dto.HoldDays),
                SignedAt         = now,
                Notes            = dto.Notes
            };
            _db.ArchitectUndertakings.Add(req.ArchitectUndertaking);
        }
        else
        {
            req.ArchitectUndertaking.SignedDocumentUrl = dto.SignedDocumentUrl;
            req.ArchitectUndertaking.HoldDays          = dto.HoldDays;
            req.ArchitectUndertaking.HoldStartDate     = now;
            req.ArchitectUndertaking.HoldEndDate       = now.AddDays(dto.HoldDays);
            req.ArchitectUndertaking.SignedAt          = now;
            req.ArchitectUndertaking.Notes             = dto.Notes;
        }

        req.Status    = DDFC.Domain.Enums.PossessionRequestStatus.OnHold;
        req.UpdatedAt = now;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Undertaking attached. Request placed on hold.", holdEndDate = now.AddDays(dto.HoldDays) });
    }

    // ── Plot Annexation ───────────────────────────────────────────────────────

    [HttpPost("{id:guid}/annexation")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> RecordAnnexation(Guid id, [FromBody] AnnexationDto dto)
    {
        var req = await _db.PossessionRequests
            .Include(r => r.PlotAnnexation)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (req is null) return NotFound();

        if (req.PlotAnnexation is null)
        {
            req.PlotAnnexation = new DDFC.Domain.Entities.PlotAnnexation
            {
                RequestId        = id,
                RecordedByUserId = GetCurrentUserId(),
                AdditionalArea   = dto.AdditionalArea,
                AnnexationFee    = dto.AnnexationFee,
                Notes            = dto.Notes
            };
            _db.PlotAnnexations.Add(req.PlotAnnexation);
        }
        else
        {
            req.PlotAnnexation.AdditionalArea = dto.AdditionalArea;
            req.PlotAnnexation.AnnexationFee  = dto.AnnexationFee;
            req.PlotAnnexation.Notes          = dto.Notes;
        }

        req.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Plot annexation recorded." });
    }

    // ── Plot Merging ──────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/plot-merge")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> RecordPlotMerge(Guid id, [FromBody] PlotMergeDto dto)
    {
        var req = await _db.PossessionRequests
            .Include(r => r.PlotMerging)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (req is null) return NotFound();

        if (req.PlotMerging is null)
        {
            req.PlotMerging = new DDFC.Domain.Entities.PlotMerging
            {
                RequestId        = id,
                RecordedByUserId = GetCurrentUserId(),
                MergedPlotNumber = dto.MergedPlotNumber,
                MergedPlotSector = dto.MergedPlotSector,
                MergedPlotSize   = dto.MergedPlotSize,
                Notes            = dto.Notes
            };
            _db.PlotMergings.Add(req.PlotMerging);
        }
        else
        {
            req.PlotMerging.MergedPlotNumber = dto.MergedPlotNumber;
            req.PlotMerging.MergedPlotSector = dto.MergedPlotSector;
            req.PlotMerging.MergedPlotSize   = dto.MergedPlotSize;
            req.PlotMerging.Notes            = dto.Notes;
        }

        req.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Plot merging recorded." });
    }

    // ── CAD Assignments ───────────────────────────────────────────────────────

    /// <summary>Assign a CAD operator for a specific CAD type on this request.</summary>
    [HttpPost("{id:guid}/cad-assignments")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> AssignCadOperator(Guid id, [FromBody] CadAssignmentRequestDto dto)
    {
        var req = await _db.PossessionRequests
            .Include(r => r.CadAssignments)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (req is null) return NotFound();

        // Replace existing assignment of same cadType if present
        var existing = req.CadAssignments.FirstOrDefault(ca => ca.CadType == dto.CadType);
        if (existing is not null)
        {
            existing.AssignedUserId = dto.AssignedUserId;
            existing.AssignedAt     = DateTime.UtcNow;
        }
        else
        {
            var assignment = new DDFC.Domain.Entities.CadAssignment
            {
                RequestId      = id,
                CadType        = dto.CadType,
                AssignedUserId = dto.AssignedUserId,
                AssignedAt     = DateTime.UtcNow
            };
            _db.CadAssignments.Add(assignment);
        }

        req.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = $"{dto.CadType} CAD operator assigned." });
    }

    /// <summary>CAD operator submits the completed CAD file for a given type.</summary>
    [HttpPost("{id:guid}/cad-files/{cadType}")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> SubmitDeptCadFile(Guid id, string cadType, [FromBody] DeptCadFileDto dto)
    {
        var normalised = cadType.ToLower() switch
        {
            "architecture" => "Architecture",
            "threed"       => "ThreeD",
            "structure"    => "Structure",
            "mep"          => "MEP",
            _              => null
        };
        if (normalised is null)
            return BadRequest(new { message = "Invalid cadType. Use: architecture | threed | structure | mep" });

        var req = await _db.PossessionRequests
            .Include(r => r.CadAssignments)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (req is null) return NotFound();

        var assignment = req.CadAssignments.FirstOrDefault(ca => ca.CadType == normalised);
        if (assignment is null)
            return BadRequest(new { message = $"No CAD operator has been assigned for {normalised} yet." });

        assignment.FileUrl     = dto.FileUrl;
        assignment.FileName    = dto.FileName;
        assignment.FileType    = dto.FileType;
        assignment.CompletedAt = DateTime.UtcNow;
        req.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = $"{normalised} CAD file submitted." });
    }

    // ── Step 5: Package Selection ─────────────────────────────────────────────
    [HttpPost("{id:guid}/package")]
    [Authorize(Policy = "CanSelectPackage")]
    public async Task<IActionResult> SelectPackage(Guid id, [FromBody] SelectPackageDto dto)
    {
        try
        {
            await _svc.SelectPackageAsync(id, dto.PackageId, dto.InteriorDesignPackageId, dto.SupervisionPackageId, GetCurrentUserId());
            return NoContent();
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    // ── Step 5b: Principal Architect – Initial Review ──────────────────────────
    [HttpPost("{id:guid}/principal-architect/initial-review")]
    [Authorize(Policy = "CanPrincipalApprove")]
    public async Task<IActionResult> PAInitialReview(Guid id, [FromBody] PAInitialReviewDto dto)
    {
        try
        {
            SoilTestDto? soilTest = dto.SoilTestFileUrl != null ? new SoilTestDto(
                dto.SoilTestDate ?? DateTime.UtcNow,
                dto.LabName ?? "Unknown",
                dto.SoilBearingCapacity ?? "N/A",
                dto.ResultSummary ?? "",
                dto.SoilTestFileUrl) : null;

            var req = await _svc.PAInitialReviewAsync(id, dto.AssignedArchitectId, soilTest, dto.Notes, GetCurrentUserId());
            return Ok(MapToDto(req));
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // ── Step 6: Payment ────────────────────────────────────────────────────────
    [HttpGet("{id:guid}/payment")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> GetPayment(Guid id)
    {
        var req = await _svc.GetRequestAsync(id);
        var payment = req.Payments.FirstOrDefault(p => p.Status == Domain.Enums.PaymentStatus.Pending)
                   ?? req.Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
        if (payment == null) return NotFound(new { message = "No payment record found" });
        return Ok(new {
            challanNo       = payment.ChallanNo,
            totalAmount     = payment.TotalAmount,
            paidAmount      = payment.PaidAmount,
            status          = payment.Status.ToString(),
            paidAt          = payment.PaidAt,
            scannedFileUrl  = payment.Challan?.FileUrl,
        });
    }

    [HttpGet("{id:guid}/payment/challan/print")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> PrintPaymentChallan(Guid id)
    {
        var req = await _svc.GetRequestAsync(id);
        var payment = req.Payments.FirstOrDefault(p => p.Status == Domain.Enums.PaymentStatus.Pending)
                   ?? req.Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
        var pkg = req.SelectedPackage;
        if (payment == null || pkg == null)
            return BadRequest(new { message = "Payment or package not found" });

        var customer = req.Customer;
        var plot = req.Plot;
        var lineItemRows = string.Join("", pkg.LineItems
            .Where(li => !li.IsFree)
            .OrderBy(li => li.SortOrder)
            .Select(li => $"<tr><td>{li.ServiceName}</td><td class=\"amt\">PKR {li.AmountDDFC:N0}</td></tr>"));
        var freeItems = string.Join(", ", pkg.LineItems.Where(li => li.IsFree).Select(li => li.ServiceName));

        var html = $$"""
<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8"/>
<title>Payment Challan – {{payment.ChallanNo}}</title>
<style>
  * { margin:0; padding:0; box-sizing:border-box; }
  body { font-family: Arial, sans-serif; font-size: 11px; color: #111; background:#fff; }
  .page { width:210mm; padding:10mm 12mm; }
  .header { display:flex; align-items:center; gap:10px; border-bottom:2px solid #1a3c5e; padding-bottom:6px; margin-bottom:8px; }
  .header-text h1 { font-size:16px; color:#1a3c5e; font-weight:900; letter-spacing:1px; }
  .header-text p { font-size:10px; color:#555; }
  .challan-title { text-align:center; font-size:14px; font-weight:bold; letter-spacing:2px;
    background:#1a3c5e; color:#fff; padding:5px; margin-bottom:8px; border-radius:3px; }
  .copy-label { text-align:center; font-size:10px; color:#888; margin-bottom:10px; }
  .section { border:1px solid #ccc; border-radius:4px; margin-bottom:8px; }
  .section-title { background:#f0f4f8; font-weight:bold; font-size:10px; padding:4px 8px;
    border-bottom:1px solid #ccc; color:#1a3c5e; text-transform:uppercase; letter-spacing:.5px; }
  .section-body { padding:6px 8px; }
  .grid2 { display:grid; grid-template-columns:1fr 1fr; gap:4px 16px; }
  .field label { font-size:9px; color:#888; display:block; }
  .field span { font-weight:bold; font-size:11px; }
  table.items { width:100%; border-collapse:collapse; margin-top:4px; }
  table.items th { background:#f0f4f8; font-size:10px; padding:4px 8px; text-align:left; border:1px solid #ccc; }
  table.items td { padding:4px 8px; border:1px solid #ddd; font-size:11px; }
  td.amt { text-align:right; font-weight:bold; }
  .total-row td { font-weight:bold; background:#1a3c5e; color:#fff; }
  .bank-box { background:#fffbe6; border:1px solid #f0c040; border-radius:4px; padding:8px; margin-bottom:8px; }
  .bank-box .bank-title { font-weight:bold; font-size:11px; color:#8a6c00; margin-bottom:4px; }
  .bank-row { display:flex; justify-content:space-between; font-size:11px; margin-bottom:2px; }
  .bank-label { color:#888; }
  .bank-value { font-weight:bold; }
  .sig-row { display:grid; grid-template-columns:1fr 1fr; gap:20px; margin-top:16px; }
  .sig-box { border-top:1px solid #333; padding-top:4px; text-align:center; font-size:10px; color:#555; }
  .footer { margin-top:10px; font-size:9px; color:#999; text-align:center; border-top:1px solid #eee; padding-top:6px; }
  @media print { .page { padding:5mm 8mm; } }
</style>
</head>
<body>
<div class="page">
  <div class="header">
    <div class="header-text">
      <h1>DEFENCE HOUSING AUTHORITY – LAHORE</h1>
      <p>DHA Possession & Construction Workflow System</p>
    </div>
  </div>

  <div class="challan-title">PAYMENT CHALLAN</div>
  <div class="copy-label">BANK COPY / CUSTOMER COPY / OFFICE COPY</div>

  <div class="section">
    <div class="section-title">Challan Information</div>
    <div class="section-body grid2">
      <div class="field"><label>Challan No</label><span>{{payment.ChallanNo}}</span></div>
      <div class="field"><label>Date Issued</label><span>{{DateTime.UtcNow:dd-MMM-yyyy}}</span></div>
      <div class="field"><label>Request ID</label><span>{{req.RequestId}}</span></div>
      <div class="field"><label>File No</label><span>{{req.FileNo}}</span></div>
    </div>
  </div>

  <div class="section">
    <div class="section-title">Applicant Details</div>
    <div class="section-body grid2">
      <div class="field"><label>Name</label><span>{{customer.FullName}}</span></div>
      <div class="field"><label>CNIC</label><span>{{customer.CNIC}}</span></div>
      <div class="field"><label>Phone</label><span>{{customer.PhoneNumber}}</span></div>
      <div class="field"><label>Plot No</label><span>{{plot.PlotNumber}} – Sector {{plot.SectorNo}}, Phase {{plot.PhaseNo}}</span></div>
    </div>
  </div>

  <div class="section">
    <div class="section-title">Package – {{pkg.PackageTier}} Package</div>
    <div class="section-body">
      <table class="items">
        <tr><th>Service</th><th>Amount (DDFC Rate)</th></tr>
        {{lineItemRows}}
        {{(freeItems.Length > 0 ? $"<tr><td colspan='2' style='font-size:10px;color:#777;'>Complimentary: {freeItems}</td></tr>" : "")}}
        <tr class="total-row"><td>TOTAL PAYABLE</td><td class="amt">PKR {{payment.TotalAmount:N0}}</td></tr>
      </table>
    </div>
  </div>

  <div class="bank-box">
    <div class="bank-title">&#127968; Bank Payment Instructions</div>
    <div class="bank-row"><span class="bank-label">Bank Name</span><span class="bank-value">Bank Alfalah (Islamic)</span></div>
    <div class="bank-row"><span class="bank-label">Account Title</span><span class="bank-value">MicroChip Enterprises (Pvt) Ltd.</span></div>
    <div class="bank-row"><span class="bank-label">IBAN</span><span class="bank-value" style="letter-spacing:1px;">PK40ALFH56540050023666769</span></div>
    <div class="bank-row"><span class="bank-label">Amount</span><span class="bank-value" style="font-size:14px;color:#1a3c5e;">PKR {{payment.TotalAmount:N0}}</span></div>
    <div class="bank-row"><span class="bank-label">Reference / Narration</span><span class="bank-value">{{payment.ChallanNo}} / {{req.RequestId}}</span></div>
  </div>

  <div class="sig-row">
    <div class="sig-box">Applicant Signature</div>
    <div class="sig-box">Receptionist Stamp &amp; Signature</div>
  </div>

  <div class="footer">
    This challan is valid for 30 days from the date of issue. Please deposit the exact amount and retain the bank-stamped copy as proof of payment.
  </div>
</div>
<script>window.onload = function(){ window.print(); }</script>
</body>
</html>
""";
        return Content(html, "text/html");
    }

    [HttpPost("{id:guid}/payment/confirm")]
    [Authorize(Policy = "CanConfirmPaymentOrReception")]
    public async Task<IActionResult> ConfirmPayment(Guid id, [FromBody] ConfirmPaymentDto dto)
    {
        try
        {
            await _svc.ConfirmPaymentAsync(id, GetCurrentUserId(), dto.AmountPaid, dto.ChallanNo, dto.ScannedChallanFileUrl);
            return Ok(await _svc.GetRequestAsync(id));
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    // ── Step 7: Architecture ──────────────────────────────────────────────────
    [HttpPost("{id:guid}/plan")]
    [Authorize(Policy = "CanUploadPlan")]
    public async Task<IActionResult> UploadPlan(Guid id, [FromBody] UploadPlanDto dto)
    {
        var plan = await _svc.UploadPlanAsync(id, dto.FileUrl, GetCurrentUserId(), dto.Notes);
        return Ok(plan);
    }

    [HttpPost("{id:guid}/plan/{planId:guid}/approve")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> ApprovePlan(Guid id, Guid planId)
    {
        // Staff approve on customer's behalf — use the request's actual customerId
        var req = await _db.PossessionRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (req is null) return NotFound();
        await _svc.ApprovePlanAsync(id, planId, req.CustomerId);
        return NoContent();
    }

    [HttpPost("{id:guid}/plan/{planId:guid}/revision")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> RequestRevision(Guid id, Guid planId, [FromBody] RevisionDto dto)
    {
        var req = await _db.PossessionRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (req is null) return NotFound();
        var rev = await _svc.RequestPlanRevisionAsync(id, planId, req.CustomerId, dto.Comments, dto.MarkupFileUrl);
        return Ok(rev);
    }

    // ── Step 7b: 3D Visualization ─────────────────────────────────────────────
    [HttpPost("{id:guid}/3d-visualization")]
    [Authorize(Policy = "ArchitectureDepartment")]
    public async Task<IActionResult> UploadThreeDFile(Guid id, [FromBody] ThreeDFileDto dto)
    {
        try
        {
            var req = await _svc.UploadThreeDFileAsync(id, dto, GetCurrentUserId());
            return Ok(req);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id:guid}/3d-visualization/complete")]
    [Authorize(Policy = "ArchitectureDepartment")]
    public async Task<IActionResult> CompleteThreeD(Guid id)
    {
        try
        {
            await _svc.CompleteThreeDAsync(id, GetCurrentUserId());
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // ── Step 7c: CAD Files ────────────────────────────────────────────────────
    [HttpPost("{id:guid}/cad-files")]
    [Authorize(Policy = "ArchitectureDepartment")]
    public async Task<IActionResult> UploadCadFile(Guid id, [FromBody] CadFileDto dto)
    {
        try
        {
            var req = await _svc.UploadCadFileAsync(id, dto, GetCurrentUserId());
            return Ok(req);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id:guid}/cad-files/complete")]
    [Authorize(Policy = "ArchitectureDepartment")]
    public async Task<IActionResult> CompleteCad(Guid id)
    {
        try
        {
            await _svc.CompleteCadAsync(id, GetCurrentUserId());
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // ── Step 8 / 9: Structure & MEP ───────────────────────────────────────────
    [HttpPost("{id:guid}/structure/complete")]
    [Authorize(Policy = "CanCompleteStructure")]
    public async Task<IActionResult> CompleteStructure(Guid id, [FromBody] ReportDto dto)
    {
        await _svc.CompleteStructureAsync(id, dto.FileUrl, dto.Observations, GetCurrentUserId());
        return NoContent();
    }

    [HttpPost("{id:guid}/mep/complete")]
    [Authorize(Policy = "CanCompleteMEP")]
    public async Task<IActionResult> CompleteMEP(Guid id, [FromBody] ReportDto dto)
    {
        await _svc.CompleteMEPAsync(id, dto.FileUrl, dto.Observations, GetCurrentUserId());
        return NoContent();
    }

    // ── Step 10: Principal Architect ──────────────────────────────────────────
    [HttpPost("{id:guid}/principal-review/approve")]
    [Authorize(Policy = "CanPrincipalApprove")]
    public async Task<IActionResult> PrincipalApprove(Guid id, [FromBody] ActionNoteDto note)
    {
        await _svc.PrincipalApproveAsync(id, GetCurrentUserId(), approved: true, note.Comments);
        return NoContent();
    }

    [HttpPost("{id:guid}/principal-review/send-back")]
    [Authorize(Policy = "CanPrincipalApprove")]
    public async Task<IActionResult> PrincipalSendBack(Guid id, [FromBody] ActionNoteDto note)
    {
        await _svc.PrincipalApproveAsync(id, GetCurrentUserId(), approved: false, note.Comments);
        return NoContent();
    }

    [HttpPost("{id:guid}/soil-test")]
    [Authorize(Policy = "CanUploadSoilTest")]
    public async Task<IActionResult> UploadSoilTest(Guid id, [FromBody] SoilTestDto dto)
    {
        var report = await _svc.UploadSoilTestAsync(id, dto, GetCurrentUserId());
        return Ok(report);
    }

    // ── Step 12: Building Control ─────────────────────────────────────────────
    [HttpPost("{id:guid}/building-control")]
    [Authorize(Policy = "CanSubmitBuildingControl")]
    public async Task<IActionResult> SubmitBuildingControl(Guid id, [FromBody] BuildingControlDto dto)
    {
        await _svc.SubmitBuildingControlAsync(id, dto, GetCurrentUserId());
        return NoContent();
    }

    // ── Reject (from Submitted or any stage) ────────────────────────────────
    [HttpPost("{id:guid}/initiate")]
    [Authorize(Policy = "ReceptionOfficer")]
    public async Task<IActionResult> InitiateRequest(Guid id, [FromBody] ActionNoteDto note)
    {
        await _svc.InitiateRequestAsync(id, GetCurrentUserId(), note.Comments);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "ReceptionOfficer")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ActionNoteDto note)
    {
        await _svc.RejectRequestAsync(id, GetCurrentUserId(), note.Comments);
        return NoContent();
    }

    // ── Admin Review: attach documents + Initiate or Reject ───────────────────
    [HttpPost("{id:guid}/admin-review")]
    [Authorize(Policy = "CanAdminReview")]
    public async Task<IActionResult> AdminReview(Guid id, [FromBody] AdminReviewDto dto)
    {
        try
        {
            await _svc.AdminReviewAsync(id, dto, GetCurrentUserId());
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── Step 13: Final Approval ───────────────────────────────────────────────
    [HttpPost("{id:guid}/final-approval/approve")]
    [Authorize(Policy = "CanFinalApprove")]
    public async Task<IActionResult> FinalApprove(Guid id, [FromBody] ActionNoteDto note)
    {
        await _svc.FinalApproveAsync(id, GetCurrentUserId(), note.Comments);
        return NoContent();
    }

    [HttpPost("{id:guid}/final-approval/reject")]
    [Authorize(Policy = "CanFinalApprove")]
    public async Task<IActionResult> FinalReject(Guid id, [FromBody] ActionNoteDto note)
    {
        await _svc.FinalApproveAsync(id, GetCurrentUserId(), $"REJECTED: {note.Comments}");
        return NoContent();
    }

    // ── Step 14: Delivery ─────────────────────────────────────────────────────
    [HttpPost("{id:guid}/deliver")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> Deliver(Guid id, [FromBody] ActionNoteDto note)
    {
        await _svc.DeliverDocumentsAsync(id, GetCurrentUserId(), DDFC.Domain.Enums.SurveyLanguage.EN);
        return NoContent();
    }

    // ── History ───────────────────────────────────────────────────────────────
    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> GetHistory(Guid id)
    {
        var history = await _db.RequestWorkflowHistories
            .Where(h => h.RequestId == id)
            .OrderBy(h => h.Timestamp)
            .ToListAsync();
        return Ok(history);
    }

    // ── Survey Observations ───────────────────────────────────────────────────
    [HttpGet("{id:guid}/surveys")]
    public async Task<IActionResult> GetSurveyObservations(Guid id)
    {
        var observations = await _db.SurveyObservations
            .Include(o => o.Department)
            .Where(o => o.RequestId == id)
            .OrderBy(o => o.SurveyDate)
            .ToListAsync();
        return Ok(observations);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static PossessionRequestDto MapToDto(PossessionRequest r, IEnumerable<string>? activeStepNames = null) => new(
        RequestId:               r.RequestId,
        Id:                      r.Id,
        CustomerId:              r.CustomerId,
        CustomerName:            r.Customer != null ? r.Customer.FullName : null,
        Cnic:                    r.Customer != null ? r.Customer.CNIC : null,
        PhoneNumber:             r.Customer != null ? r.Customer.PhoneNumber : null,
        PlotId:                  r.PlotId,
        PlotNumber:              r.Plot != null ? r.Plot.PlotNumber : null,
        SectorNo:                r.Plot != null ? r.Plot.SectorNo : null,
        PhaseNo:                 r.Plot != null ? r.Plot.PhaseNo : null,
        PlotType:                r.Plot != null ? r.Plot.PlotType.ToString() : null,
        PlotSize:                r.Plot != null ? r.Plot.PlotSize.ToString() : null,
        FileNo:                  r.FileNo,
        MembershipDPRNo:         r.MembershipDPRNo,
        OwnerTitle:              r.OwnerTitle,
        OwnerName:               r.OwnerName,
        GuardianName:            r.GuardianName,
        GuardianRelation:        r.GuardianRelation,
        Contractor:              r.Contractor,
        AllotmentLetterUrl:      r.AllotmentLetterUrl,
        CnicUrl:                 r.CnicUrl,
        AdminRejectionReason:    r.AdminRejectionReason,
        AdminReviewedAt:         r.AdminReviewedAt,
        MessageScreenshotUrl:    r.MessageScreenshotUrl,
        EStampPaperUrl:          r.EStampPaperUrl,
        AuthorizedPersonCnicUrl: r.AuthorizedPersonCnicUrl,
        AuthorizedPersonPhone:   r.AuthorizedPersonPhone,
        Status:                  r.Status.ToString(),
        SubmittedAt:             r.SubmittedAt,
        UpdatedAt:               r.UpdatedAt,
        TransferApproved:        r.TransferApproved,
        FinanceApproved:         r.FinanceApproved,
        AdcAmount:               r.AdcAmount,
        StructureCompleted:      r.StructureCompleted,
        MepCompleted:            r.MEPCompleted,
        TownPlanningCompleted:   r.TownPlanningCompleted,
        BuildingControlCompleted: r.BuildingControlCompleted,
        WorkflowHistory:         r.WorkflowHistory
            .OrderBy(h => h.Timestamp)
            .Select(h => new WorkflowHistoryDto(
                HistoryId:  h.Id.ToString(),
                RequestId:  h.RequestId.ToString(),
                FromStatus: h.FromStatus,
                ToStatus:   h.ToStatus,
                ActionBy:   h.Action,
                ActorName:  h.ActionByUser != null ? h.ActionByUser.FullName : null,
                Comments:   h.Comments,
                Timestamp:  h.Timestamp
            )),
        ActiveWorkflowStepNames: activeStepNames ?? Enumerable.Empty<string>(),
        AssignedArchitectId:   r.AssignedArchitectId,
        AssignedArchitectName: r.AssignedArchitect?.FullName,
        PackageTier:     r.SelectedPackage?.PackageTier.ToString(),
        PackageTotal:    (r.SelectedPackage?.LineItems.Where(li => !li.IsFree).Sum(li => li.AmountDDFC) ?? 0)
                         + (r.SelectedInteriorDesignPackage?.LineItems.Where(li => !li.IsFree).Sum(li => li.AmountDDFC) ?? 0)
                         + (r.SelectedSupervisionPackage?.LineItems.Where(li => !li.IsFree).Sum(li => li.AmountDDFC) ?? 0)
                         is > 0 ? (decimal?)(
                             (r.SelectedPackage?.LineItems.Where(li => !li.IsFree).Sum(li => li.AmountDDFC) ?? 0)
                             + (r.SelectedInteriorDesignPackage?.LineItems.Where(li => !li.IsFree).Sum(li => li.AmountDDFC) ?? 0)
                             + (r.SelectedSupervisionPackage?.LineItems.Where(li => !li.IsFree).Sum(li => li.AmountDDFC) ?? 0))
                         : null,
        SelectedInteriorDesignPackageId: r.SelectedInteriorDesignPackageId,
        InteriorDesignPackageTier:       r.SelectedInteriorDesignPackage?.PackageTier.ToString(),
        InteriorDesignPackageTotal:      r.SelectedInteriorDesignPackage?.LineItems.Where(li => !li.IsFree).Sum(li => li.AmountDDFC),
        SelectedSupervisionPackageId:    r.SelectedSupervisionPackageId,
        SupervisionPackageTier:          r.SelectedSupervisionPackage?.PackageTier.ToString(),
        SupervisionPackageTotal:         r.SelectedSupervisionPackage?.LineItems.Where(li => !li.IsFree).Sum(li => li.AmountDDFC),
        ChallanNo:       r.Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault()?.ChallanNo,
        PaymentStatus:   r.Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault()?.Status.ToString(),
        SelectedPackageId:   r.SelectedPackageId,
        SelectedDesignType:  r.SelectedPackage?.DesignType.ToString(),
        RequestType:         r.RequestType.ToString(),
        LinkedPossessionRequestId: null, // not stored on entity; informational only
        DelayUndertaking: r.DelayUndertaking == null ? null : new DelayUndertakingDto(
            r.DelayUndertaking.Id,
            r.DelayUndertaking.InitiatedBy.ToString(),
            r.DelayUndertaking.RequestedByUserId,
            r.DelayUndertaking.RequestedAt,
            r.DelayUndertaking.SignedByCustomerId,
            r.DelayUndertaking.SignedAt,
            r.DelayUndertaking.DelayReason,
            r.DelayUndertaking.ExpectedDelayDays,
            r.DelayUndertaking.UndertakingDocumentUrl,
            r.DelayUndertaking.Notes),
        ArchitectUndertaking: r.ArchitectUndertaking == null ? null : new ArchitectUndertakingDto(
            r.ArchitectUndertaking.Id,
            r.ArchitectUndertaking.SignedDocumentUrl,
            r.ArchitectUndertaking.HoldDays,
            r.ArchitectUndertaking.HoldStartDate,
            r.ArchitectUndertaking.HoldEndDate,
            r.ArchitectUndertaking.SignedAt,
            r.ArchitectUndertaking.Notes),
        PlotAnnexation: r.PlotAnnexation == null ? null : new PlotAnnexationDto(
            r.PlotAnnexation.Id,
            r.PlotAnnexation.AdditionalArea,
            r.PlotAnnexation.AnnexationFee,
            r.PlotAnnexation.Notes,
            r.PlotAnnexation.CreatedAt,
            r.PlotAnnexation.ApprovedAt,
            r.PlotAnnexation.DocumentUrl),
        PlotMerging: r.PlotMerging == null ? null : new PlotMergingDto(
            r.PlotMerging.Id,
            r.PlotMerging.MergedPlotNumber,
            r.PlotMerging.MergedPlotSector,
            r.PlotMerging.MergedPlotSize,
            r.PlotMerging.Notes,
            r.PlotMerging.CreatedAt,
            r.PlotMerging.ApprovedAt,
            r.PlotMerging.DocumentUrl),
        CadAssignments: r.CadAssignments.Select(ca => new CadAssignmentDto(
            ca.Id,
            ca.CadType,
            ca.AssignedUserId,
            ca.AssignedUser?.FullName,
            ca.AssignedAt,
            ca.FileUrl,
            ca.FileName,
            ca.FileType,
            ca.CompletedAt)),
        Documents:       r.Documents.Where(d => !d.IsArchived)
            .Select(d => new RequestDocumentDto(d.Id.ToString(), d.DocType, d.FileUrl, d.UploadedAt))
            .Concat(r.ArchitecturalPlans
                .OrderBy(p => p.Version)
                .Select(p => new RequestDocumentDto(
                    p.Id.ToString(),
                    $"Architectural Plan v{p.Version}" + (p.CustomerApproved ? " \u2713" : ""),
                    p.FileUrl, p.UploadedAt)))
            .Concat(r.StructuralReports
                .Select(s => new RequestDocumentDto(s.Id.ToString(), "Structural Report", s.FileUrl, s.CompletedAt)))
            .Concat(r.MEPReports
                .Select(m => new RequestDocumentDto(m.Id.ToString(), "MEP Report", m.FileUrl, m.CompletedAt)))
            .Concat(r.SoilTestReport != null
                ? new[] { new RequestDocumentDto(r.SoilTestReport.Id.ToString(), "Soil Test Report", r.SoilTestReport.ReportFileUrl, r.SoilTestReport.UploadedAt) }
                : Array.Empty<RequestDocumentDto>())
            .OrderBy(d => d.UploadedAt)
    );

    private Guid GetCurrentUserId()
    {
        var v = User.FindFirst("userId")?.Value ?? User.FindFirst("customerId")?.Value
            ?? throw new InvalidOperationException("No user claim");
        return Guid.Parse(v);
    }
}

// ── Request body models ───────────────────────────────────────────────────────
public record ActionNoteDto(string? Comments);
public record FinanceApproveDto(string? Comments, decimal? AdcAmount);
// IssueCertDto is defined in DDFC.Application.Interfaces
public record SelectPackageDto(Guid PackageId, Guid? InteriorDesignPackageId, Guid? SupervisionPackageId);
public record ConfirmPaymentDto(string? ChallanNo, decimal AmountPaid, string? ScannedChallanFileUrl);
public record UploadPlanDto(string FileUrl, string? Notes);
public record PAInitialReviewDto(
    Guid      AssignedArchitectId,
    string?   SoilTestFileUrl,
    DateTime? SoilTestDate,
    string?   LabName,
    string?   SoilBearingCapacity,
    string?   ResultSummary,
    string?   Notes);
public record RevisionDto(string Comments, string? MarkupFileUrl);
public record ReportDto(string FileUrl, string? Observations);
public record RequestDelayUndertakingDto(string? DelayReason, int? ExpectedDelayDays, string? Notes);
public record SignDelayUndertakingRequestDto(string? UndertakingDocumentUrl, string? Notes);

// ── Response DTOs ─────────────────────────────────────────────────────────────
public record WorkflowHistoryDto(
    string    HistoryId,
    string    RequestId,
    string?   FromStatus,
    string    ToStatus,
    string?   ActionBy,
    string?   ActorName,
    string?   Comments,
    DateTime  Timestamp
);

public record PossessionRequestDto(
    string   RequestId,
    Guid     Id,
    Guid     CustomerId,
    string?  CustomerName,
    string?  Cnic,
    string?  PhoneNumber,
    Guid     PlotId,
    string?  PlotNumber,
    string?  SectorNo,
    string?  PhaseNo,
    string?  PlotType,
    string?  PlotSize,
    string   FileNo,
    string   MembershipDPRNo,
    string   OwnerTitle,
    string   OwnerName,
    string   GuardianName,
    string   GuardianRelation,
    string?  Contractor,
    string?  AllotmentLetterUrl,
    string?  CnicUrl,
    string?  AdminRejectionReason,
    DateTime? AdminReviewedAt,
    string?  MessageScreenshotUrl,
    string?  EStampPaperUrl,
    string?  AuthorizedPersonCnicUrl,
    string?  AuthorizedPersonPhone,
    string   Status,
    DateTime SubmittedAt,
    DateTime? UpdatedAt,
    bool     TransferApproved,
    bool     FinanceApproved,
    decimal? AdcAmount,
    bool     StructureCompleted,
    bool     MepCompleted,
    bool     TownPlanningCompleted,
    bool     BuildingControlCompleted,
    IEnumerable<WorkflowHistoryDto> WorkflowHistory,
    IEnumerable<string> ActiveWorkflowStepNames,
    Guid?    AssignedArchitectId,
    string?  AssignedArchitectName,
    string?  PackageTier,
    decimal? PackageTotal,
    Guid?    SelectedInteriorDesignPackageId,
    string?  InteriorDesignPackageTier,
    decimal? InteriorDesignPackageTotal,
    Guid?    SelectedSupervisionPackageId,
    string?  SupervisionPackageTier,
    decimal? SupervisionPackageTotal,
    string?  ChallanNo,
    string?  PaymentStatus,
    Guid?    SelectedPackageId,
    string?  SelectedDesignType,
    string   RequestType,
    Guid?    LinkedPossessionRequestId,
    DelayUndertakingDto? DelayUndertaking,
    ArchitectUndertakingDto? ArchitectUndertaking,
    PlotAnnexationDto? PlotAnnexation,
    PlotMergingDto? PlotMerging,
    IEnumerable<CadAssignmentDto> CadAssignments,
    IEnumerable<RequestDocumentDto> Documents
);

public record DelayUndertakingDto(
    Guid     Id,
    string   InitiatedBy,
    Guid?    RequestedByUserId,
    DateTime? RequestedAt,
    Guid?    SignedByCustomerId,
    DateTime? SignedAt,
    string?  DelayReason,
    int?     ExpectedDelayDays,
    string?  UndertakingDocumentUrl,
    string?  Notes
);

public record RequestDocumentDto(
    string   DocumentId,
    string   DocumentType,
    string   FileUrl,
    DateTime UploadedAt
);

// ── New request body DTOs ─────────────────────────────────────────────────────
public record AttachUndertakingDto(string SignedDocumentUrl, int HoldDays, string? Notes);
public record AnnexationDto(string? AdditionalArea, decimal AnnexationFee, string? Notes);
public record PlotMergeDto(string MergedPlotNumber, string MergedPlotSector, string? MergedPlotSize, string? Notes);
public record CadAssignmentRequestDto(string CadType, Guid AssignedUserId);
public record DeptCadFileDto(string FileUrl, string? FileName, string? FileType);

// ── New response DTOs ─────────────────────────────────────────────────────────
public record ArchitectUndertakingDto(
    Guid      Id,
    string?   SignedDocumentUrl,
    int?      HoldDays,
    DateTime? HoldStartDate,
    DateTime? HoldEndDate,
    DateTime? SignedAt,
    string?   Notes
);

public record PlotAnnexationDto(
    Guid      Id,
    string?   AdditionalArea,
    decimal   AnnexationFee,
    string?   Notes,
    DateTime  RequestedAt,
    DateTime? ApprovedAt,
    string?   DocumentUrl
);

public record PlotMergingDto(
    Guid      Id,
    string    MergedPlotNumber,
    string    MergedPlotSector,
    string?   MergedPlotSize,
    string?   Notes,
    DateTime  RequestedAt,
    DateTime? ApprovedAt,
    string?   DocumentUrl
);

public record CadAssignmentDto(
    Guid      Id,
    string    CadType,
    Guid      AssignedUserId,
    string?   AssignedUserName,
    DateTime  AssignedAt,
    string?   FileUrl,
    string?   FileName,
    string?   FileType,
    DateTime? CompletedAt
);
