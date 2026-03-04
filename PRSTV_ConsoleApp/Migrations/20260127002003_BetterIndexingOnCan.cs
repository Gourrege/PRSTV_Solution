using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRSTV_ConsoleApp.Migrations
{
    /// <inheritdoc />
    public partial class BetterIndexingOnCan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CandidateCountStates_CandidateCountStateId",
                table: "CandidateCountStates");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCountStates_CountNumber_CandidateId",
                table: "CandidateCountStates",
                columns: new[] { "CountNumber", "CandidateId" });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCountStates_CountNumber_Status",
                table: "CandidateCountStates",
                columns: new[] { "CountNumber", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CandidateCountStates_CountNumber_CandidateId",
                table: "CandidateCountStates");

            migrationBuilder.DropIndex(
                name: "IX_CandidateCountStates_CountNumber_Status",
                table: "CandidateCountStates");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCountStates_CandidateCountStateId",
                table: "CandidateCountStates",
                column: "CandidateCountStateId");
        }
    }
}
