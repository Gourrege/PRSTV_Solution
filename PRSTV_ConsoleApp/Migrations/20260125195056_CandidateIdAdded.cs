using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRSTV_ConsoleApp.Migrations
{
    /// <inheritdoc />
    public partial class CandidateIdAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CandidateId",
                table: "CandidateCountStates",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CandidateId",
                table: "CandidateCountStates");
        }
    }
}
