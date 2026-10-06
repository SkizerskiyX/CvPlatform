using CvPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CvPlatform.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261006220000_AddSupportDropboxConnection")]
public sealed class AddSupportDropboxConnection : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TABLE "SupportDropboxConnection" (
            "Id" integer PRIMARY KEY CHECK ("Id" = 1),
            "AppKey" text NOT NULL,
            "EncryptedToken" text NOT NULL,
            "ConnectedAt" timestamp with time zone NOT NULL
        );
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("SupportDropboxConnection");
}
