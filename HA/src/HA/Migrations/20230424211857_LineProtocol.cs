using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HA.Migrations
{
    /// <inheritdoc />
    public partial class LineProtocol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_RedisStreamItems",
                table: "RedisStreamItems");

            migrationBuilder.RenameTable(
                name: "RedisStreamItems",
                newName: "MeasurementEntities");

            migrationBuilder.RenameColumn(
                name: "MeasurementJson",
                table: "MeasurementEntities",
                newName: "LineProtocol");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MeasurementEntities",
                table: "MeasurementEntities",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MeasurementEntities",
                table: "MeasurementEntities");

            migrationBuilder.RenameTable(
                name: "MeasurementEntities",
                newName: "RedisStreamItems");

            migrationBuilder.RenameColumn(
                name: "LineProtocol",
                table: "RedisStreamItems",
                newName: "MeasurementJson");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RedisStreamItems",
                table: "RedisStreamItems",
                column: "Id");
        }
    }
}