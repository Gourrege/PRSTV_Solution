using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRSTV_ConsoleApp.Migrations
{
    /// <inheritdoc />
    public partial class BetterIndexing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_BallotCurrentStates_CountNumber_CurrentCandidateId_IsExhausted",
                table: "BallotCurrentStates",
                columns: new[] { "CountNumber", "CurrentCandidateId", "IsExhausted" });

            migrationBuilder.CreateIndex(
                name: "IX_BallotCurrentStates_CountNumber_RandomBallotId",
                table: "BallotCurrentStates",
                columns: new[] { "CountNumber", "RandomBallotId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BallotCurrentStates_CountNumber_CurrentCandidateId_IsExhausted",
                table: "BallotCurrentStates");

            migrationBuilder.DropIndex(
                name: "IX_BallotCurrentStates_CountNumber_RandomBallotId",
                table: "BallotCurrentStates");
        }
    }
}
