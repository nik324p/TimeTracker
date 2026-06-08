using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cards",
                columns: table => new
                {
                    card_uid = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cards", x => x.card_uid);
                    table.ForeignKey(
                        name: "fk_cards_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "schedule_exclusions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    type_exclusion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    start_datetime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    end_datetime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_schedule_exclusions", x => x.id);
                    table.ForeignKey(
                        name: "fk_schedule_exclusions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_days",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    arrival_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    departure_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_days", x => x.id);
                    table.ForeignKey(
                        name: "fk_work_days_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_schedules",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    days = table.Column<int>(type: "integer", nullable: false),
                    free_schedule = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_schedules", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_work_schedules_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cards_user_id",
                table: "cards",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_schedule_exclusions_user_id",
                table: "schedule_exclusions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_days_date",
                table: "work_days",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_work_days_user_id_date",
                table: "work_days",
                columns: new[] { "user_id", "date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cards");

            migrationBuilder.DropTable(
                name: "schedule_exclusions");

            migrationBuilder.DropTable(
                name: "work_days");

            migrationBuilder.DropTable(
                name: "work_schedules");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
