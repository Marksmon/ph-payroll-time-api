---
stepsCompleted: [step-01-init, step-02-discovery, step-02b-vision, step-02c-executive-summary, step-03-success, step-04-journeys, step-05-domain, step-06-innovation, step-07-project-type, step-08-scoping, step-09-functional, step-10-nonfunctional, step-11-polish]
releaseMode: phased
inputDocuments: []
workflowType: 'prd'
technicalGuidelines:
  language: 'C# / .NET 8'
  architecture: 'N-Tier Pattern'
  codeQuality: 'Clean Code'
  pattern: 'CQRS'
  apiDesign: 'Small-Controller'
  apiDocs: 'Swagger / OpenAPI'
classification:
  projectType: 'api_backend'
  domain: 'HR / Time & Attendance'
  complexity: 'medium'
  projectContext: 'greenfield'
---

# Product Requirements Document - ph-payroll-time-api

**Author:** Mark
**Date:** 2026-04-30

## Executive Summary

`ph-payroll-time-api` is an internal REST API that computes compensable work time under Philippine DOLE RA 6727 overtime rules. It targets backend systems requiring accurate, rule-governed classification of work hours — regular time, overtime, night differential, rest day, and holiday categories — expressed as **time quantities**, not monetary amounts.

The core problem it solves is the inherent complexity of shift-based overtime: naive implementations break when shifts span date boundaries, particularly when a crossing moves from a regular workday into a rest day or public holiday mid-shift. This API resolves that ambiguity deterministically by modeling time intervals against the applicable rule schedule at each boundary.

Built as a portfolio project, it demonstrates production-grade .NET 8 backend engineering through Clean Architecture layering, CQRS command/query separation, thin controllers with rich domain handlers, and full Swagger/OpenAPI documentation.

### What Makes This Special

Most overtime engines treat a workday as an atomic unit. `ph-payroll-time-api` treats it as a **rule-bounded interval**. When a shift crosses midnight into a different schedule category (e.g., regular day → public holiday), the API segments the shift at the boundary and applies the correct RA 6727 rule to each segment independently — producing legally accurate compensable-time breakdowns for edge cases most implementations silently get wrong.

The technical differentiator is the strict separation of concerns enforced by CQRS and Small-Controller: overtime classification logic lives entirely in domain handlers and value objects, never in controllers or data mappers. The architecture makes business rules explicit, testable, and auditable.

## Project Classification

| Attribute | Value |
|---|---|
| **Project Type** | API Backend (Internal REST API) |
| **Domain** | HR / Time & Attendance |
| **Complexity** | Medium |
| **Project Context** | Greenfield |
| **Tech Stack** | C# / .NET 8, N-Tier Architecture, CQRS, Clean Code, Small-Controller, Swagger/OpenAPI |

## Success Criteria

### Time Segment Classifications

Philippine labor law defines three tiers of special days — **Regular Holidays** (Art. 94, Labor Code), **Special Non-Working Holidays** (RA 9492), and **Special Working Days** — each carrying distinct pay obligations. Rest day is an independent dimension derived from the employee's work schedule pattern. These two dimensions combine to produce the full classification set.

Night differential (ND) applies to any segment whose interval falls within the **10pm–6am** window, creating additional boundaries at 10pm and 6am.

**Special Working Days** (government-declared ordinary working days despite proximity to a holiday) carry no pay premium — segments on these dates use Regular Day classifications.

#### Regular Day

| Classification | Description |
|---|---|
| **Normal Paid Hours** | Within scheduled hours, regular day, outside 10pm–6am |
| **Night Diff Paid Hours** | Within scheduled hours, regular day, 10pm–6am |
| **Early OT** | Before schedule start, regular day, outside 10pm–6am; pending approval |
| **Night Diff Early OT** | Before schedule start, regular day, 10pm–6am; pending approval |
| **Normal OT** | After schedule end, regular day, outside 10pm–6am; pending approval |
| **Night Diff OT** | After schedule end, regular day, 10pm–6am; pending approval |

#### Regular Holiday *(Art. 94 — 100% paid even if absent; 200% if worked)*

| Classification | Description |
|---|---|
| **Regular Holiday Paid Hours** | Within scheduled hours, approved regular holiday shift, outside 10pm–6am *(feature-flagged)* |
| **Night Diff Regular Holiday Paid Hours** | Within scheduled hours, approved regular holiday shift, 10pm–6am *(feature-flagged)* |
| **Regular Holiday Early OT** | Before schedule start, approved regular holiday, outside 10pm–6am; pending approval |
| **Night Diff Regular Holiday Early OT** | Before schedule start, approved regular holiday, 10pm–6am; pending approval |
| **Regular Holiday OT** | After schedule end, approved regular holiday, outside 10pm–6am; pending approval |
| **Night Diff Regular Holiday OT** | After schedule end, approved regular holiday, 10pm–6am; pending approval |

#### Special Non-Working Holiday *(RA 9492 — "no work, no pay"; 130% if worked)*

| Classification | Description |
|---|---|
| **Special Holiday Paid Hours** | Within scheduled hours, approved special non-working holiday shift, outside 10pm–6am *(feature-flagged)* |
| **Night Diff Special Holiday Paid Hours** | Within scheduled hours, approved special non-working holiday shift, 10pm–6am *(feature-flagged)* |
| **Special Holiday Early OT** | Before schedule start, approved special holiday, outside 10pm–6am; pending approval |
| **Night Diff Special Holiday Early OT** | Before schedule start, approved special holiday, 10pm–6am; pending approval |
| **Special Holiday OT** | After schedule end, approved special holiday, outside 10pm–6am; pending approval |
| **Night Diff Special Holiday OT** | After schedule end, approved special holiday, 10pm–6am; pending approval |

#### Rest Day *(Art. 91–93 — 130% if worked)*

| Classification | Description |
|---|---|
| **Rest Day Paid Hours** | Within scheduled hours, approved rest day schedule, outside 10pm–6am |
| **Night Diff Rest Day Paid Hours** | Within scheduled hours, approved rest day schedule, 10pm–6am |
| **Rest Day Early OT** | Before schedule start, approved rest day schedule, outside 10pm–6am; pending OT approval |
| **Night Diff Rest Day Early OT** | Before schedule start, approved rest day schedule, 10pm–6am; pending OT approval |
| **Rest Day OT** | After schedule end, approved rest day schedule, outside 10pm–6am; pending OT approval |
| **Night Diff Rest Day OT** | After schedule end, approved rest day schedule, 10pm–6am; pending OT approval |

#### Rest Day + Regular Holiday *(260% if worked)*

| Classification | Description |
|---|---|
| **Rest Day Regular Holiday Paid Premium** | Within scheduled hours, combined approval, outside 10pm–6am |
| **Night Diff Rest Day Regular Holiday Paid Premium** | Within scheduled hours, combined approval, 10pm–6am |
| **Rest Day Regular Holiday Early OT** | Before schedule start, combined approval, outside 10pm–6am; pending OT approval |
| **Night Diff Rest Day Regular Holiday Early OT** | Before schedule start, combined approval, 10pm–6am; pending OT approval |
| **Rest Day Regular Holiday OT** | After schedule end, combined approval, outside 10pm–6am; pending OT approval |
| **Night Diff Rest Day Regular Holiday OT** | After schedule end, combined approval, 10pm–6am; pending OT approval |

#### Rest Day + Special Non-Working Holiday *(150% if worked)*

| Classification | Description |
|---|---|
| **Rest Day Special Holiday Paid Premium** | Within scheduled hours, combined approval, outside 10pm–6am |
| **Night Diff Rest Day Special Holiday Paid Premium** | Within scheduled hours, combined approval, 10pm–6am |
| **Rest Day Special Holiday Early OT** | Before schedule start, combined approval, outside 10pm–6am; pending OT approval |
| **Night Diff Rest Day Special Holiday Early OT** | Before schedule start, combined approval, 10pm–6am; pending OT approval |
| **Rest Day Special Holiday OT** | After schedule end, combined approval, outside 10pm–6am; pending OT approval |
| **Night Diff Rest Day Special Holiday OT** | After schedule end, combined approval, 10pm–6am; pending OT approval |

#### Tagged / Terminal States

| Classification | Description |
|---|---|
| **Normal Paid Hours** *(Regular Holiday Rest)* | Regular Holiday, no approved schedule AND no logs — 100% paid by Art. 94 entitlement. If logs are present without approval, full Regular Holiday classification applies instead. |
| **Rest Day Special Holiday Unpaid** *(tag)* | Rest day + Special Non-Working Holiday, no combined approval AND no logs — no pay, no deduction. If logs are present without approval, Rest Day + Special Holiday classification applies instead. |
| **Absent** | No valid IN/OUT pair, schedule end has passed |
| **Is in Current Schedule** | Valid IN found, no OUT yet, server time before schedule end. Exclusive state — when emitted, no other classification is produced for that schedule entry. |

### Log Pairing Algorithm

**Finding the IN:**
1. Look for an unclaimed IN on the schedule start date — skip if it falls **within or before** another schedule's time range
2. If none, check the immediately preceding calendar day only — take the **earliest** valid unclaimed IN (e.g., 6pm beats 11pm)
3. Skip any IN that falls **within or before** another schedule's time range
4. Unclaimed IN from 2+ days prior → not included
5. No valid IN → **Absent** or **Is in Current Schedule**

**Finding the OUT:**
1. Look for an unclaimed OUT after the schedule start
2. Skip if it falls **within or after** the next schedule's time range
3. No valid OUT → **Absent** or **Is in Current Schedule**

**Log consumption** is tracked in-memory during computation only — not persisted to the database.

### Regular Holiday Schedule Rules *(Art. 94 — 100% paid regardless of work)*

> **Cross-cutting rule:** Where time logs exist indicating actual work was performed, statutory compensation rates apply regardless of schedule approval status. Schedule Approval governs the authorization and HR/disciplinary workflow — it does not override statutory premium pay entitlements under Art. 94 (Labor Code) and RA 9492.

| Holiday Schedule Approval | Logs Present | Result |
|---|---|---|
| Not approved | No logs | Normal Paid Hours *(tagged: Regular Holiday Rest — 100% paid by Art. 94 entitlement)* |
| Not approved | Logs present | Regular holiday classification applies — Art. 94 premium triggered by actual work; shift flagged for HR review |
| Approved | No logs | Regular Holiday Paid Hours *(100% paid by Art. 94 regardless of absence)* |
| Approved | Logs present | Full regular holiday classification applies |

### Special Non-Working Holiday Schedule Rules *(RA 9492 — "no work, no pay"; 130% if worked)*

| Holiday Schedule Approval | Logs Present | Result |
|---|---|---|
| Not approved | No logs | No result — no pay, no deduction *(RA 9492 "no work, no pay")* |
| Not approved | Logs present | Special holiday classification applies — RA 9492 130% rate triggered by actual work; shift flagged for HR review |
| Approved | No logs | Special Holiday Paid Hours *(paid by entitlement regardless of absence)* |
| Approved | Logs present | Full special holiday classification applies |

- Approval is triggered only when **shift start date** falls on a PH holiday (Regular or Special Non-Working)
- Cross-date shifts where only the end date is a holiday do not require schedule approval
- Holiday Schedule Approval is a prerequisite for OT Approval on that day

### Rest Day Schedule Rules

A calendar date is a **rest day** for an employee when the employee's active `WorkSchedulePattern` designates that day of the week as a rest day and the pattern is in effect for that calendar date. Rest day status is derived from the **employer-designated `WorkSchedulePattern`** — not inferred from the absence of a scheduled shift. This satisfies the Art. 91 Labor Code requirement that rest days be employer-designated.

The `WorkSchedulePattern` is the source of truth: for a given employee and Manila local date, find the record where `EffectiveDate <= date < ExpiryDate` (or `ExpiryDate IS NULL`) and `IsActive = true`. If the date's `DayOfWeek` is in `RestDays`, it is a rest day. If no active pattern exists for the employee, rest day determination raises a validation error — it does not silently default to non-rest-day.

**Rest Day + Schedule (employee called in on rest day):**

- Employee or manager applies for a **Rest Day Schedule** on the rest day date
- Manager approves the Rest Day Schedule Approval (prerequisite gate — same tier as Holiday Schedule Approval)
- Once approved, rest day classifications apply to that schedule

| Rest Day Schedule Approval | Logs Present | Result |
|---|---|---|
| Not approved | Any | Absent *(no pay — rest day with no approved schedule is not a paid entitlement)* |
| Approved | No logs | Rest Day Paid Hours *(paid by entitlement regardless of absence)* |
| Approved | Logs present | Full rest day classification applies |

- Rest Day Schedule Approval is a prerequisite for OT Approval on that rest day
- Cross-date shifts: if a shift **starts on a regular workday** and crosses midnight into a rest day, the entire shift is computed as a regular workday — rest day classifications do not apply

### Rest Day + Regular Holiday Rules *(260% if worked)*

When a rest day coincides with a **Regular Holiday**, a **single combined approval request** covers both the rest day schedule and the holiday entitlement — one manager action for both.

| Combined Approval | Logs Present | Result |
|---|---|---|
| Not approved | No logs | Normal Paid Hours *(tagged: Regular Holiday Rest — 100% paid by Art. 94 entitlement)* |
| Not approved | Logs present | Rest Day + Regular Holiday classification applies — 260% rate; actual work triggers statutory premium; shift flagged for HR review |
| Approved | No logs | Rest Day Regular Holiday Paid Premium *(paid by entitlement regardless of absence)* |
| Approved | Logs present | Full rest day + regular holiday classification applies |

### Rest Day + Special Non-Working Holiday Rules *(150% if worked)*

When a rest day coincides with a **Special Non-Working Holiday**, a **single combined approval request** covers both.

| Combined Approval | Logs Present | Result |
|---|---|---|
| Not approved | No logs | Rest Day Special Holiday Unpaid *(tagged — no pay, no deduction; RA 9492 "no work, no pay")* |
| Not approved | Logs present | Rest Day + Special Holiday classification applies — 150% rate; actual work triggers statutory premium; shift flagged for HR review |
| Approved | No logs | Rest Day Special Holiday Paid Premium *(paid by entitlement regardless of absence)* |
| Approved | Logs present | Full rest day + special holiday classification applies |

- "No pay, no deduction" for Rest Day + Special Non-Working means the employee is not marked Absent and no salary deduction is applied
- Combined approval is a single DB record covering both rest day schedule and holiday entitlement; they cannot be independently approved or rejected
- Combined approval is a prerequisite for OT Approval on that day

### Manager Approval Workflow

**Holiday Schedule Approval (prerequisite):**
- Binary approve/reject per employee per day
- Supports bulk multi-employee selection
- Must be approved before OT Approval is available for that day

**Rest Day Schedule Approval (prerequisite):**
- Binary approve/reject per employee per day
- Supports bulk multi-employee selection
- Must be approved before OT Approval is available for that rest day
- When rest day coincides with a non-working holiday: **combined into a single approval request** — one action approves both rest day schedule and holiday entitlement

**OT Approval (transactional, per employee per day):**

| Action | Cascade Effect |
|---|---|
| Reduce Early OT end time past night diff boundary (10pm) | Auto-removes Night Diff Early OT; reinstates if extended back |
| Reduce Early OT duration past midnight | Auto-removes Regular Holiday Early OT or Special Holiday Early OT (whichever applies) and their night diff variants; reinstates if extended back with logs |
| Reject Early OT | Removes Early OT, Night Diff Early OT, Regular/Special Holiday Early OT, Night Diff Regular/Special Holiday Early OT |
| Override Regular Holiday Early OT → Early OT | Reclassifies without changing duration; Night Diff variants follow same override |
| Override Special Holiday Early OT → Early OT | Reclassifies without changing duration; Night Diff variants follow same override |
| Reduce Normal OT end time past night diff boundary (10pm) | Auto-removes Night Diff OT; reinstates if extended back |
| Reduce Normal OT duration past midnight | Auto-removes Regular Holiday OT or Special Holiday OT (whichever applies) and their night diff variants; reinstates if extended back with logs |
| Override Regular Holiday OT → Normal OT | Reclassifies without changing duration; Night Diff variants follow same override |
| Override Special Holiday OT → Normal OT | Reclassifies without changing duration; Night Diff variants follow same override |
| Override Rest Day Regular Holiday OT → Rest Day OT | Reclassifies without changing duration; Night Diff variants follow same override |
| Override Rest Day Special Holiday OT → Rest Day OT | Reclassifies without changing duration; Night Diff variants follow same override |
| Override Rest Day Regular Holiday Paid Premium → Rest Day Paid Hours | Reclassifies scheduled-hours portion; does not affect OT segments |
| Override Rest Day Special Holiday Paid Premium → Rest Day Paid Hours | Reclassifies scheduled-hours portion; does not affect OT segments |
| Reduce Rest Day OT end time past night diff boundary (10pm) | Auto-removes Night Diff Rest Day OT; reinstates if extended back |
| Reduce Rest Day OT duration past midnight | Auto-removes Rest Day Regular/Special Holiday OT and their night diff variants; reinstates if extended back with logs |
| Reject Rest Day Early OT | Removes Rest Day Early OT and Night Diff Rest Day Early OT |

- All staged actions commit **atomically** on final approval
- Supports bulk multi-employee selection for approval commit
- Night Diff segment variants always follow the same override/cascade as their non-night-diff counterpart

### User Success

- Any time entry is correctly segmented into the correct classification based on schedule, logs, holiday calendar, and approval state
- Break time automatically deducted based on the break window defined in the shift schedule
- Cross-date schedules fully supported; holiday classification applies to the correct date segment
- API response includes full segment-level breakdown: start, end, duration, classification, approval status
- No OT segment is payment-eligible until manager-approved

### Business Success

- Demonstrates correct domain modeling of a complex, rule-governed computation engine
- CQRS/Small-Controller discipline — overtime logic lives in handlers and value objects, never in controllers
- Full Swagger/OpenAPI documentation — every endpoint, request/response model, error code, and feature flag documented
- Unit and integration tests covering all named edge cases

### Technical Success

- Computation engine correctly classifies all 40 segment types across date, night differential, rest day, Regular Holiday, and Special Non-Working Holiday boundaries
- Feature flag: `HolidayPayWithinScheduledHours` — suppresses `REGULAR_HOLIDAY_PAID_HOURS`, `NIGHT_DIFF_REGULAR_HOLIDAY_PAID_HOURS`, `SPECIAL_HOLIDAY_PAID_HOURS`, and `NIGHT_DIFF_SPECIAL_HOLIDAY_PAID_HOURS`; OT types in all holiday groups are unaffected; default enabled
- PH national holiday calendar (Regular + Special Non-Working + Special Working) integrated as a queryable data source
- Night differential boundaries (10pm, 6am) applied as additional segment split points alongside schedule and date boundaries
- Rest day determined from `WorkSchedulePattern` (employer-designated per Art. 91); no schedule-absence inference
- Combined Rest Day + Holiday Schedule Approval: single request covers both approvals when rest day coincides with a non-working holiday
- Approval state machine is transactional — staged actions committed atomically
- Log pairing is stateless at the DB level; consumption state lives only in the computation layer

### Measurable Outcomes

- 100% classification accuracy across all defined scenarios in automated test suite
- API computation response: < 200ms per time entry
- Swagger coverage: 100% of endpoints with request/response examples
- All OT cascading rules, log pairing edge cases, and holiday schedule rules covered by automated tests

## Product Scope

> For full capability justification, must-have analysis, and recommended build order, see [Project Scoping & Phased Development](#project-scoping--phased-development).

### MVP — Minimum Viable Product

- **Employee & schedule management:** Define employees, shifts (including cross-date), and break windows
- **PH national holiday calendar:** Pre-loaded Regular + Special Non-Working holidays; `HOLIDAY_TYPE` enum with 4 values: `REGULAR | SPECIAL_NON_WORKING | SPECIAL_WORKING | NONE`; Special Working Days recognized as ordinary working days (no pay premium)
- **Time log management:** Clock-in/clock-out recording per employee
- **Log pairing engine:** Full algorithm with all constraints (unclaimed, not within/before another schedule, same/previous day only, earliest on previous day, OUT not within/after next schedule)
- **Time segmentation engine:** All 40 classification types — Regular Day (6), Regular Holiday (6), Special Non-Working Holiday (6), Rest Day (6), Rest Day + Regular Holiday (6), Rest Day + Special Non-Working Holiday (6), Tagged/Terminal (4)
- **Night differential computation:** Segment boundaries at 10pm and 6am; night diff variants for all 6 category groups
- **Rest day classification:** Determined from `WorkSchedulePattern` (employer-designated per Art. 91); rest day schedule approval gate; cross-date rule (shift start date governs)
- **WorkSchedulePattern entity:** `EmployeeId`, `EffectiveDate`, `ExpiryDate` (nullable), `RestDays` (collection of `DayOfWeek`), `WorkDays`, `IsActive`; source of truth for employer-designated rest days
- **Feature flag:** `HolidayPayWithinScheduledHours` (default enabled) — suppresses `REGULAR_HOLIDAY_PAID_HOURS`, `NIGHT_DIFF_REGULAR_HOLIDAY_PAID_HOURS`, `SPECIAL_HOLIDAY_PAID_HOURS`, `NIGHT_DIFF_SPECIAL_HOLIDAY_PAID_HOURS`; OT types unaffected
- **Holiday Schedule Approval queue:** Binary approve/reject with bulk multi-employee support
- **Rest Day Schedule Approval queue:** Binary approve/reject with bulk multi-employee support; combined single-request approval when rest day coincides with non-working holiday
- **OT Approval queue:** Stage → adjust → atomic commit with bulk multi-employee support; prerequisite: Holiday Schedule Approval or Rest Day Schedule Approval (whichever applies)
- **OT cascading:** Auto-remove/reinstate Regular/Special Holiday OT, Rest Day Regular/Special Holiday OT, Night Diff variants, and Early OT counterparts based on duration and boundary adjustments
- **Full Swagger/OpenAPI documentation**
- **Test coverage** for all computation, pairing, cascading, and holiday schedule rules

### Growth Features (Post-MVP)

- Flexi-time and compressed work week shift patterns
- Approval delegation / proxy approver support
- OT application audit trail and history
- Custom holiday configuration (company-specific non-working days)

### Vision (Future)

- Full RA 6727 compliance across all overtime rate categories
- Reporting and analytics (OT trends, approval rates, cost projections)
- Payroll system integration hooks (webhook/export)

## User Roles

| User | Schedules | Approvals | Feature Flags / Config | Self Clock-In/Out | Add Any-Date Logs | Attendance Reports |
|---|---|---|---|---|---|---|
| **Employee** | — | — | — | ✅ Current time only | — | Own only |
| **Manager** | ✅ | ✅ | — | — | ✅ Any date | Direct reports only |
| **HR Admin** | ✅ | — | ✅ | — | ✅ Any date | All employees |
| **API Integrator** | — | — | — | — | — | — |

## User Journeys

### Journey 1: Ana — Employee, Happy Path (Early Clock-In + Overtime)

**Opening Scene:** Ana Reyes, a production staff member, arrives at 5am — three hours before her 8am–5pm shift — because her manager needed extra hands for a product launch. She taps her badge to clock in.

**Rising Action:** Ana works through the day. The system automatically deducts her scheduled break window. The launch prep runs long; she stays well past 5pm. At 1am the next day — May 1, a public holiday — she finally clocks out.

**Climax:** The system pairs her Apr 30 5am IN with her May 1 1am OUT. It segments her time at three boundaries:
- 5am–8am → **Early OT** (180 min) — pending approval
- 8am–5pm minus break → **Normal Paid Hours** (480 min)
- 5pm–midnight → **Normal OT** (420 min) — pending approval
- midnight–1am → **Regular Holiday OT** (60 min, May 1 = Labor Day, a Regular Holiday) — pending approval

**Resolution:** Ana's time is fully and correctly computed before she reaches the parking lot. Her OT line items are in her manager's queue. She goes home confident she'll be compensated accurately once approved.

---

### Journey 2: Marco — Employee, Edge Case (Night Shift Starting on a Holiday)

**Opening Scene:** Marco Dela Cruz works nights. His schedule is May 1 10pm → May 2 7am — Labor Day into a regular workday. He clocks in at 9pm, an hour early.

**Rising Action:** The system detects that Marco's shift start (May 1) falls on a non-working holiday and checks: has the Holiday Schedule Approval been granted? Yes — his manager approved it earlier. Full holiday classifications are unlocked. The system pairs his May 1 9pm IN with his May 2 7am OUT.

**Climax:** Segmentation at four boundaries — schedule start (10pm), midnight, 6am night diff exit, and the 10pm night diff entry (coincides with schedule start here):
- May 1 9pm–10pm → **Regular Holiday Early OT** (60 min) — before schedule start, Regular Holiday date, outside ND window (9pm is before 10pm); pending approval
- May 1 10pm–midnight → **Night Diff Regular Holiday Paid Hours** (120 min) — within scheduled hours, Regular Holiday date, inside ND window (10pm–midnight)
- May 2 midnight–6am → **Night Diff Paid Hours** (360 min) — within scheduled hours, regular workday, inside ND window (midnight–6am)
- May 2 6am–7am → **Normal Paid Hours** (60 min) — within scheduled hours, regular workday, outside ND window

**Resolution:** A shift that would break most overtime engines — starting on a holiday, crossing midnight into a regular day, with early arrival — is classified correctly at every boundary.

---

### Journey 3: Carlos — Manager, Holiday Schedule Approval Queue

**Opening Scene:** It's Friday afternoon. Next Monday is a Regular Holiday (a public holiday covered by Art. 94). Carlos Santos, Operations Manager, opens the Holiday Schedule Approval queue and finds 8 employees with Monday schedules pending his decision.

**Rising Action:** Six employees were genuinely asked to work Monday — he multi-selects all six and bulk-approves in a single action. Two have standing schedules but were never called in — he rejects their holiday approval.

**Climax:** For the 6 approved employees, full holiday classification applies when their logs are processed Monday. For the 2 rejected employees, the system tags them as **Normal Paid Hours (Holiday Rest)** — paid by holiday entitlement, no logs required.

**Resolution:** Carlos finishes in under two minutes. Monday's computation is pre-configured for all 8 employees.

---

### Journey 4: Carlos — Manager, OT Approval with Reduction and Override

**Opening Scene:** Tuesday morning. Carlos opens the OT Approval queue for Monday — 4 employees pending, including Ana from Journey 1.

**Rising Action:**
- **Ana:** 180min Early OT + 420min Normal OT + 60min Regular Holiday OT. He checks logs — Ana left at 11pm. He reduces Normal OT from 420min to 360min. System auto-removes Regular Holiday OT since end time no longer crosses midnight. Committed.
- **Employee 2:** Regular Holiday OT flagged incorrectly — Carlos overrides Regular Holiday OT → Normal OT, approves.
- **Employees 3 & 4:** No adjustments — bulk-selected and committed.

**Climax:** All four employees resolved in one session with cascading adjustments applied correctly and atomically.

**Resolution:** Correct compensable time locked in for payroll. Full audit trail of every staged action Carlos made.

---

### Journey 5: Carlos — Manager, Corrects a Missed Punch

**Opening Scene:** Ben Aquino's Monday schedule shows **"Is in Current Schedule"** on Tuesday morning. Ben worked until 7pm but forgot to clock out.

**Rising Action:** Carlos adds a backdated OUT log: Monday 7pm. The system immediately re-runs computation:
- Scheduled hours: **Normal Paid Hours** (480 min)
- 5pm–7pm: **Normal OT** (120 min) — pending approval

Carlos approves the 2 hours.

**Resolution:** A missed punch corrected in seconds without burdening the employee. The any-date log entry capability fixes real-world timekeeping errors cleanly within the approval flow.

---

### Journey 6: Maria — HR Admin, Employee Onboarding + Configuration

**Opening Scene:** Maria Cruz, HR Administrator, onboards 3 new night shift employees starting tonight. She configures profiles, assigns cross-date schedules, sets break windows, and verifies holiday pay configuration.

**Rising Action:** She creates a cross-date shift: 10pm–6am, break window 2am–2:30am. Assigns it to all 3 employees for the coming month. Then sets `HolidayPayWithinScheduledHours` to **disabled** — company policy pays holiday premium on OT only, not within scheduled hours.

**Climax:** With the flag off, night shifts crossing into holidays produce Normal Paid Hours for the scheduled portion and Holiday OT only for post-schedule overtime.

**Resolution:** All 3 employees are correctly configured. Maria's policy change takes effect immediately, system-wide.

---

### Journey 7: Dev Team — API Integrator, Swagger Discovery + Dashboard Integration

**Opening Scene:** The frontend team needs to wire up log submission, computation results, and the two-tier approval workflow — all without direct access to the backend team.

**Rising Action:** They browse Swagger UI, testing each endpoint:
- `POST /employees/{id}/logs` — clock-in submission
- `GET /schedules/{id}/computation` — full segment breakdown per entry
- `POST /approvals/holiday-schedule/batch` — bulk holiday schedule approval
- `POST /approvals/overtime/{employeeId}/{date}/stage` → `.../commit` — two-phase OT approval

They test a cross-midnight Regular Holiday edge case and verify the response returns correct Regular Holiday Early OT and Regular Holiday OT segments.

**Climax:** Every endpoint documented with real examples. Dashboard integration complete without a single clarifying question to the backend team.

**Resolution:** Integration ships in a day. Swagger serves as the living contract between frontend and backend.

---

### Journey 8: Attendance Report Generation (Role-Scoped)

**Opening Scene:** Three people need attendance data on the same Friday afternoon — Ana needs her own timesheet, Carlos needs his team's weekly summary, Maria needs a full company export for payroll cutoff.

**Rising Action:**
- **Ana** calls the report endpoint — system scopes to her own records automatically. She sees her scheduled hours, OT minutes by classification, and approval status per day.
- **Carlos** calls the same endpoint — system returns only his direct reports. He identifies who has unapproved OT, who was absent, and who has pending Holiday Schedule Approvals.
- **Maria** calls with full access — all employees, any date range. She exports the complete dataset for payroll processing.

**Climax:** Same endpoint, three callers, three scopes — enforced server-side by role. No caller can access records outside their permission boundary regardless of what parameters they pass.

**Resolution:** Attendance data flows to the right people at the right scope. Access control lives at the API layer, not the client.

---

### Journey Requirements Summary

| Capability | Revealed By |
|---|---|
| Employee self-service clock-in/out (current time only) | Journeys 1, 2 |
| Any-date log entry for Manager and HR Admin | Journeys 5, 6 |
| Log pairing engine with all constraints | Journeys 1, 2, 5 |
| Holiday Schedule Approval queue (bulk, prerequisite gate) | Journeys 2, 3 |
| OT Approval queue (staged actions, atomic commit, bulk) | Journeys 4, 5 |
| OT cascading on duration adjustment | Journey 4 |
| Feature flag: `HolidayPayWithinScheduledHours` | Journey 6 |
| Cross-date schedule support | Journeys 2, 6 |
| Computation re-run on log correction | Journey 5 |
| Segment-level computation response (not just totals) | Journey 7 |
| Full Swagger/OpenAPI documentation | Journey 7 |
| Role-scoped attendance report endpoint | Journey 8 |
| Server-side access control enforcement per role | Journey 8 |

---

## Domain-Specific Requirements

### Compliance & Regulatory

- Overtime classification rules implement **RA 6727 as of the project's implementation date**. Legislative amendments to RA 6727 constitute a code-change event — rules are domain logic, not data-driven configuration.
- OT is defined relative to **scheduled shift boundaries**, not absolute daily hour limits. A 12-hour scheduled shift produces no OT within scheduled hours even if it exceeds 8 hours. OT triggers only at schedule end.
- The system computes **compensable time quantities only** — never monetary amounts. Rate computation is downstream of this API.

---

### Computation Engine Precision Requirements

#### Log Pairing Algorithm — Precision Clauses

- **Schedule traversal order:** Schedules for an employee are processed in **ascending chronological order by start time** when running log pairing. Non-deterministic traversal order is prohibited.
- **Earliest valid IN selection:** Collecting the earliest valid unclaimed IN from the previous calendar day requires gathering **all valid candidates first, then selecting the minimum timestamp** — not short-circuiting on the first found. Finding the first unclaimed IN is incorrect if a later-encountered log has an earlier timestamp.
- **"Within or before another schedule's time range" scoping:** This guard prevents an IN from being stolen by a schedule it does not belong to. It must not over-filter: an IN that would be valid as Early OT for the **currently matched schedule** must not be excluded solely because it precedes a different schedule's start. The filter applies only when the IN falls within or before a schedule **other than** the one currently being matched.
- **"Within or after next schedule's time range" (OUT guard):** An OUT is excluded if `out.Timestamp >= nextSchedule.Start`. The boundary is **inclusive on the start** of the next schedule's range.
- **Calendar-day comparisons use Asia/Manila local date**, not UTC date. "Previous calendar day" means `log.Timestamp.ToLocalTime(AsiaManila).Date == schedule.StartDate.AddDays(-1)`.
- **If no valid OUT is found** after applying all OUT constraints: the result is determined by server time vs. schedule end — `Absent` if server time ≥ schedule end, `Is in Current Schedule` if server time < schedule end.

#### Time Boundary Definitions

- **"Crosses midnight"** means the timestamp reaches or passes `00:00:00.000` of the next calendar day. Midnight is the **exclusive upper bound** of the prior day — a timestamp of exactly `00:00:00.000` belongs to the new day.
- **"Within a schedule's time range"** — both bounds are **inclusive**: `log.Timestamp >= schedule.Start AND log.Timestamp <= schedule.End`.
- **Absent vs. Is in Current Schedule boundary:** Uses strict less-than for server time — `Is in Current Schedule` when `serverTime < scheduleEnd`; `Absent` when `serverTime >= scheduleEnd`. No grace period.

#### Break Deduction

- Break deduction is computed as the **intersection of the shift's break window with the employee's clocked interval** `[IN, OUT]`.
  - If `OUT` falls before the break window starts → zero deduction.
  - If `OUT` falls during the break window → only the elapsed portion of the break is deducted.
  - If the clocked interval is entirely within the break window → full elapsed time is deducted; net compensable minutes = 0 (valid; zero-duration result must not crash).
- Break deduction applies only within **Normal Paid Hours and Holiday Paid Hours** segments. OT segments (Early OT, Normal OT, Holiday OT) are never reduced by the break window.
- A break window that crosses the schedule end boundary is truncated to the schedule end for deduction purposes; OT begins at schedule end, unaffected.

#### OT Reduction Edge Cases

- **Reducing Early OT duration to 0** is semantically equivalent to rejecting Early OT and triggers the same cascade: Early Holiday OT is removed if present.
- **Staged OT end time** is computed as `staged_start + staged_duration_minutes`. When a manager reduces Normal OT duration, the new end time is `normalOT.Start + approvedDuration`. Holiday OT is removed if this new end time does not cross into a holiday calendar date (i.e., `newEnd <= midnight` or the new end date is not a non-working holiday).

#### Computation Engine Invariants (Key Subset)

The computation engine must enforce these invariants — a `ComputationResult` violating any of them must not be constructible:

1. **Minute Conservation:** `sum(all segment minutes) == (OUT − IN) − breakDeductionMinutes`. Every minute is accounted for in exactly one segment.
2. **No Overlap:** No two segments share any minute. Segment end = next segment start.
3. **No Zero-Duration Segments:** Zero-duration segments must not be emitted.
4. **Classification Exclusivity:** Normal Paid Hours and Holiday Paid Hours are mutually exclusive per interval; Early OT and Early Holiday OT are mutually exclusive; Normal OT and Holiday OT are mutually exclusive.
5a. **Regular Holiday Gate:** `REGULAR_HOLIDAY_PAID_HOURS`, `NIGHT_DIFF_REGULAR_HOLIDAY_PAID_HOURS`, `REGULAR_HOLIDAY_EARLY_OT`, `NIGHT_DIFF_REGULAR_HOLIDAY_EARLY_OT`, `REGULAR_HOLIDAY_OT`, and `NIGHT_DIFF_REGULAR_HOLIDAY_OT` may only appear when: (a) segment date is a PH Regular Holiday; (b) the shift has an approved Holiday Schedule Approval **OR** time logs demonstrating actual work are present (Art. 94 — actual work triggers premium regardless of approval); (c) for paid-hours types only: `HolidayPayWithinScheduledHours` is enabled. When (b) is satisfied by logs alone: Regular Holiday types are emitted AND the shift is flagged for HR review. When neither approval nor logs exist: `REGULAR_HOLIDAY_REST` is emitted.
5b. **Special Holiday Gate:** `SPECIAL_HOLIDAY_PAID_HOURS`, `NIGHT_DIFF_SPECIAL_HOLIDAY_PAID_HOURS`, `SPECIAL_HOLIDAY_EARLY_OT`, `NIGHT_DIFF_SPECIAL_HOLIDAY_EARLY_OT`, `SPECIAL_HOLIDAY_OT`, and `NIGHT_DIFF_SPECIAL_HOLIDAY_OT` may only appear when: (a) segment date is a PH Special Non-Working Holiday; (b) the shift has an approved Holiday Schedule Approval **OR** time logs demonstrating actual work are present (RA 9492 — actual work triggers 130% premium regardless of approval); (c) for paid-hours types only: `HolidayPayWithinScheduledHours` is enabled. When (b) is satisfied by logs alone: Special Holiday types are emitted AND the shift is flagged for HR review. When neither approval nor logs exist: `REST_DAY_SPECIAL_HOLIDAY_UNPAID` is emitted if date is also a rest day; otherwise no segment is emitted.
6. **Early OT bounds:** Early OT and Early Holiday OT segments must end at or before schedule start.
7. **Normal/Holiday OT bounds:** Normal OT and Holiday OT segments must start at or after schedule end.
8. **Schedule-hours bounds:** Normal Paid Hours and Holiday Paid Hours must fall entirely within `[scheduleStart, scheduleEnd]`.

When multiple invariants are violated, the `ComputationResult` constructor must **collect and report all violations** — not stop at the first failure.

#### Required Domain Interfaces

The following abstractions must exist to keep computation testable and correct by construction:

| Interface | Purpose |
|---|---|
| `IHolidayCalendar` | Returns `IsNonWorkingHoliday(DateOnly)` and `GetHolidayType(DateOnly)` — never hardcoded |
| `IClockProvider` | Returns `DateTimeOffset UtcNow` — eliminates bare `DateTime.UtcNow` calls in domain logic |
| `IFeatureFlagProvider` | Returns `bool IsEnabled(string flag)` — `HolidayPayWithinScheduledHours` always read through this |
| `ILogClaimTracker` | Tracks in-memory IN/OUT claim state during a single computation pass |
| `IHolidayApprovalRepository` | Returns `HolidayApprovalStatus` (NotApplicable / Pending / Approved / Rejected) — never throws on missing record |

---

### Technical Constraints

#### Timestamps and Timezone

- **All timestamp fields are `DateTimeOffset`** throughout — domain objects, DTOs, EF Core entity models, and database columns. `DateTime` (timezone-naive) is prohibited.
- **Database column type:** `datetimeoffset` (SQL Server) or `timestamptz` (PostgreSQL). EF Core global convention: `builder.Properties<DateTimeOffset>().HaveColumnType("datetimeoffset")` applied in `OnModelCreating`.
- **System timezone:** `Asia/Manila` (UTC+8). All calendar-day boundaries (previous day, midnight, holiday lookups) evaluate against Manila local date. Server clock must be NTP-synchronized; bare system clock without NTP is not acceptable for production.

#### Authentication and Authorization

- **Mechanism:** JWT Bearer tokens, RS256 signing. ASP.NET Core policy-based authorization.
- **Token claims:** `sub` (employee ID), `role` (`Employee` | `Manager` | `HR Admin`), standard `iss`, `aud`, `exp`.
- **Role enforcement is server-side and derived from the token** — never from a query parameter. Employee report scope = `sub` claim. Manager scope = employees where manager FK = `sub`. HR Admin scope = all employees.
- **MVP token issuer:** A built-in test token endpoint signs JWTs for dev/test use. This endpoint is clearly documented as non-production. The architecture supports swapping in a real identity provider without changing the authorization layer.
- **JWT validation:** Must validate `iss`, `aud`, and `exp`. `alg: none` is rejected.

#### Data Model Constraints

- **`HolidayType` enum** on the holiday calendar entity: `REGULAR` | `SPECIAL_NON_WORKING` | `SPECIAL_WORKING` | `NONE`. `SPECIAL_WORKING` (government-declared ordinary working days despite proximity to a holiday) carries no pay premium — segments classify as Regular Day types. `NONE` is the default for ordinary calendar dates.
- **`WorkSchedulePattern` entity:** `Id` (GUID), `EmployeeId` (FK), `EffectiveDate` (DateOnly), `ExpiryDate` (DateOnly, nullable), `RestDays` (collection of `DayOfWeek`, minimum 1), `WorkDays` (collection of `DayOfWeek`, stored for validation cross-check), `IsActive` (bool), `CreatedAt`, `UpdatedAt`. Source of truth for employer-designated rest days per Art. 91 Labor Code. Shift timing stays on the shift entity — this entity answers *which days*, not *what hours*.
- **Overlapping schedules for the same employee are prohibited.** Schedule creation validates that `[start, end]` does not overlap any existing schedule for the same employee on any shared date. Overlapping schedule requests are rejected with a structured validation error.
- **Log pairing lookback window:** Configurable at system level (`OTLookbackDays`), default = 1 calendar day, maximum = 3. Guards against pairing logs from 2+ days prior.

#### API Contract

- **Enum wire values** are stable, SCREAMING_SNAKE_CASE strings. Wire values are the exact string serializations of the C# enum member names; no aliasing. Full set (40 types):
  - Regular Day: `NORMAL_PAID_HOURS`, `NIGHT_DIFF_PAID_HOURS`, `EARLY_OT`, `NIGHT_DIFF_EARLY_OT`, `NORMAL_OT`, `NIGHT_DIFF_OT`
  - Regular Holiday: `REGULAR_HOLIDAY_PAID_HOURS`, `NIGHT_DIFF_REGULAR_HOLIDAY_PAID_HOURS`, `REGULAR_HOLIDAY_EARLY_OT`, `NIGHT_DIFF_REGULAR_HOLIDAY_EARLY_OT`, `REGULAR_HOLIDAY_OT`, `NIGHT_DIFF_REGULAR_HOLIDAY_OT`
  - Special Non-Working Holiday: `SPECIAL_HOLIDAY_PAID_HOURS`, `NIGHT_DIFF_SPECIAL_HOLIDAY_PAID_HOURS`, `SPECIAL_HOLIDAY_EARLY_OT`, `NIGHT_DIFF_SPECIAL_HOLIDAY_EARLY_OT`, `SPECIAL_HOLIDAY_OT`, `NIGHT_DIFF_SPECIAL_HOLIDAY_OT`
  - Rest Day: `REST_DAY_PAID_HOURS`, `NIGHT_DIFF_REST_DAY_PAID_HOURS`, `REST_DAY_EARLY_OT`, `NIGHT_DIFF_REST_DAY_EARLY_OT`, `REST_DAY_OT`, `NIGHT_DIFF_REST_DAY_OT`
  - Rest Day + Regular Holiday: `REST_DAY_REGULAR_HOLIDAY_PAID_PREMIUM`, `NIGHT_DIFF_REST_DAY_REGULAR_HOLIDAY_PAID_PREMIUM`, `REST_DAY_REGULAR_HOLIDAY_EARLY_OT`, `NIGHT_DIFF_REST_DAY_REGULAR_HOLIDAY_EARLY_OT`, `REST_DAY_REGULAR_HOLIDAY_OT`, `NIGHT_DIFF_REST_DAY_REGULAR_HOLIDAY_OT`
  - Rest Day + Special Non-Working Holiday: `REST_DAY_SPECIAL_HOLIDAY_PAID_PREMIUM`, `NIGHT_DIFF_REST_DAY_SPECIAL_HOLIDAY_PAID_PREMIUM`, `REST_DAY_SPECIAL_HOLIDAY_EARLY_OT`, `NIGHT_DIFF_REST_DAY_SPECIAL_HOLIDAY_EARLY_OT`, `REST_DAY_SPECIAL_HOLIDAY_OT`, `NIGHT_DIFF_REST_DAY_SPECIAL_HOLIDAY_OT`
  - Tagged/Terminal: `REGULAR_HOLIDAY_REST`, `REST_DAY_SPECIAL_HOLIDAY_UNPAID`, `ABSENT`, `IN_PROGRESS`
  - Display labels are separate from wire values. Wire values are never renamed without API versioning.
- **Log submission idempotency:** Log creation endpoints accept an `Idempotency-Key` request header. Duplicate submissions with the same key within a 5-minute window are silently deduplicated — no duplicate log records created.
- **OT approval commit idempotency:** The OT approval commit endpoint requires an `Idempotency-Key`. Duplicate commit requests within a 5-minute window are silently deduplicated.
- **Bulk approval batch size:** Bulk Holiday Schedule Approval and bulk OT Approval commit endpoints cap batch size at **100 employees per request**. Clients paginate for larger sets.
- **Attendance report endpoint** uses a bulk-fetch strategy: all schedules, logs, approvals, and holiday calendar data for the requested date range are loaded in set-based queries before computation begins. The per-time-entry `< 200ms` SLA applies to the individual computation endpoint only; the report endpoint SLA is defined separately.
- **Error responses** follow **RFC 7807 Problem Details** (`application/problem+json`). Every error includes `type`, `title`, `status`, `detail`, and `instance`. Computation invariant violations, validation errors, and authorization failures each have distinct `type` URIs documented in Swagger.

---

### Approval Workflow Precision Requirements

- **Bulk commit pre-validation:** Before committing any staged actions in a batch, the system validates that all staged actions remain consistent with the current computation state (no log corrections have invalidated a referenced segment since staging). If any conflict is detected, **no actions in the batch are committed**. The response includes a structured conflict list identifying which employee/segment has the discrepancy. The manager resolves conflicts and resubmits.
- **Staged action archival:** On successful commit, all staged actions are written to an **immutable audit record** before the staging records are removed. The audit record captures: actor ID, action type, original value, adjusted value, cascade effects, and commit timestamp.
- **Feature flag audit:** All changes to `HolidayPayWithinScheduledHours` (or any system feature flag) are written to an immutable audit log with actor ID, old value, new value, and timestamp. Flag changes never block; they execute immediately.
- **Feature flag snapshot in bulk operations:** The attendance report endpoint and bulk computation operations snapshot the `HolidayPayWithinScheduledHours` flag value **once at request start** and apply it uniformly across all computations within that request. Mid-request flag changes do not affect an in-flight bulk operation.
- **Holiday calendar mutation and committed approvals:** If a non-working holiday is added to or removed from the calendar for a date that has existing committed OT approvals, those approvals are flagged as **"stale — requires re-review"**. Stale approvals remain in effect until a manager explicitly re-reviews; the system does not auto-void committed approvals.

---

### Risk Mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| UTC/Manila timezone confusion causes midnight misclassification | High | Critical | `DateTimeOffset` mandate + `IClockProvider` + Manila calendar-day evaluation |
| "Find-first IN" bug selects wrong previous-day log | High | High | Collect-all-then-minimum algorithm; named unit test `C-02` |
| Holiday classification applied to wrong segment (segment date vs. shift start date) | Medium | High | Shift start date governs holiday approval gate; test `B-06`, `B-07` |
| Same IN log claimed by two schedules in separate API calls | Medium | High | Stateless computation is idempotent; deterministic inputs → identical outputs across concurrent requests |
| Feature flag toggle invalidates staged approvals silently | Medium | High | Flag snapshot per request; stale-approval flagging on flag change |
| Bulk commit partial failure leaves inconsistent approval state | Medium | Critical | Pre-commit full-batch validation; single DB transaction for commit; rollback on any failure |
| JWT role claim spoofing allows cross-employee data access | Low | Critical | RS256 validation; `alg: none` rejected; scope derived from `sub` claim, not request parameter |
| Holiday calendar stale (proclamation not loaded) | Medium | High | Admin update endpoint; health check metric `holiday_calendar_last_updated_days`; alert if > 30 days stale |
| Overlapping schedules create ambiguous log pairing | Low | High | Schedule creation validation; overlap returns `422 Unprocessable Entity` |
| Night diff boundary at 10pm not split correctly for cross-midnight OT | Medium | High | Named boundary test: OT spanning 10pm must produce two segments |
| Rest day derived incorrectly — schedule absence vs. employer-designated rest day | Medium | High | Rest day determined from `WorkSchedulePattern.RestDays`, not inferred from shift absence; Art. 91 compliance |
| Combined approval not applied atomically for rest day + holiday | Low | Critical | Combined approval is a single DB record covering both; not two separate approvals |

---

### Night Differential Computation Rules

- **Night differential window:** 10pm (`22:00:00`) to 6am (`06:00:00`) in Asia/Manila time.
- **Segment boundaries:** The computation engine adds 10pm and 6am to the boundary list alongside schedule start, schedule end, and midnight. Any interval that spans 10pm or 6am is split at that boundary.
- **Classification rule:** A sub-interval is classified as the night differential variant of its primary type if and only if the interval falls **entirely within** the 10pm–6am window. Intervals partially inside the window are split; the inside portion gets the night diff variant, the outside portion gets the base variant.
- **Night diff applies to all segment categories:** Regular, Holiday, Rest Day, and Rest Day + Holiday. No exemptions.
- **`HolidayPayWithinScheduledHours` flag:** When disabled, it suppresses `REGULAR_HOLIDAY_PAID_HOURS`, `NIGHT_DIFF_REGULAR_HOLIDAY_PAID_HOURS`, `SPECIAL_HOLIDAY_PAID_HOURS`, and `NIGHT_DIFF_SPECIAL_HOLIDAY_PAID_HOURS`. OT types in all holiday groups remain unaffected by this flag.
- **Night diff + OT cascade:** When a manager reduces OT end time past the 10pm boundary (from inside the window to outside it), the Night Diff OT segment is automatically removed. It is reinstated if the OT end time is extended back past 10pm. The same cascade applies symmetrically for reduction past the 6am boundary on early OT.
- **Night diff does not require separate approval.** It is a computed classification applied automatically based on time of day. The manager approves total OT duration; night diff variants are derived from that duration and the 10pm/6am boundaries.

#### Night Differential Boundary Test Cases (Required)

- Shift ends exactly at 10pm: no Night Diff OT emitted.
- Shift ends at 10:01pm: Night Diff OT = 1 minute; base Normal OT ends at 10pm.
- Shift spans 9pm–11pm post-schedule: Normal OT = 60 min (9pm–10pm) + Night Diff OT = 60 min (10pm–11pm).
- Shift spans 5am–7am post-schedule (cross-midnight into morning): Night Diff OT = 60 min (5am–6am) + Normal OT = 60 min (6am–7am).
- Full 10pm–6am post-schedule: all 480 min classified as Night Diff OT only.

---

### Rest Day Computation Rules

- **Rest day determination:** A calendar date is a rest day for an employee if the employee's active `WorkSchedulePattern` designates that day of the week as a rest day. The pattern is evaluated per-employee, per-date: find the `WorkSchedulePattern` where `EffectiveDate <= date < ExpiryDate` (or `ExpiryDate IS NULL`) and `IsActive = true`; check if `DayOfWeek` is in `RestDays`. Rest day status is **declared by the employer via WorkSchedulePattern**, not inferred from shift absence.
- **No rest day without an approved Rest Day Schedule:** A rest day with logs but no approved Rest Day Schedule produces `ABSENT` — the logs cannot be matched to any schedule, and no paid entitlement exists.
- **Shift start governs rest day classification** (same rule as holidays): if a shift starts on a regular workday and crosses midnight into a rest day, the entire shift is computed as a regular workday. Rest day classifications only apply when the shift start date is itself a rest day.
- **Rest Day Schedule Approval is a prerequisite gate** for OT Approval on that rest day — the same two-tier structure as the Holiday Schedule Approval workflow.
- **Employee application:** Employees may submit a Rest Day Schedule application. Managers review and approve or reject. The system surfaces pending applications in the approval queue.

#### Rest Day + Holiday Combined Approval

- When a rest day date is also a non-working PH holiday, the approval system issues a **single combined approval request** — one manager action covers both the rest day schedule and the holiday entitlement.
- The combined approval record stores both `isRestDayApproved` and `isHolidayApproved` as a single entity; they cannot be independently approved or rejected in separate actions.
- Combined approval is a prerequisite for OT Approval on that day. Actual work without approval triggers statutory rates (Option A rule) and flags the shift for HR review.

#### Rest Day Approval Matrix Summary

| Scenario | Approval State | Logs | Result |
|---|---|---|---|
| Rest day only, no schedule applied | N/A | Any | No computation result (no schedule to compute) |
| Rest day, schedule applied, not approved | Rejected | None | Absent |
| Rest day, schedule applied, not approved | Rejected | Present | Rest Day classification applies — actual work triggers 130% (Art. 91); flagged for HR review |
| Rest day, schedule applied, approved, no logs | Approved | None | Rest Day Paid Hours *(paid by entitlement)* |
| Rest day, schedule applied, approved, logs present | Approved | Present | Full rest day classification |
| Rest day + Regular Holiday, not approved | Rejected | None | Normal Paid Hours *(tagged: Regular Holiday Rest — Art. 94 entitlement)* |
| Rest day + Regular Holiday, not approved | Rejected | Present | Rest Day + Regular Holiday classification — 260% rate; flagged for HR review |
| Rest day + Regular Holiday, approved, no logs | Approved | None | Rest Day Regular Holiday Paid Premium *(paid by entitlement)* |
| Rest day + Regular Holiday, approved, logs present | Approved | Present | Full rest day + regular holiday classification |
| Rest day + Special Holiday, not approved | Rejected | None | Rest Day Special Holiday Unpaid *(no pay, no deduction)* |
| Rest day + Special Holiday, not approved | Rejected | Present | Rest Day + Special Holiday classification — 150% rate; flagged for HR review |
| Rest day + Special Holiday, approved, no logs | Approved | None | Rest Day Special Holiday Paid Premium *(paid by entitlement)* |
| Rest day + Special Holiday, approved, logs present | Approved | Present | Full rest day + special holiday classification |

---

## Innovation & Novel Patterns

### Detected Innovation Areas

**Rule-Bounded Interval Computation Model**
The canonical innovation in `ph-payroll-time-api` is modeling a work shift as a sequence of rule-bounded intervals rather than an atomic daily unit. The computation engine constructs a sorted boundary list from all relevant timestamps — schedule start, schedule end, midnight, 10pm, 6am — and classifies each resulting sub-interval independently. No interval knows about any other interval; it only knows its own time range and the rules applicable to it. This eliminates the most common class of overtime implementation errors: cross-date shifts where the day-of-week, holiday status, or night-differential window changes mid-shift.

**Stateless Recomputation as a Correctness Strategy**
Computed overtime results are never persisted. Every call to the computation engine is a full, deterministic re-derivation from raw inputs (schedules, clock logs, holiday calendar, approval state, feature flags). This is counterintuitive in a payroll-adjacent domain where caching computed results is standard practice, but it eliminates an entire category of consistency bugs — stale cached computations that diverge from current approval or calendar state. The tradeoff is accepted explicitly: computation cost is bounded by shift complexity, not data volume.

**Legally-Grounded, Enumerable Classification Space**
Rather than modeling overtime as a rate multiplier applied to hours, the system defines an explicit, closed taxonomy of 40 classification types — each grounded in a specific provision of Philippine labor law. The taxonomy is exhaustive by design: every minute of every shift must fall into exactly one classification, enforced by the Minute Conservation invariant. This makes the system legally auditable — an external auditor can map every output segment to a statutory provision.

**Statutory Rate Primacy Over Administrative Approval**
The system architecturally separates statutory compensation obligations from employer authorization controls. Actual work performed triggers legal entitlements (Art. 94, RA 9492, Art. 91-93) regardless of whether a manager approved the schedule in advance. The approval workflow governs HR discipline and authorization records; it cannot suppress compensation. This is legally correct and architecturally separates two concerns that most overtime systems conflate.

### Market Context & Competitive Landscape

Existing overtime calculation libraries and APIs typically operate as rate multipliers — they accept hours worked and apply a multiplier based on a configured day type. This approach cannot correctly handle the cross-date, cross-holiday-boundary, cross-night-differential scenarios that Philippine labor law creates for shift workers. The segment traversal model is the differentiator: it works at the minute level across boundaries, not the day level.

As a portfolio project, the competitive frame is demonstrating engineering depth in a domain where shallow implementations are common. The value is not market differentiation — it is demonstrating that complex regulatory compliance can be modeled as precise, testable domain logic rather than heuristic special-casing.

### Validation Approach

- **Minute Conservation invariant** (Invariant #1): Every computation result must account for every minute between IN and OUT. This is the primary proof that the traversal model is complete and correct.
- **Named test case matrix**: All 40 classification types must have at least one positive and one negative test. Cross-boundary scenarios (shift spans midnight into holiday, shift spans 10pm into ND window) are named test cases, not incidental coverage.
- **Journey scenarios as acceptance tests**: The 8 user journeys in the PRD are directly translatable to integration test scenarios with exact expected segment outputs.
- **Invariant violation collection**: The `ComputationResult` constructor enforces all 8 invariants and collects all violations before throwing — making invalid engine outputs impossible to construct, not just observable after the fact.

### Risk Mitigation

| Innovation Risk | Mitigation |
|---|---|
| Segment traversal misses a boundary (e.g., midnight not added to list) | Invariant #1 (Minute Conservation) catches any gap; named test `B-01` for midnight boundary |
| Stateless recomputation produces different results on concurrent requests | Determinism guarantee: same inputs → same outputs; no shared mutable state in computation layer |
| 40-type taxonomy becomes unmaintainable as rules change | Wire values are stable SCREAMING_SNAKE_CASE; display labels are separate; rule changes are code changes, not data changes — intentional by design |
| Approval/compensation separation misunderstood by implementers | Cross-cutting rule stated explicitly in PRD; Invariants 5a and 5b define exact fallback behavior when approval gate fails |

---

## API Backend Specific Requirements

### Project-Type Overview

`ph-payroll-time-api` is a versioned internal REST API exposing overtime computation, schedule management, time log recording, approval workflows, and attendance reporting. All routes are under `/api/v1/`. The API is consumed by frontend dashboards and payroll integrations; Swagger/OpenAPI is the sole client contract — no separate SDK is provided at MVP.

---

### Endpoint Specification

#### Authentication
| Method | Route | Role | Notes |
|---|---|---|---|
| `POST` | `/api/v1/auth/token` | — | Test JWT issuer — dev/test only; documented as non-production |

#### Employees
| Method | Route | Role | Notes |
|---|---|---|---|
| `POST` | `/api/v1/employees` | Manager, HR Admin | Create employee |
| `GET` | `/api/v1/employees/{id}` | Manager, HR Admin | Get employee profile |
| `PUT` | `/api/v1/employees/{id}` | HR Admin | Update employee profile |

#### Work Schedule Patterns
| Method | Route | Role | Notes |
|---|---|---|---|
| `POST` | `/api/v1/employees/{id}/work-schedule-patterns` | Manager, HR Admin | Assign rest-day pattern |
| `GET` | `/api/v1/employees/{id}/work-schedule-patterns` | Manager, HR Admin | List active patterns |
| `PUT` | `/api/v1/work-schedule-patterns/{id}` | Manager, HR Admin | Update pattern (effective/expiry dates) |

#### Schedules
| Method | Route | Role | Notes |
|---|---|---|---|
| `POST` | `/api/v1/employees/{id}/schedules` | Manager, HR Admin | Create schedule (cross-date supported) |
| `GET` | `/api/v1/employees/{id}/schedules` | Manager, HR Admin, Employee (self) | List schedules — Employee scoped to self |
| `PUT` | `/api/v1/schedules/{id}` | Manager, HR Admin | Update schedule |
| `DELETE` | `/api/v1/schedules/{id}` | Manager, HR Admin | Delete schedule |

#### Time Logs
| Method | Route | Role | Notes |
|---|---|---|---|
| `POST` | `/api/v1/employees/{id}/logs` | Employee (self, current time only), Manager, HR Admin | Submit log; requires `Idempotency-Key` header |
| `GET` | `/api/v1/employees/{id}/logs` | Manager, HR Admin, Employee (self) | List logs with date range filter |

#### Computation
| Method | Route | Role | Notes |
|---|---|---|---|
| `GET` | `/api/v1/schedules/{id}/computation` | Manager, HR Admin, Employee (self) | Full segment breakdown for one schedule entry; < 200ms SLA |

#### Holiday Calendar
| Method | Route | Role | Notes |
|---|---|---|---|
| `GET` | `/api/v1/holidays` | All authenticated | List holidays; filter by `?from=&to=` |
| `POST` | `/api/v1/holidays` | HR Admin | Add holiday entry |
| `PUT` | `/api/v1/holidays/{date}` | HR Admin | Update holiday type or name |
| `DELETE` | `/api/v1/holidays/{date}` | HR Admin | Remove holiday |

#### Approvals — Holiday Schedule
| Method | Route | Role | Notes |
|---|---|---|---|
| `GET` | `/api/v1/approvals/holiday-schedule` | Manager | Pending queue — scoped to direct reports |
| `POST` | `/api/v1/approvals/holiday-schedule/batch` | Manager | Bulk approve/reject; max 100 employees per request |

#### Approvals — Rest Day Schedule
| Method | Route | Role | Notes |
|---|---|---|---|
| `GET` | `/api/v1/approvals/rest-day-schedule` | Manager | Pending queue — scoped to direct reports |
| `POST` | `/api/v1/approvals/rest-day-schedule/batch` | Manager | Bulk approve/reject; max 100; combined Rest Day+Holiday request handled here |

#### Approvals — Overtime
| Method | Route | Role | Notes |
|---|---|---|---|
| `GET` | `/api/v1/approvals/overtime` | Manager | Pending OT queue — scoped to direct reports |
| `POST` | `/api/v1/approvals/overtime/{employeeId}/{date}/stage` | Manager | Stage an OT action (reduce, override, reject) |
| `DELETE` | `/api/v1/approvals/overtime/{employeeId}/{date}/stage` | Manager | Remove a staged action |
| `POST` | `/api/v1/approvals/overtime/commit` | Manager | Atomic commit of all staged actions; requires `Idempotency-Key` |

#### Reports
| Method | Route | Role | Notes |
|---|---|---|---|
| `GET` | `/api/v1/reports/attendance` | Employee (self), Manager (direct reports), HR Admin (all) | Date range required; role-scoped server-side; bulk-fetch strategy |

#### Feature Flags
| Method | Route | Role | Notes |
|---|---|---|---|
| `GET` | `/api/v1/config/feature-flags` | HR Admin | List all feature flags and current values |
| `PUT` | `/api/v1/config/feature-flags/{name}` | HR Admin | Toggle flag; audit log written on change |

---

### Authentication Model

JWT Bearer, RS256, validated via ASP.NET Core `AddJwtBearer`. Claims: `sub` (employeeId), `role`, `iss`, `aud`, `exp`. `alg: none` rejected at middleware level. Scope enforcement is server-side — derived from `sub` and `role` claims only, never from query parameters.

---

### Data Schemas (Key Contracts)

#### `TimeSegment`
```json
{
  "start": "2026-04-30T17:00:00+08:00",
  "end": "2026-05-01T00:00:00+08:00",
  "durationMinutes": 420,
  "classification": "NORMAL_OT",
  "approvalStatus": "PENDING"
}
```

#### `ComputationResult`
```json
{
  "scheduleId": "...",
  "employeeId": "...",
  "scheduleStart": "2026-04-30T08:00:00+08:00",
  "scheduleEnd": "2026-04-30T17:00:00+08:00",
  "logIn": "2026-04-30T05:00:00+08:00",
  "logOut": "2026-05-01T01:00:00+08:00",
  "breakDeductionMinutes": 60,
  "segments": [],
  "hrReviewFlagged": false
}
```

#### `HolidayCalendarEntry`
```json
{
  "date": "2026-05-01",
  "name": "Labor Day",
  "type": "REGULAR"
}
```

All timestamp fields are `DateTimeOffset` (ISO 8601 with UTC offset). `DateOnly` fields serialize as `YYYY-MM-DD`.

---

### Error Codes

RFC 7807 Problem Details (`application/problem+json`) with distinct `type` URI suffixes:

| HTTP Status | `type` URI suffix | Scenario |
|---|---|---|
| 400 | `/errors/validation` | Request body fails validation |
| 401 | `/errors/unauthorized` | Missing or invalid JWT |
| 403 | `/errors/forbidden` | Valid JWT, insufficient role or out-of-scope resource |
| 404 | `/errors/not-found` | Entity not found |
| 409 | `/errors/conflict/overlapping-schedule` | Schedule overlap for same employee |
| 409 | `/errors/conflict/stale-approval` | Staged action conflicts with current computation state |
| 422 | `/errors/computation-invariant` | ComputationResult invariant violation (lists all violations) |
| 429 | `/errors/rate-limit-exceeded` | Rate limit hit |

---

### Rate Limiting

Implemented via ASP.NET Core Rate Limiting middleware (.NET 7+, built-in). Two policies:

**Standard policy** — all endpoints except bulk/report:
- Fixed window: **300 requests per minute** per authenticated `sub` claim
- Response headers: `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `Retry-After` on 429

**Bulk policy** — `/approvals/*/batch`, `/approvals/overtime/commit`, `/reports/attendance`:
- Fixed window: **20 requests per minute** per authenticated `sub` claim
- Same response headers and error format

Unauthenticated requests are rejected at JWT middleware before rate limiting applies.

---

### API Versioning

URL path versioning: all routes prefixed `/api/v1/`. Implemented via `Asp.Versioning` package.

- Default version: `v1`
- Version sunset policy: 12 months deprecation notice minimum before removing a version
- Wire values (enum strings) and `type` URI suffixes are version-stable within `v1` — breaking changes require a new version

---

### Implementation Considerations

- **Small-Controller discipline**: Route binding and response shaping only in controllers; all computation and domain logic in CQRS handlers
- **CQRS split**: Read operations (`GET /computation`, `GET /reports`) → Query handlers; Write operations and approvals → Command handlers
- **Swagger/OpenAPI**: `Swashbuckle.AspNetCore`; enum values serialized as strings (`JsonStringEnumConverter`); all 40 classification types enumerated in schema; all Problem Details `type` URIs documented with examples
- **Idempotency-Key handling**: Middleware reads header, hashes with endpoint + body, checks in-memory cache at MVP; 5-minute TTL
- **Audit log writes**: Feature flag changes and OT approval commits write to append-only audit table within the same DB transaction

---

## Project Scoping & Phased Development

### MVP Strategy & Philosophy

**MVP Approach:** Correctness-first portfolio MVP — the product must solve the full problem correctly to be a meaningful portfolio demonstration. A partial implementation (e.g., omitting Rest Day + Holiday classifications) would undermine the core differentiator (segment traversal across all legal boundaries). All 8 user journeys must be supported end-to-end.

**Developer:** Solo build.

**Timeline:** Open-ended; ship MVP as fast as possible without sacrificing correctness. Correctness is non-negotiable — the computation engine is the portfolio centrepiece.

**Resource Requirements:** Single developer with full-stack .NET ownership. No external dependencies beyond the framework.

---

### MVP Feature Set

**Core User Journeys Supported:** All 8 (Ana early OT + cross-midnight holiday, Marco holiday night shift, Carlos holiday approval, Carlos OT adjustment + cascade, Carlos missed punch correction, Maria HR onboarding + feature flag, API integrator Swagger discovery, role-scoped attendance report).

**Must-Have Capabilities:**

| Capability | Justification |
|---|---|
| All 40 classification types (6 groups + 4 tagged) | Core correctness — any missing group leaves a legal edge case unhandled |
| WorkSchedulePattern entity | Legal requirement (Art. 91); rest day without it is indefensible |
| PH holiday calendar with 4-value HOLIDAY_TYPE enum | Correctness — Special Working Days must not receive premium pay |
| Log pairing algorithm (all constraints) | Without correct pairing, every downstream computation is wrong |
| Segment traversal at all 5 boundary types | The differentiating technical capability |
| Break deduction (intersection model) | Required for Minute Conservation invariant |
| Night differential (10pm/6am boundaries) | Legal requirement (RA 6727) |
| Two-tier approval workflow (Holiday, Rest Day, OT) | Required for classification gating |
| Combined Rest Day + Holiday single approval | Required for correctness when rest day coincides with holiday |
| OT cascading (all rules) | Required for approval workflow integrity |
| ComputationResult invariant enforcement (all 8) | Correctness by construction — not a test strategy |
| JWT RS256 auth + role-scoped access | Required for report scoping and approval gating |
| Idempotency-Key on log submission + OT commit | Required to prevent duplicate records under retry |
| Rate limiting (built-in middleware, 2 policies) | Low-effort, high-signal portfolio inclusion |
| API versioning (`/api/v1/`) | Low-effort; removes future breaking change debt |
| RFC 7807 Problem Details error responses | Required for Swagger contract completeness |
| Full Swagger/OpenAPI with all 40 types documented | Portfolio requirement — living contract |
| Test coverage (all named edge cases) | Required for classification accuracy claim |

**Nice-to-Have at MVP (include if time allows, not blocking):**

| Capability | Reason Deferrable |
|---|---|
| HR review flag surfaced in a dedicated queue endpoint | Logs-with-no-approval appear in OT queue already; dedicated queue is a UX improvement, not a correctness requirement |
| Health check endpoint (`/health`) with `holiday_calendar_last_updated_days` metric | Useful for production ops; not needed for portfolio demo |
| Pagination on list endpoints | Initial implementation can return all within a reasonable date range; pagination is an enhancement |

---

### Post-MVP Features (Growth)

Dependencies on MVP being stable and correct.

| Feature | Value Add |
|---|---|
| Flexi-time and compressed work week shift patterns | Expands employee type coverage |
| Approval delegation / proxy approver support | Operational convenience |
| OT application audit trail and history | Compliance and transparency |
| Custom holiday configuration (company-specific non-working days) | Tenant-specific holiday support |

---

### Vision (Future)

| Feature | Value Add |
|---|---|
| Full RA 6727 compliance across all OT rate categories | Widens legal coverage |
| Reporting and analytics (OT trends, approval rates, cost projections) | Business intelligence layer |
| Payroll system integration hooks (webhook/export) | Downstream system connectivity |

---

### Risk Mitigation Strategy

**Technical Risks:**
The segment traversal model and the 40-type invariant enforcement are the highest-complexity pieces. Both are mitigated by the `ComputationResult` self-validating constructor — an incorrect traversal cannot produce a valid result. The named test case matrix (one positive + one negative per classification type) catches regressions early. Build the computation engine and its tests first, before any controller or persistence layer.

**Resource Risks (Solo Developer):**
Recommended build order: domain model and computation engine → persistence layer → CQRS handlers → controllers → approval workflow → reports → Swagger polish. Each layer is independently testable. If time pressure emerges, the nice-to-have list above is the safe cut line — core correctness is not negotiable.

**Schedule Risk:**
"ASAP" with open-ended timeline means correctness gates shipping, not a calendar date. Define "done" as all 8 user journeys passing integration tests — not feature-complete Growth tier.

---

## Functional Requirements

### Employee & Schedule Management

- **FR1:** Manager and HR Admin can create an employee profile
- **FR2:** HR Admin can update an employee profile
- **FR3:** Manager and HR Admin can create a shift schedule for an employee, including cross-date shifts and a break window
- **FR4:** Manager and HR Admin can update or delete an existing shift schedule for an employee
- **FR5:** The system prevents creation of overlapping schedules for the same employee on any shared date
- **FR6:** Manager and HR Admin can assign a work schedule pattern to an employee with an effective date and optional expiry date
- **FR7:** Manager and HR Admin can update a work schedule pattern's effective or expiry date
- **FR8:** Employee can view their own schedules

### Time Log Recording

- **FR9:** Employee can submit a time log at the current server time only
- **FR10:** Manager and HR Admin can submit a time log for any employee on any date
- **FR11:** Manager and HR Admin can view all time logs for any employee within a date range
- **FR12:** Employee can view their own time logs
- **FR13:** The system deduplicates time log submissions sharing the same Idempotency-Key within the deduplication window

### Overtime Computation Engine

- **FR14:** The system classifies every minute of a schedule entry into exactly one of the 40 defined classification types, determined by schedule boundaries, time logs, holiday calendar, work schedule pattern, and approval state
- **FR15:** The system deducts break time as the intersection of the shift's break window with the employee's clocked interval
- **FR16:** The system splits any interval spanning the night differential window (10pm–6am) at the 10pm and 6am boundaries, applying the night differential classification variant to the inside portion
- **FR17:** The system derives rest day status for an employee and date from the employee's active WorkSchedulePattern; rest day is not inferred from shift absence
- **FR18:** The system governs holiday and rest day classification by the shift start date; segments that cross midnight inherit the rules applicable at shift start
- **FR19:** The system pairs IN and OUT logs using the full log pairing algorithm: same-day or immediately prior calendar day, earliest valid IN from prior day, OUT not within or after the next schedule's time range, IN not within or before another schedule's time range
- **FR20:** When no valid OUT exists and server time is before schedule end, the system returns `IN_PROGRESS` as the sole result for that schedule entry
- **FR21:** When no valid OUT exists and server time is at or after schedule end, the system returns `ABSENT`
- **FR22:** On a Regular Holiday date with no approved schedule and no time logs, the system returns `REGULAR_HOLIDAY_REST` (100% paid by Art. 94 entitlement)
- **FR23:** On a Special Non-Working Holiday date with no approved schedule and no time logs, the system returns no result (no pay, no deduction)
- **FR24:** When time logs are present regardless of approval status, the system applies statutory compensation rates and flags the computation result for HR review
- **FR25:** The system rejects any computation result that violates any of the 8 defined invariants (Minute Conservation, No Overlap, No Zero-Duration, Classification Exclusivity, Regular Holiday Gate, Special Holiday Gate, OT Bounds, Schedule-Hours Bounds), collecting all violations before rejection

### Approval Workflow

- **FR26:** Manager can view pending Holiday Schedule Approval requests scoped to their direct reports
- **FR27:** Manager can bulk approve or reject Holiday Schedule Approval requests for up to 100 employees per request
- **FR28:** Manager can view pending Rest Day Schedule Approval requests scoped to their direct reports
- **FR29:** Manager can bulk approve or reject Rest Day Schedule Approval requests for up to 100 employees per request
- **FR30:** When a rest day date coincides with a non-working holiday, the system issues a single combined approval request covering both rest day schedule and holiday entitlement; both cannot be independently approved or rejected
- **FR31:** Manager can view pending OT Approval requests scoped to their direct reports, including all staged segment classifications and durations
- **FR32:** Manager can stage OT adjustment actions (reduce duration, override classification, reject) for individual employee segments before committing
- **FR33:** Manager can remove a staged OT action before committing
- **FR34:** The system automatically removes and reinstates OT segment classifications when a manager reduces OT duration past a classification boundary (midnight or 10pm night diff threshold)
- **FR35:** Manager can atomically commit all staged OT actions for one or more employees in a single request; the system validates all staged actions against current computation state before committing and rejects the entire batch if any conflict is detected
- **FR36:** Holiday Schedule Approval and Rest Day Schedule Approval are prerequisite gates for OT Approval on the same date
- **FR37:** The system deduplicates OT commit requests sharing the same Idempotency-Key within the deduplication window
- **FR38:** The system writes an immutable audit record of all staged actions, cascade effects, actor, and timestamp on each successful OT commit

### Holiday Calendar Management

- **FR39:** All authenticated users can view PH holiday calendar entries filtered by date range
- **FR40:** HR Admin can add, update, and remove holiday entries with a date, name, and HOLIDAY_TYPE (REGULAR, SPECIAL_NON_WORKING, SPECIAL_WORKING, NONE)
- **FR41:** The system treats Special Working Day dates as ordinary working days with no pay premium
- **FR42:** The system flags existing committed OT approvals as stale when a holiday is added or removed for a date that has existing approvals; stale approvals remain in effect until a manager explicitly re-reviews

### Attendance Reporting

- **FR43:** Employee can retrieve their own attendance report for a specified date range, including per-schedule segment breakdowns and approval status
- **FR44:** Manager can retrieve attendance reports for their direct reports only
- **FR45:** HR Admin can retrieve attendance reports for all employees
- **FR46:** The system enforces report scope server-side from the caller's JWT claims; no request parameter can expand a caller's scope beyond their role boundary

### System Configuration & Feature Flags

- **FR47:** HR Admin can view all system feature flags and their current values
- **FR48:** HR Admin can toggle the `HolidayPayWithinScheduledHours` flag; when disabled, Regular Holiday Paid Hours and Special Holiday Paid Hours (and their night diff variants) are suppressed while OT types remain unaffected
- **FR49:** All feature flag changes are written to an immutable audit log with actor, timestamp, old value, and new value

### API Platform, Security & Contracts

- **FR50:** The system authenticates all API requests via JWT Bearer tokens; requests with missing, expired, or invalid tokens are rejected before processing
- **FR51:** The system derives user identity and access scope exclusively from JWT claims (`sub`, `role`); request parameters cannot substitute for or override claims
- **FR52:** All API routes are accessible under the `/api/v1/` path prefix; breaking changes require a new version
- **FR53:** All error responses follow RFC 7807 Problem Details format with a distinct `type` URI per error category
- **FR54:** The system enforces rate limits per authenticated user with a standard policy (all endpoints) and a stricter bulk policy (batch approval and report endpoints); exceeded limits return structured error responses with `Retry-After`
- **FR55:** A test JWT issuer endpoint is available for development and testing purposes and is documented as non-production
- **FR56:** All endpoints, request/response schemas, the complete 40-type classification enum, and all Problem Details `type` URIs are documented in Swagger/OpenAPI with examples

---

## Non-Functional Requirements

### Performance

- **NFR-P1:** The individual computation endpoint (`GET /api/v1/schedules/{id}/computation`) responds in < 200ms at the 95th percentile under normal operating conditions
- **NFR-P2:** The attendance report endpoint completes for a 31-day range across up to 100 employees in < 5 seconds at the 95th percentile
- **NFR-P3:** Bulk approval batch commit for 100 employees completes in < 3 seconds at the 95th percentile
- **NFR-P4:** All non-computation endpoints respond in < 500ms at the 95th percentile

### Security

- **NFR-S1:** All API traffic is served over HTTPS; HTTP connections are rejected at the transport layer
- **NFR-S2:** JWT tokens must be RS256-signed; `alg: none` and symmetric-algorithm tokens are rejected at middleware before any business logic executes
- **NFR-S3:** JWT `exp`, `iss`, and `aud` claims are validated on every authenticated request; expired or mismatched tokens are rejected with `401`
- **NFR-S4:** Employee compensation data (logs, computation results, approval state) is only accessible within the caller's role-defined scope, enforced server-side on every request
- **NFR-S5:** Rate limiting is enforced per authenticated `sub` claim, not per IP; switching IPs does not reset a user's rate limit window
- **NFR-S6:** All database interactions use ORM-generated or parameterized queries; raw string concatenation into SQL is prohibited throughout the codebase

### Reliability & Data Integrity

- **NFR-R1:** OT approval commits are fully atomic — all staged actions persist together or none do; the database must not contain a partial commit state
- **NFR-R2:** Audit log records for OT commits and feature flag changes are written within the same database transaction as the committed action; an audit record must never exist without its corresponding action also having persisted
- **NFR-R3:** The computation engine is deterministic — identical inputs (schedule, logs, approvals, holiday calendar, feature flags, server time) always produce identical output regardless of call order or concurrency
- **NFR-R4:** Idempotent endpoints deduplicate within the 5-minute window regardless of concurrent requests from the same client
- **NFR-R5:** Server clock is NTP-synchronized; bare OS system time without NTP is not acceptable for production deployment

### Testability & Correctness

- **NFR-T1:** All 40 classification types have at least one automated positive test case and one automated negative test case in the test suite
- **NFR-T2:** All 8 computation engine invariants have dedicated test cases verifying that violations are detected, all violations are collected before rejection, and the invalid result cannot be constructed
- **NFR-T3:** All 8 user journey scenarios have corresponding integration test cases with exact expected segment output assertions (classification, duration, approval status per segment)
- **NFR-T4:** The computation engine is testable without a live database, HTTP server, or real clock — all external dependencies are injected through the 5 defined domain interfaces (`IHolidayCalendar`, `IClockProvider`, `IFeatureFlagProvider`, `ILogClaimTracker`, `IHolidayApprovalRepository`)
