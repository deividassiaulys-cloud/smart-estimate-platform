using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartEstimatePlatform.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Materials",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "EstimateMaterialLines",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "EstimateMaterialLines");
        }
    }
}
