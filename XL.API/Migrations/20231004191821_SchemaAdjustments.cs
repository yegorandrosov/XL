using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XL.API.Migrations
{
    /// <inheritdoc />
    public partial class SchemaAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Level",
                table: "SheetCells");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "SheetCells");

            migrationBuilder.AddColumn<decimal>(
                name: "NumericValue",
                table: "SheetCells",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumericValue",
                table: "SheetCells");

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "SheetCells",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "SheetCells",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
