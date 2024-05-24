using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TempSensorDB.Models.Migrations
{
    /// <inheritdoc />
    public partial class InitialModelCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    LocationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MinTempF = table.Column<double>(type: "float", nullable: true),
                    MaxTempF = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.LocationID);
                });

            migrationBuilder.CreateTable(
                name: "Sensors",
                columns: table => new
                {
                    SensorID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocationID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sensors", x => x.SensorID);
                    table.ForeignKey(
                        name: "FK_Sensors_Locations_LocationID",
                        column: x => x.LocationID,
                        principalTable: "Locations",
                        principalColumn: "LocationID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TempReadings",
                columns: table => new
                {
                    TempReadingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TempF = table.Column<double>(type: "float", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SensorID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TempReadings", x => x.TempReadingID);
                    table.ForeignKey(
                        name: "FK_TempReadings_Sensors_SensorID",
                        column: x => x.SensorID,
                        principalTable: "Sensors",
                        principalColumn: "SensorID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TempSummary",
                columns: table => new
                {
                    TempSummaryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TempF = table.Column<double>(type: "float", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TimeCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SensorID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TempSummary", x => x.TempSummaryID);
                    table.ForeignKey(
                        name: "FK_TempSummary_Sensors_SensorID",
                        column: x => x.SensorID,
                        principalTable: "Sensors",
                        principalColumn: "SensorID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_LocationID",
                table: "Sensors",
                column: "LocationID");

            migrationBuilder.CreateIndex(
                name: "IX_TempReadings_SensorID",
                table: "TempReadings",
                column: "SensorID");

            migrationBuilder.CreateIndex(
                name: "IX_TempSummary_SensorID",
                table: "TempSummary",
                column: "SensorID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TempReadings");

            migrationBuilder.DropTable(
                name: "TempSummary");

            migrationBuilder.DropTable(
                name: "Sensors");

            migrationBuilder.DropTable(
                name: "Locations");
        }
    }
}
