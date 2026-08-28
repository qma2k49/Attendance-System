using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AttendanceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSprint5Tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "monthly_timesheet_summaries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    standard_working_days = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false, defaultValue: 0.0m),
                    actual_working_days = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false, defaultValue: 0.0m),
                    actual_working_hours = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false, defaultValue: 0.00m),
                    paid_leave_days = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false, defaultValue: 0.0m),
                    unpaid_leave_days = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false, defaultValue: 0.0m),
                    absent_days = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false, defaultValue: 0.0m),
                    late_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    early_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    late_occurrences = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    early_occurrences = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    overtime_hours = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false, defaultValue: 0.00m),
                    total_payable_days = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false, defaultValue: 0.0m),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "DRAFT"),
                    finalized_by = table.Column<int>(type: "integer", nullable: true),
                    finalized_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monthly_timesheet_summaries", x => x.id);
                    table.CheckConstraint("chk_timesheet_month", "month >= 1 AND month <= 12");
                    table.CheckConstraint("chk_timesheet_year", "year >= 2000");
                    table.ForeignKey(
                        name: "fk_monthly_timesheets_employee",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_monthly_timesheets_finalizer",
                        column: x => x.finalized_by,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_monthly_timesheets_period",
                table: "monthly_timesheet_summaries",
                columns: new[] { "year", "month" });

            migrationBuilder.CreateIndex(
                name: "idx_monthly_timesheets_status",
                table: "monthly_timesheet_summaries",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_monthly_timesheet_summaries_finalized_by",
                table: "monthly_timesheet_summaries",
                column: "finalized_by");

            migrationBuilder.CreateIndex(
                name: "uq_monthly_timesheet_emp_period",
                table: "monthly_timesheet_summaries",
                columns: new[] { "employee_id", "year", "month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "monthly_timesheet_summaries");
        }
    }
}
