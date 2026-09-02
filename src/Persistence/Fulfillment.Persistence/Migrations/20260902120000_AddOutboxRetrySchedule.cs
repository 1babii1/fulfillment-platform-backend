using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FulfillmentPlatform.Persistence.Migrations;

[DbContext(typeof(FulfillmentDbContext))]
[Migration("20260902120000_AddOutboxRetrySchedule")]
public partial class AddOutboxRetrySchedule : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "next_attempt_at",
            table: "outbox_messages",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.DropIndex(
            name: "IX_outbox_messages_processed_at_occurred_at",
            table: "outbox_messages");

        migrationBuilder.CreateIndex(
            name: "IX_outbox_messages_processed_at_next_attempt_at_occurred_at",
            table: "outbox_messages",
            columns: new[] { "processed_at", "next_attempt_at", "occurred_at" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_outbox_messages_processed_at_next_attempt_at_occurred_at",
            table: "outbox_messages");

        migrationBuilder.DropColumn(name: "next_attempt_at", table: "outbox_messages");

        migrationBuilder.CreateIndex(
            name: "IX_outbox_messages_processed_at_occurred_at",
            table: "outbox_messages",
            columns: new[] { "processed_at", "occurred_at" });
    }
}
