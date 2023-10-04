using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XL.API.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDecimalPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "NumericValue",
                table: "SheetCells",
                type: "decimal(10,2)",
                precision: 10,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "NumericValue",
                table: "SheetCells",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldNullable: true);
        }
    }
}
