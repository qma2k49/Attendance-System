using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AttendanceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSprint4Tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attendance_adjustments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    work_date = table.Column<DateOnly>(type: "date", nullable: false),
                    adjustment_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    adjusted_check_in = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    adjusted_check_out = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "PENDING"),
                    approver_id = table.Column<int>(type: "integer", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_adjustments", x => x.id);
                    table.ForeignKey(
                        name: "fk_adjustments_approver",
                        column: x => x.approver_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_adjustments_employee",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "leave_requests",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    leave_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    to_date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_days = table.Column<decimal>(type: "numeric(3,1)", precision: 3, scale: 1, nullable: false, defaultValue: 1.0m),
                    reason = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "PENDING"),
                    approver_id = table.Column<int>(type: "integer", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_requests", x => x.id);
                    table.CheckConstraint("chk_leave_dates", "to_date >= from_date");
                    table.ForeignKey(
                        name: "fk_leave_requests_approver",
                        column: x => x.approver_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_leave_requests_employee",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_adjustments_emp_date",
                table: "attendance_adjustments",
                columns: new[] { "employee_id", "work_date" });

            migrationBuilder.CreateIndex(
                name: "idx_adjustments_status",
                table: "attendance_adjustments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_adjustments_approver_id",
                table: "attendance_adjustments",
                column: "approver_id");

            migrationBuilder.CreateIndex(
                name: "idx_leave_requests_emp",
                table: "leave_requests",
                columns: new[] { "employee_id", "from_date", "to_date" });

            migrationBuilder.CreateIndex(
                name: "idx_leave_requests_status",
                table: "leave_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_approver_id",
                table: "leave_requests",
                column: "approver_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_adjustments");

            migrationBuilder.DropTable(
                name: "leave_requests");
        }
    }
}
