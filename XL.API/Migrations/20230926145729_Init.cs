using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XL.API.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SheetCells",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SheetId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CellId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Expression = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SheetCells", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SheetCellReference",
                columns: table => new
                {
                    ParentId = table.Column<int>(type: "int", nullable: false),
                    ChildId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SheetCellReference", x => new { x.ParentId, x.ChildId });
                    table.ForeignKey(
                        name: "FK_SheetCellReference_SheetCells_ChildId",
                        column: x => x.ChildId,
                        principalTable: "SheetCells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_SheetCellReference_SheetCells_ParentId",
                        column: x => x.ParentId,
                        principalTable: "SheetCells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SheetCellReference_ChildId",
                table: "SheetCellReference",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_SheetCells_SheetId_CellId",
                table: "SheetCells",
                columns: new[] { "SheetId", "CellId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SheetCellReference");

            migrationBuilder.DropTable(
                name: "SheetCells");
        }
    }
}
