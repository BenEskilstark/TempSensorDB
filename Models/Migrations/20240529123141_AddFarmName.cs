using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TempSensorDB.Models.Migrations
{
    /// <inheritdoc />
    public partial class AddFarmName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Farm",
                table: "Locations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Farm",
                table: "Locations");
        }
    }
}
