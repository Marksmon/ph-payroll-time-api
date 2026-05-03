using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Application.Commands.Approvals;
using PhPayrollTimeApi.Application.Dtos;
using PhPayrollTimeApi.Application.Queries.GetHolidayApprovalQueue;
using PhPayrollTimeApi.Application.Queries.GetOtApprovalQueue;
using PhPayrollTimeApi.Application.Queries.GetRestDayApprovalQueue;
using PhPayrollTimeApi.Api.Constants;
using PhPayrollTimeApi.Domain.Enums;

namespace PhPayrollTimeApi.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/approvals")]
[Authorize(Roles = "MANAGER")]
[EnableRateLimiting("bulk")]
public class ApprovalsController : ControllerBase
{
    private readonly IQueryHandler<GetHolidayApprovalQueueQuery, List<HolidayScheduleApprovalDto>> _getHolidayQueue;
    private readonly ICommandHandler<BulkApproveHolidayScheduleCommand> _bulkApproveHoliday;
    private readonly IQueryHandler<GetRestDayApprovalQueueQuery, List<RestDayScheduleApprovalDto>> _getRestDayQueue;
    private readonly ICommandHandler<BulkApproveRestDayScheduleCommand> _bulkApproveRestDay;
    private readonly IQueryHandler<GetOtApprovalQueueQuery, List<OtApprovalDto>> _getOtQueue;
    private readonly ICommandHandler<StageOtActionCommand> _stageOtAction;
    private readonly ICommandHandler<RemoveStagedOtActionCommand> _removeStagedAction;
    private readonly ICommandHandler<CommitOtApprovalsCommand> _commitOt;

    public ApprovalsController(
        IQueryHandler<GetHolidayApprovalQueueQuery, List<HolidayScheduleApprovalDto>> getHolidayQueue,
        ICommandHandler<BulkApproveHolidayScheduleCommand> bulkApproveHoliday,
        IQueryHandler<GetRestDayApprovalQueueQuery, List<RestDayScheduleApprovalDto>> getRestDayQueue,
        ICommandHandler<BulkApproveRestDayScheduleCommand> bulkApproveRestDay,
        IQueryHandler<GetOtApprovalQueueQuery, List<OtApprovalDto>> getOtQueue,
        ICommandHandler<StageOtActionCommand> stageOtAction,
        ICommandHandler<RemoveStagedOtActionCommand> removeStagedAction,
        ICommandHandler<CommitOtApprovalsCommand> commitOt)
    {
        _getHolidayQueue = getHolidayQueue;
        _bulkApproveHoliday = bulkApproveHoliday;
        _getRestDayQueue = getRestDayQueue;
        _bulkApproveRestDay = bulkApproveRestDay;
        _getOtQueue = getOtQueue;
        _stageOtAction = stageOtAction;
        _removeStagedAction = removeStagedAction;
        _commitOt = commitOt;
    }

    // ── Holiday Schedule Approvals ────────────────────────────────────────────

    [HttpGet("holiday-schedules")]
    public async Task<IActionResult> GetHolidayApprovals([FromQuery] string status = "PENDING", CancellationToken ct = default)
    {
        if (!Enum.TryParse<ApprovalStatus>(status, true, out var statusEnum))
            return BadRequestProblem("Invalid status value. Use PENDING, APPROVED, or REJECTED.");

        var result = await _getHolidayQueue.HandleAsync(new GetHolidayApprovalQueueQuery(statusEnum), ct);
        return Ok(result);
    }

    [HttpPost("holiday-schedules/bulk-action")]
    public async Task<IActionResult> BulkActionHolidayApprovals(
        [FromBody] BulkApprovalActionRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<ApprovalStatus>(req.Action, true, out var action) ||
            action == ApprovalStatus.PENDING)
            return BadRequestProblem("Action must be APPROVED or REJECTED.");

        await _bulkApproveHoliday.HandleAsync(new BulkApproveHolidayScheduleCommand(
            req.ApprovalIds, action, CallerSub()), ct);
        return Ok(new { count = req.ApprovalIds.Count, action = action.ToString() });
    }

    // ── Rest Day Schedule Approvals ───────────────────────────────────────────

    [HttpGet("rest-day-schedules")]
    public async Task<IActionResult> GetRestDayApprovals([FromQuery] string status = "PENDING", CancellationToken ct = default)
    {
        if (!Enum.TryParse<ApprovalStatus>(status, true, out var statusEnum))
            return BadRequestProblem("Invalid status value.");

        var result = await _getRestDayQueue.HandleAsync(new GetRestDayApprovalQueueQuery(statusEnum), ct);
        return Ok(result);
    }

    [HttpPost("rest-day-schedules/bulk-action")]
    public async Task<IActionResult> BulkActionRestDayApprovals(
        [FromBody] BulkApprovalActionRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<ApprovalStatus>(req.Action, true, out var action) ||
            action == ApprovalStatus.PENDING)
            return BadRequestProblem("Action must be APPROVED or REJECTED.");

        await _bulkApproveRestDay.HandleAsync(new BulkApproveRestDayScheduleCommand(
            req.ApprovalIds, action, CallerSub()), ct);
        return Ok(new { count = req.ApprovalIds.Count, action = action.ToString() });
    }

    // ── OT Approvals ──────────────────────────────────────────────────────────

    [HttpGet("ot")]
    public async Task<IActionResult> GetOtApprovals(CancellationToken ct)
    {
        var result = await _getOtQueue.HandleAsync(new GetOtApprovalQueueQuery(), ct);
        return Ok(result);
    }

    [HttpPost("ot/{id:guid}/staged-actions")]
    public async Task<IActionResult> StageOtAction(Guid id, [FromBody] StageOtActionRequest req, CancellationToken ct)
    {
        Domain.Enums.TimeSegmentClassification? classification = null;
        if (req.SegmentClassification is not null &&
            Enum.TryParse<Domain.Enums.TimeSegmentClassification>(req.SegmentClassification, out var parsed))
            classification = parsed;

        Domain.Enums.StagedOtActionType actionType;
        if (!Enum.TryParse<Domain.Enums.StagedOtActionType>(req.ActionType, true, out actionType))
            return BadRequestProblem("Invalid actionType.");

        await _stageOtAction.HandleAsync(new StageOtActionCommand(
            id, actionType, classification, req.AdjustedDurationMinutes, req.Reason), ct);
        return Ok();
    }

    [HttpDelete("ot/staged-actions/{actionId:guid}")]
    public async Task<IActionResult> RemoveStagedAction(Guid actionId, CancellationToken ct)
    {
        await _removeStagedAction.HandleAsync(new RemoveStagedOtActionCommand(actionId), ct);
        return NoContent();
    }

    [HttpPost("ot/commit")]
    public async Task<IActionResult> CommitOtApprovals([FromBody] CommitOtApprovalsRequest req, CancellationToken ct)
    {
        await _commitOt.HandleAsync(new CommitOtApprovalsCommand(req.OtApprovalIds, CallerSub()), ct);
        return Ok(new { count = req.OtApprovalIds.Count });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string CallerSub() => User.FindFirstValue("sub") ?? "unknown";

    private IActionResult BadRequestProblem(string detail) =>
        new ContentResult
        {
            StatusCode = 400,
            ContentType = "application/problem+json",
            Content = System.Text.Json.JsonSerializer.Serialize(new
            {
                type = ProblemTypes.Validation,
                title = "Bad Request",
                status = 400,
                detail
            })
        };
}

// ── Request models ────────────────────────────────────────────────────────────

public record BulkApprovalActionRequest(IReadOnlyList<Guid> ApprovalIds, string Action);

public record StageOtActionRequest(
    string ActionType,
    string? SegmentClassification,
    int? AdjustedDurationMinutes,
    string? Reason);

public record CommitOtApprovalsRequest(IReadOnlyList<Guid> OtApprovalIds);
