using Microsoft.EntityFrameworkCore.Migrations;

namespace CvPlatform.Infrastructure.Migrations;

public sealed partial class AddPositionExportTokens : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PositionExportTokens",
            columns: table => new
            {
                PositionId = table.Column<Guid>(type: "uuid", nullable: false),
                TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PositionExportTokens", x => x.PositionId);
                table.ForeignKey("FK_PositionExportTokens_Positions_PositionId", x => x.PositionId, "Positions", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex("IX_PositionExportTokens_TokenHash", "PositionExportTokens", "TokenHash", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("PositionExportTokens");
    }
}
