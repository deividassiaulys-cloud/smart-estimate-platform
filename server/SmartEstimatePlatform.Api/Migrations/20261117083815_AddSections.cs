using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartEstimatePlatform.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Coefficient",
                table: "EstimateWorkLines",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SectionId",
                table: "EstimateWorkLines",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CatalogSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_0900_as_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Unit = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogSections_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EstimateSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EstimateId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Unit = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimateSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstimateSections_Estimates_EstimateId",
                        column: x => x.EstimateId,
                        principalTable: "Estimates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CatalogSectionLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CatalogSectionId = table.Column<int>(type: "int", nullable: false),
                    WorkId = table.Column<int>(type: "int", nullable: false),
                    Coefficient = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogSectionLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogSectionLines_CatalogSections_CatalogSectionId",
                        column: x => x.CatalogSectionId,
                        principalTable: "CatalogSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CatalogSectionLines_Works_WorkId",
                        column: x => x.WorkId,
                        principalTable: "Works",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EstimateWorkLines_SectionId",
                table: "EstimateWorkLines",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogSectionLines_CatalogSectionId",
                table: "CatalogSectionLines",
                column: "CatalogSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogSectionLines_WorkId",
                table: "CatalogSectionLines",
                column: "WorkId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogSections_CompanyId_Name_Unit",
                table: "CatalogSections",
                columns: new[] { "CompanyId", "Name", "Unit" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EstimateSections_EstimateId",
                table: "EstimateSections",
                column: "EstimateId");

            migrationBuilder.AddForeignKey(
                name: "FK_EstimateWorkLines_EstimateSections_SectionId",
                table: "EstimateWorkLines",
                column: "SectionId",
                principalTable: "EstimateSections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EstimateWorkLines_EstimateSections_SectionId",
                table: "EstimateWorkLines");

            migrationBuilder.DropTable(
                name: "CatalogSectionLines");

            migrationBuilder.DropTable(
                name: "EstimateSections");

            migrationBuilder.DropTable(
                name: "CatalogSections");

            migrationBuilder.DropIndex(
                name: "IX_EstimateWorkLines_SectionId",
                table: "EstimateWorkLines");

            migrationBuilder.DropColumn(
                name: "Coefficient",
                table: "EstimateWorkLines");

            migrationBuilder.DropColumn(
                name: "SectionId",
                table: "EstimateWorkLines");
        }
    }
}
