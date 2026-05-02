namespace PhPayrollTimeApi.Domain.Enums;

public enum TimeSegmentClassification
{
    // ── Regular Day ──────────────────────────────────────────────────────────
    NORMAL_PAID_HOURS,
    NIGHT_DIFF_PAID_HOURS,
    EARLY_OT,
    NIGHT_DIFF_EARLY_OT,
    NORMAL_OT,
    NIGHT_DIFF_OT,

    // ── Regular Holiday (Art. 94 — 200% if worked) ───────────────────────────
    REGULAR_HOLIDAY_PAID_HOURS,               // feature-flagged
    NIGHT_DIFF_REGULAR_HOLIDAY_PAID_HOURS,    // feature-flagged
    REGULAR_HOLIDAY_EARLY_OT,
    NIGHT_DIFF_REGULAR_HOLIDAY_EARLY_OT,
    REGULAR_HOLIDAY_OT,
    NIGHT_DIFF_REGULAR_HOLIDAY_OT,

    // ── Special Non-Working Holiday (RA 9492 — 130% if worked) ───────────────
    SPECIAL_HOLIDAY_PAID_HOURS,               // feature-flagged
    NIGHT_DIFF_SPECIAL_HOLIDAY_PAID_HOURS,    // feature-flagged
    SPECIAL_HOLIDAY_EARLY_OT,
    NIGHT_DIFF_SPECIAL_HOLIDAY_EARLY_OT,
    SPECIAL_HOLIDAY_OT,
    NIGHT_DIFF_SPECIAL_HOLIDAY_OT,

    // ── Rest Day (Art. 91-93 — 130% if worked) ───────────────────────────────
    REST_DAY_PAID_HOURS,
    NIGHT_DIFF_REST_DAY_PAID_HOURS,
    REST_DAY_EARLY_OT,
    NIGHT_DIFF_REST_DAY_EARLY_OT,
    REST_DAY_OT,
    NIGHT_DIFF_REST_DAY_OT,

    // ── Rest Day + Regular Holiday (260% if worked) ───────────────────────────
    REST_DAY_REGULAR_HOLIDAY_PAID_PREMIUM,
    NIGHT_DIFF_REST_DAY_REGULAR_HOLIDAY_PAID_PREMIUM,
    REST_DAY_REGULAR_HOLIDAY_EARLY_OT,
    NIGHT_DIFF_REST_DAY_REGULAR_HOLIDAY_EARLY_OT,
    REST_DAY_REGULAR_HOLIDAY_OT,
    NIGHT_DIFF_REST_DAY_REGULAR_HOLIDAY_OT,

    // ── Rest Day + Special Non-Working Holiday (150% if worked) ──────────────
    REST_DAY_SPECIAL_HOLIDAY_PAID_PREMIUM,
    NIGHT_DIFF_REST_DAY_SPECIAL_HOLIDAY_PAID_PREMIUM,
    REST_DAY_SPECIAL_HOLIDAY_EARLY_OT,
    NIGHT_DIFF_REST_DAY_SPECIAL_HOLIDAY_EARLY_OT,
    REST_DAY_SPECIAL_HOLIDAY_OT,
    NIGHT_DIFF_REST_DAY_SPECIAL_HOLIDAY_OT,

    // ── Tagged / Terminal States ──────────────────────────────────────────────
    // Regular Holiday, no approved schedule AND no logs — 100% paid Art. 94
    REGULAR_HOLIDAY_REST_PAID,
    // Rest Day + Special Holiday, no combined approval AND no logs — no pay, no deduction
    REST_DAY_SPECIAL_HOLIDAY_UNPAID,
    ABSENT,
    // Exclusive: emitted when valid IN found but no OUT yet and schedule still ongoing
    IS_IN_CURRENT_SCHEDULE,
}
