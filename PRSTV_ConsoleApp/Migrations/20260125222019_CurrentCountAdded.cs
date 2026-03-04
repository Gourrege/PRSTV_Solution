using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRSTV_ConsoleApp.Migrations
{
    /// <inheritdoc />
    public partial class CurrentCountAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CountNumber",
                table: "CandidateCountStates",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountNumber",
                table: "CandidateCountStates");
        }
    }
}
