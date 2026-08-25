using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AttendanceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSprint2Tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "device_sync_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    device_id = table.Column<int>(type: "integer", nullable: false),
                    sync_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "AUTO_SCHEDULED"),
                    records_pulled = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    records_inserted = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "SUCCESS"),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_sync_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_sync_logs_device",
                        column: x => x.device_id,
                        principalTable: "attendance_devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "raw_attendance_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    device_id = table.Column<int>(type: "integer", nullable: false),
                    device_user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    check_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    verify_mode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "FINGERPRINT"),
                    processed_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "PENDING"),
                    raw_payload = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_attendance_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_raw_logs_device",
                        column: x => x.device_id,
                        principalTable: "attendance_devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_sync_logs_device_id",
                table: "device_sync_logs",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "idx_raw_logs_status",
                table: "raw_attendance_logs",
                column: "processed_status");

            migrationBuilder.CreateIndex(
                name: "uq_raw_logs_dedup",
                table: "raw_attendance_logs",
                columns: new[] { "device_id", "device_user_id", "check_time" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "device_sync_logs");

            migrationBuilder.DropTable(
                name: "raw_attendance_logs");
        }
    }
}
