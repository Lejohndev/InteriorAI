using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InteriorAI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StyleAesthetics",
                columns: table => new
                {
                    StyleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StyleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CoreAesthetic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BaseStructuralPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LightingOptions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaterialOptions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ColorRuleOptions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AtmosphereOptions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpecificNegative = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TechnicalSpecs = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StyleAesthetics", x => x.StyleID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoomInteriors",
                columns: table => new
                {
                    RoomID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StyleID = table.Column<int>(type: "int", nullable: false),
                    FocalFurnitureOptions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DecorOptions = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomInteriors", x => x.RoomID);
                    table.ForeignKey(
                        name: "FK_RoomInteriors_StyleAesthetics_StyleID",
                        column: x => x.StyleID,
                        principalTable: "StyleAesthetics",
                        principalColumn: "StyleID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoomInteriors_StyleID",
                table: "RoomInteriors",
                column: "StyleID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoomInteriors");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "StyleAesthetics");
        }
    }
}
