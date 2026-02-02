using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Converge.Configuration.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOutboxEventAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dispatchedat",
                schema: "public",
                table: "outboxevents");

            // Convert correlationid from text to uuid with USING clause for PostgreSQL
            migrationBuilder.Sql(
                "ALTER TABLE public.outboxevents ALTER COLUMN correlationid TYPE uuid USING correlationid::uuid;");

            migrationBuilder.AddColumn<Guid>(
                name: "domainid",
                schema: "public",
                table: "outboxevents",
                type: "uuid",
                nullable: true);

            // Convert correlationid from text to uuid in companyconfigevents
            migrationBuilder.Sql(
                "ALTER TABLE public.companyconfigevents ALTER COLUMN correlationid TYPE uuid USING correlationid::uuid;");

            migrationBuilder.AddColumn<Guid>(
                name: "domainid",
                schema: "public",
                table: "companyconfigevents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_outboxevents_key_scope_tenantid_companyid",
                schema: "public",
                table: "outboxevents",
                columns: new[] { "key", "scope", "tenantid", "companyid" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outboxevents_key_scope_tenantid_companyid",
                schema: "public",
                table: "outboxevents");

            migrationBuilder.DropColumn(
                name: "domainid",
                schema: "public",
                table: "outboxevents");

            migrationBuilder.DropColumn(
                name: "domainid",
                schema: "public",
                table: "companyconfigevents");

            migrationBuilder.AlterColumn<string>(
                name: "correlationid",
                schema: "public",
                table: "outboxevents",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTime>(
                name: "dispatchedat",
                schema: "public",
                table: "outboxevents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "correlationid",
                schema: "public",
                table: "companyconfigevents",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
