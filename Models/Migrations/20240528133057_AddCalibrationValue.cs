using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TempSensorDB.Models.Migrations
{
    /// <inheritdoc />
    public partial class AddCalibrationValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CalibrationValueF",
                table: "Sensors",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalibrationValueF",
                table: "Sensors");
        }
    }
}
