using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Enums;
using PhPayrollTimeApi.Domain.Interfaces;
using PhPayrollTimeApi.Domain.ValueObjects;

namespace PhPayrollTimeApi.Domain.Services;

public class ComputationEngine
{
    private static readonly TimeSpan ManilaOffset = TimeSpan.FromHours(8);
    private const string HolidayPayFlag = "HolidayPayWithinScheduledHours";

    // Night diff window: 10pm–6am Manila = 22:00–06:00 local
    private static readonly TimeOnly NightDiffStart = new(22, 0);
    private static readonly TimeOnly NightDiffEnd = new(6, 0);

    private readonly IHolidayCalendar _holidayCalendar;
    private readonly IClockProvider _clock;
    private readonly IFeatureFlagProvider _featureFlags;
    private readonly ILogClaimTracker _logClaimTracker;
    private readonly IHolidayApprovalRepository _approvalRepository;

    public ComputationEngine(
        IHolidayCalendar holidayCalendar,
        IClockProvider clock,
        IFeatureFlagProvider featureFlags,
        ILogClaimTracker logClaimTracker,
        IHolidayApprovalRepository approvalRepository)
    {
        _holidayCalendar = holidayCalendar;
        _clock = clock;
        _featureFlags = featureFlags;
        _logClaimTracker = logClaimTracker;
        _approvalRepository = approvalRepository;
    }

    public async Task<ComputationResult> ComputeAsync(
        ShiftSchedule schedule,
        WorkSchedulePattern? workPattern,
        IReadOnlyList<TimeLog> logs,
        CancellationToken ct)
    {
        var scheduleStartManila = ToManila(schedule.ScheduleStart);
        var shiftDate = DateOnly.FromDateTime(scheduleStartManila.DateTime);

        var holidayType = await _holidayCalendar.GetHolidayTypeAsync(shiftDate, ct);
        var isRestDay = workPattern is not null && IsRestDay(workPattern, shiftDate);
        var holidayApproval = await _approvalRepository.GetHolidayScheduleApprovalAsync(schedule.Id, ct);
        var restDayApproval = await _approvalRepository.GetRestDayScheduleApprovalAsync(schedule.Id, ct);
        var holidayPayEnabled = await _featureFlags.IsEnabledAsync(HolidayPayFlag, ct);

        var isHolidayApproved = holidayApproval?.Status == ApprovalStatus.APPROVED;
        var isRestDayApproved = restDayApproval?.Status == ApprovalStatus.APPROVED;

        var pairing = new LogPairingService(_clock, _logClaimTracker);
        var (inLog, outLog) = pairing.PairLogs(schedule, logs);

        bool hrReview = logs.Count > 0;

        // ── Terminal states when no IN found ─────────────────────────────────
        if (inLog is null)
        {
            // Regular Holiday + no approved schedule + no logs → 100% paid rest
            if (holidayType == HolidayType.REGULAR && !isHolidayApproved && !hrReview)
            {
                var seg = new TimeSegment(
                    schedule.ScheduleStart, schedule.ScheduleEnd,
                    ScheduleMinutes(schedule),
                    TimeSegmentClassification.REGULAR_HOLIDAY_REST_PAID,
                    ApprovalStatus.PENDING);
                return new ComputationResult(schedule.Id, schedule.EmployeeId,
                    schedule.ScheduleStart, schedule.ScheduleEnd,
                    null, null, 0, new[] { seg }, false);
            }

            // Special Non-Working + no approved schedule + no logs → no result
            if (holidayType == HolidayType.SPECIAL_NON_WORKING && !isHolidayApproved && !hrReview)
            {
                return new ComputationResult(schedule.Id, schedule.EmployeeId,
                    schedule.ScheduleStart, schedule.ScheduleEnd,
                    null, null, 0, Array.Empty<TimeSegment>(), false);
            }

            // Otherwise ABSENT
            var absentSeg = new TimeSegment(
                schedule.ScheduleStart, schedule.ScheduleEnd,
                ScheduleMinutes(schedule),
                TimeSegmentClassification.ABSENT, ApprovalStatus.PENDING);
            return new ComputationResult(schedule.Id, schedule.EmployeeId,
                schedule.ScheduleStart, schedule.ScheduleEnd,
                null, null, 0, new[] { absentSeg }, false);
        }

        // ── No OUT found ──────────────────────────────────────────────────────
        if (outLog is null)
        {
            if (_clock.UtcNow < schedule.ScheduleEnd)
            {
                // In progress — partial clocked time
                var nowUtc = _clock.UtcNow;
                var clockedMins = (int)(nowUtc - inLog.LoggedAt).TotalMinutes;
                if (clockedMins < 1) clockedMins = 1;
                var seg = new TimeSegment(
                    inLog.LoggedAt, nowUtc,
                    clockedMins,
                    TimeSegmentClassification.IS_IN_CURRENT_SCHEDULE,
                    ApprovalStatus.PENDING);
                return new ComputationResult(schedule.Id, schedule.EmployeeId,
                    schedule.ScheduleStart, schedule.ScheduleEnd,
                    inLog.LoggedAt, null, 0, new[] { seg }, hrReview);
            }
            else
            {
                var absentSeg = new TimeSegment(
                    schedule.ScheduleStart, schedule.ScheduleEnd,
                    ScheduleMinutes(schedule),
                    TimeSegmentClassification.ABSENT, ApprovalStatus.PENDING);
                return new ComputationResult(schedule.Id, schedule.EmployeeId,
                    schedule.ScheduleStart, schedule.ScheduleEnd,
                    inLog.LoggedAt, null, 0, new[] { absentSeg }, hrReview);
            }
        }

        // ── Both IN and OUT found — full classification ───────────────────────
        var approvalStatus = DetermineApprovalStatus(
            holidayType, isRestDay, isHolidayApproved, isRestDayApproved);

        var segments = BuildSegments(
            schedule, inLog.LoggedAt, outLog.LoggedAt,
            holidayType, isRestDay, holidayPayEnabled, approvalStatus,
            out int breakDeductionMinutes);

        return new ComputationResult(
            schedule.Id, schedule.EmployeeId,
            schedule.ScheduleStart, schedule.ScheduleEnd,
            inLog.LoggedAt, outLog.LoggedAt,
            breakDeductionMinutes, segments, hrReview);
    }

    // ── Segment building ──────────────────────────────────────────────────────

    private List<TimeSegment> BuildSegments(
        ShiftSchedule schedule,
        DateTimeOffset clockIn,
        DateTimeOffset clockOut,
        HolidayType holidayType,
        bool isRestDay,
        bool holidayPayEnabled,
        ApprovalStatus approvalStatus,
        out int breakDeductionMinutes)
    {
        var rawSegments = new List<(DateTimeOffset Start, DateTimeOffset End, SegmentKind Kind)>();

        var schedStart = schedule.ScheduleStart;
        var schedEnd = schedule.ScheduleEnd;

        // Early OT: clocked in before schedule start
        if (clockIn < schedStart)
            rawSegments.Add((clockIn, schedStart, SegmentKind.EarlyOt));

        // Scheduled hours: overlap of clocked interval with schedule window, split around breaks
        var paidStart = clockIn > schedStart ? clockIn : schedStart;
        var paidEnd = clockOut < schedEnd ? clockOut : schedEnd;

        if (paidStart < paidEnd)
        {
            var paidIntervals = SubtractBreaks(paidStart, paidEnd, schedule.BreakWindows, out breakDeductionMinutes);
            foreach (var (ivStart, ivEnd) in paidIntervals)
                rawSegments.Add((ivStart, ivEnd, SegmentKind.PaidHours));
        }
        else
        {
            breakDeductionMinutes = 0;
        }

        // Late OT: clocked out after schedule end
        if (clockOut > schedEnd)
            rawSegments.Add((schedEnd, clockOut, SegmentKind.NormalOt));

        // Classify each raw segment with night diff splitting
        var result = new List<TimeSegment>();
        foreach (var (start, end, kind) in rawSegments)
        {
            var subSegments = SplitNightDiff(start, end);
            foreach (var (subStart, subEnd, isNightDiff) in subSegments)
            {
                var mins = (int)(subEnd - subStart).TotalMinutes;
                if (mins <= 0) continue;

                var classification = ClassifySegment(
                    kind, isNightDiff, holidayType, isRestDay, holidayPayEnabled);

                if (classification is null) continue; // suppressed by feature flag

                result.Add(new TimeSegment(subStart, subEnd, mins, classification.Value, approvalStatus));
            }
        }

        return result;
    }

    private static List<(DateTimeOffset Start, DateTimeOffset End)> SubtractBreaks(
        DateTimeOffset windowStart, DateTimeOffset windowEnd,
        IEnumerable<BreakWindow> breaks, out int deductionMinutes)
    {
        var clipped = breaks
            .Select(bw => (
                Start: bw.BreakStart > windowStart ? bw.BreakStart : windowStart,
                End: bw.BreakEnd < windowEnd ? bw.BreakEnd : windowEnd))
            .Where(b => b.End > b.Start)
            .OrderBy(b => b.Start)
            .ToList();

        deductionMinutes = clipped.Sum(b => (int)(b.End - b.Start).TotalMinutes);

        var result = new List<(DateTimeOffset, DateTimeOffset)>();
        var cursor = windowStart;
        foreach (var (bStart, bEnd) in clipped)
        {
            if (cursor < bStart)
                result.Add((cursor, bStart));
            if (bEnd > cursor) cursor = bEnd;
        }
        if (cursor < windowEnd)
            result.Add((cursor, windowEnd));

        return result;
    }

    // Returns sub-intervals split at 10pm and 6am Manila time boundaries
    private static List<(DateTimeOffset Start, DateTimeOffset End, bool IsNightDiff)> SplitNightDiff(
        DateTimeOffset start, DateTimeOffset end)
    {
        var result = new List<(DateTimeOffset, DateTimeOffset, bool)>();
        var cursor = start;

        while (cursor < end)
        {
            // Determine next boundary point in Manila time after cursor
            var cursorManila = ToManila(cursor);
            var timeOfDay = TimeOnly.FromTimeSpan(cursorManila.TimeOfDay);
            bool currentlyNightDiff = IsNightDiffTime(timeOfDay);

            // Find next crossing of a night-diff boundary (10pm or 6am Manila)
            DateTimeOffset? boundary = null;

            // Try 10pm boundary today and yesterday/tomorrow Manila
            foreach (var boundaryTime in new[] { NightDiffStart, NightDiffEnd })
            {
                var candidateManila = new DateTimeOffset(
                    cursorManila.Year, cursorManila.Month, cursorManila.Day,
                    boundaryTime.Hour, boundaryTime.Minute, 0, ManilaOffset);

                // Also check the next day for this boundary
                for (int dayOffset = 0; dayOffset <= 1; dayOffset++)
                {
                    var b = candidateManila.AddDays(dayOffset);
                    if (b > cursor && b < end)
                    {
                        if (boundary is null || b < boundary.Value)
                            boundary = b;
                    }
                }
            }

            var segEnd = boundary ?? end;
            result.Add((cursor, segEnd, currentlyNightDiff));
            cursor = segEnd;
        }

        return result;
    }

    private static bool IsNightDiffTime(TimeOnly t)
    {
        // Night diff: 10pm (22:00) to 6am (06:00) — spans midnight
        return t >= NightDiffStart || t < NightDiffEnd;
    }

    private static TimeSegmentClassification? ClassifySegment(
        SegmentKind kind,
        bool nightDiff,
        HolidayType holiday,
        bool restDay,
        bool holidayPayEnabled)
    {
        // Special Working Day → treated as ordinary working day (FR41)
        if (holiday == HolidayType.SPECIAL_WORKING)
            holiday = HolidayType.NONE;

        return (kind, nightDiff, holiday, restDay) switch
        {
            // ── Regular Day ────────────────────────────────────────────────
            (SegmentKind.PaidHours, false, HolidayType.NONE, false) => TSC.NORMAL_PAID_HOURS,
            (SegmentKind.PaidHours, true,  HolidayType.NONE, false) => TSC.NIGHT_DIFF_PAID_HOURS,
            (SegmentKind.EarlyOt,  false, HolidayType.NONE, false) => TSC.EARLY_OT,
            (SegmentKind.EarlyOt,  true,  HolidayType.NONE, false) => TSC.NIGHT_DIFF_EARLY_OT,
            (SegmentKind.NormalOt, false, HolidayType.NONE, false) => TSC.NORMAL_OT,
            (SegmentKind.NormalOt, true,  HolidayType.NONE, false) => TSC.NIGHT_DIFF_OT,

            // ── Regular Holiday (not rest day) ─────────────────────────────
            (SegmentKind.PaidHours, false, HolidayType.REGULAR, false)
                => holidayPayEnabled ? TSC.REGULAR_HOLIDAY_PAID_HOURS : null,
            (SegmentKind.PaidHours, true,  HolidayType.REGULAR, false)
                => holidayPayEnabled ? TSC.NIGHT_DIFF_REGULAR_HOLIDAY_PAID_HOURS : null,
            (SegmentKind.EarlyOt,  false, HolidayType.REGULAR, false) => TSC.REGULAR_HOLIDAY_EARLY_OT,
            (SegmentKind.EarlyOt,  true,  HolidayType.REGULAR, false) => TSC.NIGHT_DIFF_REGULAR_HOLIDAY_EARLY_OT,
            (SegmentKind.NormalOt, false, HolidayType.REGULAR, false) => TSC.REGULAR_HOLIDAY_OT,
            (SegmentKind.NormalOt, true,  HolidayType.REGULAR, false) => TSC.NIGHT_DIFF_REGULAR_HOLIDAY_OT,

            // ── Special Non-Working Holiday (not rest day) ─────────────────
            (SegmentKind.PaidHours, false, HolidayType.SPECIAL_NON_WORKING, false)
                => holidayPayEnabled ? TSC.SPECIAL_HOLIDAY_PAID_HOURS : null,
            (SegmentKind.PaidHours, true,  HolidayType.SPECIAL_NON_WORKING, false)
                => holidayPayEnabled ? TSC.NIGHT_DIFF_SPECIAL_HOLIDAY_PAID_HOURS : null,
            (SegmentKind.EarlyOt,  false, HolidayType.SPECIAL_NON_WORKING, false) => TSC.SPECIAL_HOLIDAY_EARLY_OT,
            (SegmentKind.EarlyOt,  true,  HolidayType.SPECIAL_NON_WORKING, false) => TSC.NIGHT_DIFF_SPECIAL_HOLIDAY_EARLY_OT,
            (SegmentKind.NormalOt, false, HolidayType.SPECIAL_NON_WORKING, false) => TSC.SPECIAL_HOLIDAY_OT,
            (SegmentKind.NormalOt, true,  HolidayType.SPECIAL_NON_WORKING, false) => TSC.NIGHT_DIFF_SPECIAL_HOLIDAY_OT,

            // ── Rest Day (no holiday) ──────────────────────────────────────
            (SegmentKind.PaidHours, false, HolidayType.NONE, true) => TSC.REST_DAY_PAID_HOURS,
            (SegmentKind.PaidHours, true,  HolidayType.NONE, true) => TSC.NIGHT_DIFF_REST_DAY_PAID_HOURS,
            (SegmentKind.EarlyOt,  false, HolidayType.NONE, true) => TSC.REST_DAY_EARLY_OT,
            (SegmentKind.EarlyOt,  true,  HolidayType.NONE, true) => TSC.NIGHT_DIFF_REST_DAY_EARLY_OT,
            (SegmentKind.NormalOt, false, HolidayType.NONE, true) => TSC.REST_DAY_OT,
            (SegmentKind.NormalOt, true,  HolidayType.NONE, true) => TSC.NIGHT_DIFF_REST_DAY_OT,

            // ── Rest Day + Regular Holiday ─────────────────────────────────
            (SegmentKind.PaidHours, false, HolidayType.REGULAR, true) => TSC.REST_DAY_REGULAR_HOLIDAY_PAID_PREMIUM,
            (SegmentKind.PaidHours, true,  HolidayType.REGULAR, true) => TSC.NIGHT_DIFF_REST_DAY_REGULAR_HOLIDAY_PAID_PREMIUM,
            (SegmentKind.EarlyOt,  false, HolidayType.REGULAR, true) => TSC.REST_DAY_REGULAR_HOLIDAY_EARLY_OT,
            (SegmentKind.EarlyOt,  true,  HolidayType.REGULAR, true) => TSC.NIGHT_DIFF_REST_DAY_REGULAR_HOLIDAY_EARLY_OT,
            (SegmentKind.NormalOt, false, HolidayType.REGULAR, true) => TSC.REST_DAY_REGULAR_HOLIDAY_OT,
            (SegmentKind.NormalOt, true,  HolidayType.REGULAR, true) => TSC.NIGHT_DIFF_REST_DAY_REGULAR_HOLIDAY_OT,

            // ── Rest Day + Special Non-Working Holiday ─────────────────────
            (SegmentKind.PaidHours, false, HolidayType.SPECIAL_NON_WORKING, true) => TSC.REST_DAY_SPECIAL_HOLIDAY_PAID_PREMIUM,
            (SegmentKind.PaidHours, true,  HolidayType.SPECIAL_NON_WORKING, true) => TSC.NIGHT_DIFF_REST_DAY_SPECIAL_HOLIDAY_PAID_PREMIUM,
            (SegmentKind.EarlyOt,  false, HolidayType.SPECIAL_NON_WORKING, true) => TSC.REST_DAY_SPECIAL_HOLIDAY_EARLY_OT,
            (SegmentKind.EarlyOt,  true,  HolidayType.SPECIAL_NON_WORKING, true) => TSC.NIGHT_DIFF_REST_DAY_SPECIAL_HOLIDAY_EARLY_OT,
            (SegmentKind.NormalOt, false, HolidayType.SPECIAL_NON_WORKING, true) => TSC.REST_DAY_SPECIAL_HOLIDAY_OT,
            (SegmentKind.NormalOt, true,  HolidayType.SPECIAL_NON_WORKING, true) => TSC.NIGHT_DIFF_REST_DAY_SPECIAL_HOLIDAY_OT,

            _ => TSC.NORMAL_PAID_HOURS
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsRestDay(WorkSchedulePattern pattern, DateOnly date)
    {
        // Find the active pattern effective on the given date
        if (pattern.ExpiryDate.HasValue && date > pattern.ExpiryDate.Value) return false;
        if (date < pattern.EffectiveDate) return false;
        var dayOfWeek = date.DayOfWeek;
        return pattern.RestDays.Contains(dayOfWeek);
    }

    private static ApprovalStatus DetermineApprovalStatus(
        HolidayType holiday, bool restDay,
        bool holidayApproved, bool restDayApproved)
    {
        if (holiday != HolidayType.NONE && restDay)
            return (holidayApproved && restDayApproved) ? ApprovalStatus.APPROVED : ApprovalStatus.PENDING;
        if (holiday != HolidayType.NONE)
            return holidayApproved ? ApprovalStatus.APPROVED : ApprovalStatus.PENDING;
        if (restDay)
            return restDayApproved ? ApprovalStatus.APPROVED : ApprovalStatus.PENDING;
        return ApprovalStatus.APPROVED;
    }

    private static int ScheduleMinutes(ShiftSchedule s)
        => (int)(s.ScheduleEnd - s.ScheduleStart).TotalMinutes;

    private static DateTimeOffset ToManila(DateTimeOffset utc) => utc.ToOffset(ManilaOffset);

    private enum SegmentKind { PaidHours, EarlyOt, NormalOt }

    // Alias for brevity
    private static class TSC
    {
        public static readonly TimeSegmentClassification NORMAL_PAID_HOURS = TimeSegmentClassification.NORMAL_PAID_HOURS;
        public static readonly TimeSegmentClassification NIGHT_DIFF_PAID_HOURS = TimeSegmentClassification.NIGHT_DIFF_PAID_HOURS;
        public static readonly TimeSegmentClassification EARLY_OT = TimeSegmentClassification.EARLY_OT;
        public static readonly TimeSegmentClassification NIGHT_DIFF_EARLY_OT = TimeSegmentClassification.NIGHT_DIFF_EARLY_OT;
        public static readonly TimeSegmentClassification NORMAL_OT = TimeSegmentClassification.NORMAL_OT;
        public static readonly TimeSegmentClassification NIGHT_DIFF_OT = TimeSegmentClassification.NIGHT_DIFF_OT;
        public static readonly TimeSegmentClassification REGULAR_HOLIDAY_PAID_HOURS = TimeSegmentClassification.REGULAR_HOLIDAY_PAID_HOURS;
        public static readonly TimeSegmentClassification NIGHT_DIFF_REGULAR_HOLIDAY_PAID_HOURS = TimeSegmentClassification.NIGHT_DIFF_REGULAR_HOLIDAY_PAID_HOURS;
        public static readonly TimeSegmentClassification REGULAR_HOLIDAY_EARLY_OT = TimeSegmentClassification.REGULAR_HOLIDAY_EARLY_OT;
        public static readonly TimeSegmentClassification NIGHT_DIFF_REGULAR_HOLIDAY_EARLY_OT = TimeSegmentClassification.NIGHT_DIFF_REGULAR_HOLIDAY_EARLY_OT;
        public static readonly TimeSegmentClassification REGULAR_HOLIDAY_OT = TimeSegmentClassification.REGULAR_HOLIDAY_OT;
        public static readonly TimeSegmentClassification NIGHT_DIFF_REGULAR_HOLIDAY_OT = TimeSegmentClassification.NIGHT_DIFF_REGULAR_HOLIDAY_OT;
        public static readonly TimeSegmentClassification SPECIAL_HOLIDAY_PAID_HOURS = TimeSegmentClassification.SPECIAL_HOLIDAY_PAID_HOURS;
        public static readonly TimeSegmentClassification NIGHT_DIFF_SPECIAL_HOLIDAY_PAID_HOURS = TimeSegmentClassification.NIGHT_DIFF_SPECIAL_HOLIDAY_PAID_HOURS;
        public static readonly TimeSegmentClassification SPECIAL_HOLIDAY_EARLY_OT = TimeSegmentClassification.SPECIAL_HOLIDAY_EARLY_OT;
        public static readonly TimeSegmentClassification NIGHT_DIFF_SPECIAL_HOLIDAY_EARLY_OT = TimeSegmentClassification.NIGHT_DIFF_SPECIAL_HOLIDAY_EARLY_OT;
        public static readonly TimeSegmentClassification SPECIAL_HOLIDAY_OT = TimeSegmentClassification.SPECIAL_HOLIDAY_OT;
        public static readonly TimeSegmentClassification NIGHT_DIFF_SPECIAL_HOLIDAY_OT = TimeSegmentClassification.NIGHT_DIFF_SPECIAL_HOLIDAY_OT;
        public static readonly TimeSegmentClassification REST_DAY_PAID_HOURS = TimeSegmentClassification.REST_DAY_PAID_HOURS;
        public static readonly TimeSegmentClassification NIGHT_DIFF_REST_DAY_PAID_HOURS = TimeSegmentClassification.NIGHT_DIFF_REST_DAY_PAID_HOURS;
        public static readonly TimeSegmentClassification REST_DAY_EARLY_OT = TimeSegmentClassification.REST_DAY_EARLY_OT;
        public static readonly TimeSegmentClassification NIGHT_DIFF_REST_DAY_EARLY_OT = TimeSegmentClassification.NIGHT_DIFF_REST_DAY_EARLY_OT;
        public static readonly TimeSegmentClassification REST_DAY_OT = TimeSegmentClassification.REST_DAY_OT;
        public static readonly TimeSegmentClassification NIGHT_DIFF_REST_DAY_OT = TimeSegmentClassification.NIGHT_DIFF_REST_DAY_OT;
        public static readonly TimeSegmentClassification REST_DAY_REGULAR_HOLIDAY_PAID_PREMIUM = TimeSegmentClassification.REST_DAY_REGULAR_HOLIDAY_PAID_PREMIUM;
        public static readonly TimeSegmentClassification NIGHT_DIFF_REST_DAY_REGULAR_HOLIDAY_PAID_PREMIUM = TimeSegmentClassification.NIGHT_DIFF_REST_DAY_REGULAR_HOLIDAY_PAID_PREMIUM;
        public static readonly TimeSegmentClassification REST_DAY_REGULAR_HOLIDAY_EARLY_OT = TimeSegmentClassification.REST_DAY_REGULAR_HOLIDAY_EARLY_OT;
        public static readonly TimeSegmentClassification NIGHT_DIFF_REST_DAY_REGULAR_HOLIDAY_EARLY_OT = TimeSegmentClassification.NIGHT_DIFF_REST_DAY_REGULAR_HOLIDAY_EARLY_OT;
        public static readonly TimeSegmentClassification REST_DAY_REGULAR_HOLIDAY_OT = TimeSegmentClassification.REST_DAY_REGULAR_HOLIDAY_OT;
        public static readonly TimeSegmentClassification NIGHT_DIFF_REST_DAY_REGULAR_HOLIDAY_OT = TimeSegmentClassification.NIGHT_DIFF_REST_DAY_REGULAR_HOLIDAY_OT;
        public static readonly TimeSegmentClassification REST_DAY_SPECIAL_HOLIDAY_PAID_PREMIUM = TimeSegmentClassification.REST_DAY_SPECIAL_HOLIDAY_PAID_PREMIUM;
        public static readonly TimeSegmentClassification NIGHT_DIFF_REST_DAY_SPECIAL_HOLIDAY_PAID_PREMIUM = TimeSegmentClassification.NIGHT_DIFF_REST_DAY_SPECIAL_HOLIDAY_PAID_PREMIUM;
        public static readonly TimeSegmentClassification REST_DAY_SPECIAL_HOLIDAY_EARLY_OT = TimeSegmentClassification.REST_DAY_SPECIAL_HOLIDAY_EARLY_OT;
        public static readonly TimeSegmentClassification NIGHT_DIFF_REST_DAY_SPECIAL_HOLIDAY_EARLY_OT = TimeSegmentClassification.NIGHT_DIFF_REST_DAY_SPECIAL_HOLIDAY_EARLY_OT;
        public static readonly TimeSegmentClassification REST_DAY_SPECIAL_HOLIDAY_OT = TimeSegmentClassification.REST_DAY_SPECIAL_HOLIDAY_OT;
        public static readonly TimeSegmentClassification NIGHT_DIFF_REST_DAY_SPECIAL_HOLIDAY_OT = TimeSegmentClassification.NIGHT_DIFF_REST_DAY_SPECIAL_HOLIDAY_OT;
    }
}
