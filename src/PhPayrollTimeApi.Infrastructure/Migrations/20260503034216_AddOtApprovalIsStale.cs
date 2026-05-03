using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhPayrollTimeApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOtApprovalIsStale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_stale",
                table: "ot_approvals",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_stale",
                table: "ot_approvals");
        }
    }
}
