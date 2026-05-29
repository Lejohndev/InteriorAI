using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteriorAI.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomStylePrompts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoomStylePrompts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StyleId = table.Column<int>(type: "int", nullable: false),
                    RoomTypeKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RoomTypeName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Variant = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Lighting = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Material = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Furniture = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Atmosphere = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomStylePrompts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomStylePrompts_StyleAesthetics_StyleId",
                        column: x => x.StyleId,
                        principalTable: "StyleAesthetics",
                        principalColumn: "StyleID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoomStylePrompts_RoomTypeKey",
                table: "RoomStylePrompts",
                column: "RoomTypeKey");

            migrationBuilder.CreateIndex(
                name: "IX_RoomStylePrompts_StyleId_RoomTypeKey",
                table: "RoomStylePrompts",
                columns: new[] { "StyleId", "RoomTypeKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoomStylePrompts");
        }
    }
}
