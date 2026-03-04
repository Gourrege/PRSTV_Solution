using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRSTV_ConsoleApp.Migrations
{
    /// <inheritdoc />
    public partial class latestCountTransfer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LatestCountTransfered",
                table: "BallotCurrentStates",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LatestCountTransfered",
                table: "BallotCurrentStates");
        }
    }
}
