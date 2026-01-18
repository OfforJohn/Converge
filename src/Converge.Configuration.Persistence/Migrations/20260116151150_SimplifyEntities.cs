using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Converge.Configuration.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "companyconfigevents",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    scope = table.Column<int>(type: "integer", nullable: false),
                    tenantid = table.Column<Guid>(type: "uuid", nullable: true),
                    companyid = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: true),
                    eventtype = table.Column<string>(type: "text", nullable: false),
                    correlationid = table.Column<string>(type: "text", nullable: false),
                    occurredat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    dispatched = table.Column<bool>(type: "boolean", nullable: false),
                    dispatchedat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companyconfigevents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "domains",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_domains", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outboxevents",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    scope = table.Column<int>(type: "integer", nullable: false),
                    tenantid = table.Column<Guid>(type: "uuid", nullable: true),
                    companyid = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: true),
                    eventtype = table.Column<string>(type: "text", nullable: false),
                    correlationid = table.Column<string>(type: "text", nullable: false),
                    occurredat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    dispatched = table.Column<bool>(type: "boolean", nullable: false),
                    dispatchedat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outboxevents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "configurationitems",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    scope = table.Column<int>(type: "integer", nullable: false),
                    tenantid = table.Column<Guid>(type: "uuid", nullable: true),
                    companyid = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    createdby = table.Column<Guid>(type: "uuid", nullable: true),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    domainid = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configurationitems", x => x.id);
                    table.ForeignKey(
                        name: "FK_configurationitems_domains_domainid",
                        column: x => x.domainid,
                        principalSchema: "public",
                        principalTable: "domains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_configurationitems_domainid",
                schema: "public",
                table: "configurationitems",
                column: "domainid");

            migrationBuilder.CreateIndex(
                name: "IX_configurationitems_key_scope_tenantid_version",
                schema: "public",
                table: "configurationitems",
                columns: new[] { "key", "scope", "tenantid", "version" });

            migrationBuilder.CreateIndex(
                name: "IX_outboxevents_dispatched",
                schema: "public",
                table: "outboxevents",
                column: "dispatched");

            migrationBuilder.CreateIndex(
                name: "IX_outboxevents_occurredat",
                schema: "public",
                table: "outboxevents",
                column: "occurredat");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "companyconfigevents",
                schema: "public");

            migrationBuilder.DropTable(
                name: "configurationitems",
                schema: "public");

            migrationBuilder.DropTable(
                name: "outboxevents",
                schema: "public");

            migrationBuilder.DropTable(
                name: "domains",
                schema: "public");
        }
    }
}
