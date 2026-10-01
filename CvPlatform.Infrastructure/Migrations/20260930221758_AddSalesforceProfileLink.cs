using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CvPlatform.Infrastructure.Migrations
{
    public partial class AddSalesforceProfileLink : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SalesforceAccountId",
                table: "UserProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SalesforceConnectedAt",
                table: "UserProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SalesforceContactId",
                table: "UserProfiles",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SalesforceAccountId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "SalesforceConnectedAt",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "SalesforceContactId",
                table: "UserProfiles");
        }
    }
}
