using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PhPayrollTimeApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    actor_sub_claim = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    jwt_subject_claim = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "feature_flags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_by_sub_claim = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feature_flags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "holiday_calendar_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_holiday_calendar_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "holiday_schedule_approvals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    holiday_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    approved_by_sub_claim = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_holiday_schedule_approvals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ot_approvals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    committed_by_sub_claim = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    committed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ot_approvals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rest_day_schedule_approvals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rest_day_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    approved_by_sub_claim = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rest_day_schedule_approvals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shift_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    schedule_start = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    schedule_end = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_schedules", x => x.id);
                    table.ForeignKey(
                        name: "FK_shift_schedules_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "time_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    log_type = table.Column<string>(type: "text", nullable: false),
                    logged_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_time_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_time_logs_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_schedule_patterns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rest_days = table.Column<int[]>(type: "integer[]", nullable: false),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_schedule_patterns", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_schedule_patterns_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "staged_ot_actions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ot_approval_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_type = table.Column<string>(type: "text", nullable: false),
                    segment_classification = table.Column<string>(type: "text", nullable: true),
                    adjusted_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staged_ot_actions", x => x.id);
                    table.ForeignKey(
                        name: "FK_staged_ot_actions_ot_approvals_ot_approval_id",
                        column: x => x.ot_approval_id,
                        principalTable: "ot_approvals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shift_schedule_break_windows",
                columns: table => new
                {
                    shift_schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    break_start = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    break_end = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_schedule_break_windows", x => new { x.shift_schedule_id, x.Id });
                    table.ForeignKey(
                        name: "FK_shift_schedule_break_windows_shift_schedules_shift_schedule~",
                        column: x => x.shift_schedule_id,
                        principalTable: "shift_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employees_employee_number",
                table: "employees",
                column: "employee_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_jwt_subject_claim",
                table: "employees",
                column: "jwt_subject_claim",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_feature_flags_name",
                table: "feature_flags",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_holiday_calendar_entries_date",
                table: "holiday_calendar_entries",
                column: "date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shift_schedules_employee_id",
                table: "shift_schedules",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_staged_ot_actions_ot_approval_id",
                table: "staged_ot_actions",
                column: "ot_approval_id");

            migrationBuilder.CreateIndex(
                name: "IX_time_logs_employee_id_logged_at",
                table: "time_logs",
                columns: new[] { "employee_id", "logged_at" });

            migrationBuilder.CreateIndex(
                name: "IX_work_schedule_patterns_employee_id",
                table: "work_schedule_patterns",
                column: "employee_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_records");

            migrationBuilder.DropTable(
                name: "feature_flags");

            migrationBuilder.DropTable(
                name: "holiday_calendar_entries");

            migrationBuilder.DropTable(
                name: "holiday_schedule_approvals");

            migrationBuilder.DropTable(
                name: "rest_day_schedule_approvals");

            migrationBuilder.DropTable(
                name: "shift_schedule_break_windows");

            migrationBuilder.DropTable(
                name: "staged_ot_actions");

            migrationBuilder.DropTable(
                name: "time_logs");

            migrationBuilder.DropTable(
                name: "work_schedule_patterns");

            migrationBuilder.DropTable(
                name: "shift_schedules");

            migrationBuilder.DropTable(
                name: "ot_approvals");

            migrationBuilder.DropTable(
                name: "employees");
        }
    }
}
