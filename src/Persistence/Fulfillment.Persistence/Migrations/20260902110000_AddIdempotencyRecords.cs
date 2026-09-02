using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FulfillmentPlatform.Persistence.Migrations;

[DbContext(typeof(FulfillmentDbContext))]
[Migration("20260902110000_AddIdempotencyRecords")]
public partial class AddIdempotencyRecords : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "idempotency_records",
            columns: table => new
            {
                operation = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                response_status_code = table.Column<int>(type: "integer", nullable: true),
                response_body = table.Column<string>(type: "jsonb", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_idempotency_records", x => new { x.operation, x.key }));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "idempotency_records");
    }
}
