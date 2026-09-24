using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace CvPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncCurrentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cvs_Positions_PositionId",
                table: "Cvs");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionAttributes_AttributeDefinitions_AttributeDefinition~",
                table: "PositionAttributes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfileAttributeValues_AttributeDefinitions_AttributeDefini~",
                table: "ProfileAttributeValues");

            migrationBuilder.DropTable(
                name: "CvAttributeValues");

            migrationBuilder.DropIndex(
                name: "IX_DiscussionPosts_PositionId",
                table: "DiscussionPosts");

            migrationBuilder.DropIndex(
                name: "IX_Cvs_PositionId",
                table: "Cvs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ProfileAttributeValues");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "PositionAttributes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PositionAttributes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Likes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "DiscussionPosts");

            migrationBuilder.DropColumn(
                name: "IncludedProjectIds",
                table: "Cvs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Cvs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AttributeOptions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AttributeDefinitions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AttributeCategories");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AccessRules");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "UserProfiles",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Projects",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Projects",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AlterColumn<string>(
                name: "StringValue",
                table: "ProfileAttributeValues",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ProfileAttributeValues",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "ProfileAttributeValues",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<string>(
                name: "Company",
                table: "Positions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Level",
                table: "Positions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxProjects",
                table: "Positions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string[]>(
                name: "ProjectTags",
                table: "Positions",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Positions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "PositionAttributes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "PositionAttributes",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Likes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Likes",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "DiscussionPosts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "DiscussionPosts",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "Cvs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Cvs",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AttributeOptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "AttributeOptions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<string>(
                name: "SystemKey",
                table: "AttributeDefinitions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "AttributeDefinitions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "AttributeCategories",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<string>(
                name: "PreferredLanguage",
                table: "AspNetUsers",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredTheme",
                table: "AspNetUsers",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AccessRules",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "AccessRules",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "UserProfiles",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "to_tsvector('simple'::regconfig, coalesce(\"FirstName\", '') || ' ' || coalesce(\"LastName\", '') || ' ' || coalesce(\"Location\", ''))",
                stored: true);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Projects",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "to_tsvector('simple'::regconfig, coalesce(\"Name\", '') || ' ' || coalesce(\"Description\", ''))",
                stored: true);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "ProfileAttributeValues",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "to_tsvector('simple'::regconfig, coalesce(\"StringValue\", ''))",
                stored: true);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Positions",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "to_tsvector('simple'::regconfig, coalesce(\"Title\", '') || ' ' || coalesce(\"ShortDescription\", '') || ' ' || coalesce(\"Company\", ''))",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_SearchVector",
                table: "UserProfiles",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_SearchVector",
                table: "Projects",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Tags",
                table: "Projects",
                column: "Tags")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileAttributeValues_SearchVector",
                table: "ProfileAttributeValues",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_SearchVector",
                table: "Positions",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_UpdatedAt",
                table: "Positions",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Likes_RecruiterProfileId",
                table: "Likes",
                column: "RecruiterProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionPosts_AuthorProfileId",
                table: "DiscussionPosts",
                column: "AuthorProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionPosts_PositionId_CreatedAt",
                table: "DiscussionPosts",
                columns: new[] { "PositionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Cvs_CreatedAt",
                table: "Cvs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Cvs_PositionId_Status",
                table: "Cvs",
                columns: new[] { "PositionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AttributeDefinitions_SystemKey",
                table: "AttributeDefinitions",
                column: "SystemKey",
                unique: true,
                filter: "\"SystemKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccessRules_AttributeDefinitionId",
                table: "AccessRules",
                column: "AttributeDefinitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccessRules_AttributeDefinitions_AttributeDefinitionId",
                table: "AccessRules",
                column: "AttributeDefinitionId",
                principalTable: "AttributeDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Cvs_Positions_PositionId",
                table: "Cvs",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DiscussionPosts_UserProfiles_AuthorProfileId",
                table: "DiscussionPosts",
                column: "AuthorProfileId",
                principalTable: "UserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Likes_UserProfiles_RecruiterProfileId",
                table: "Likes",
                column: "RecruiterProfileId",
                principalTable: "UserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionAttributes_AttributeDefinitions_AttributeDefinition~",
                table: "PositionAttributes",
                column: "AttributeDefinitionId",
                principalTable: "AttributeDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileAttributeValues_AttributeDefinitions_AttributeDefini~",
                table: "ProfileAttributeValues",
                column: "AttributeDefinitionId",
                principalTable: "AttributeDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_AspNetUsers_IdentityUserId",
                table: "UserProfiles",
                column: "IdentityUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccessRules_AttributeDefinitions_AttributeDefinitionId",
                table: "AccessRules");

            migrationBuilder.DropForeignKey(
                name: "FK_Cvs_Positions_PositionId",
                table: "Cvs");

            migrationBuilder.DropForeignKey(
                name: "FK_DiscussionPosts_UserProfiles_AuthorProfileId",
                table: "DiscussionPosts");

            migrationBuilder.DropForeignKey(
                name: "FK_Likes_UserProfiles_RecruiterProfileId",
                table: "Likes");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionAttributes_AttributeDefinitions_AttributeDefinition~",
                table: "PositionAttributes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfileAttributeValues_AttributeDefinitions_AttributeDefini~",
                table: "ProfileAttributeValues");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProfiles_AspNetUsers_IdentityUserId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_SearchVector",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Projects_SearchVector",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_Tags",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_ProfileAttributeValues_SearchVector",
                table: "ProfileAttributeValues");

            migrationBuilder.DropIndex(
                name: "IX_Positions_SearchVector",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_Positions_UpdatedAt",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_Likes_RecruiterProfileId",
                table: "Likes");

            migrationBuilder.DropIndex(
                name: "IX_DiscussionPosts_AuthorProfileId",
                table: "DiscussionPosts");

            migrationBuilder.DropIndex(
                name: "IX_DiscussionPosts_PositionId_CreatedAt",
                table: "DiscussionPosts");

            migrationBuilder.DropIndex(
                name: "IX_Cvs_CreatedAt",
                table: "Cvs");

            migrationBuilder.DropIndex(
                name: "IX_Cvs_PositionId_Status",
                table: "Cvs");

            migrationBuilder.DropIndex(
                name: "IX_AttributeDefinitions_SystemKey",
                table: "AttributeDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_AccessRules_AttributeDefinitionId",
                table: "AccessRules");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "ProfileAttributeValues");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ProfileAttributeValues");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "ProfileAttributeValues");

            migrationBuilder.DropColumn(
                name: "Company",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "MaxProjects",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "ProjectTags",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PositionAttributes");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "PositionAttributes");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Likes");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Likes");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "DiscussionPosts");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "DiscussionPosts");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Cvs");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Cvs");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AttributeOptions");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "AttributeOptions");

            migrationBuilder.DropColumn(
                name: "SystemKey",
                table: "AttributeDefinitions");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "AttributeDefinitions");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "AttributeCategories");

            migrationBuilder.DropColumn(
                name: "PreferredLanguage",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PreferredTheme",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AccessRules");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "AccessRules");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "UserProfiles",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Projects",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(8000)",
                oldMaxLength: 8000,
                oldNullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Projects",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<string>(
                name: "StringValue",
                table: "ProfileAttributeValues",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(8000)",
                oldMaxLength: 8000,
                oldNullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ProfileAttributeValues",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Positions",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "PositionAttributes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PositionAttributes",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Likes",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "DiscussionPosts",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid[]>(
                name: "IncludedProjectIds",
                table: "Cvs",
                type: "uuid[]",
                nullable: false,
                defaultValue: new Guid[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Cvs",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AttributeOptions",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AttributeDefinitions",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AttributeCategories",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AccessRules",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "CvAttributeValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CvId = table.Column<Guid>(type: "uuid", nullable: false),
                    BoolValue = table.Column<bool>(type: "boolean", nullable: true),
                    DateValue = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    NumericValue = table.Column<decimal>(type: "numeric", nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false),
                    SelectedOptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    StringValue = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvAttributeValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CvAttributeValues_AttributeDefinitions_AttributeDefinitionId",
                        column: x => x.AttributeDefinitionId,
                        principalTable: "AttributeDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CvAttributeValues_Cvs_CvId",
                        column: x => x.CvId,
                        principalTable: "Cvs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionPosts_PositionId",
                table: "DiscussionPosts",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Cvs_PositionId",
                table: "Cvs",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_CvAttributeValues_AttributeDefinitionId",
                table: "CvAttributeValues",
                column: "AttributeDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CvAttributeValues_CvId_AttributeDefinitionId",
                table: "CvAttributeValues",
                columns: new[] { "CvId", "AttributeDefinitionId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Cvs_Positions_PositionId",
                table: "Cvs",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionAttributes_AttributeDefinitions_AttributeDefinition~",
                table: "PositionAttributes",
                column: "AttributeDefinitionId",
                principalTable: "AttributeDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileAttributeValues_AttributeDefinitions_AttributeDefini~",
                table: "ProfileAttributeValues",
                column: "AttributeDefinitionId",
                principalTable: "AttributeDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
