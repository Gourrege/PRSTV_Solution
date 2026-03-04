using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRSTV_ConsoleApp.Migrations
{
    /// <inheritdoc />
    public partial class AddElection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ElectionId",
                table: "CandidateCountStates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ElectionId",
                table: "BallotCurrentStates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Elections",
                columns: table => new
                {
                    ElectionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Seats = table.Column<int>(type: "int", nullable: false),
                    Quota = table.Column<int>(type: "int", nullable: false),
                    TotalValidPoll = table.Column<int>(type: "int", nullable: false),
                    CurrentCount = table.Column<int>(type: "int", nullable: false),
                    SeatsFilled = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Elections", x => x.ElectionId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCountStates_ElectionId",
                table: "CandidateCountStates",
                column: "ElectionId");

            migrationBuilder.CreateIndex(
                name: "IX_BallotCurrentStates_ElectionId",
                table: "BallotCurrentStates",
                column: "ElectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Elections_CurrentCount",
                table: "Elections",
                column: "CurrentCount");

            migrationBuilder.AddForeignKey(
                name: "FK_BallotCurrentStates_Elections_ElectionId",
                table: "BallotCurrentStates",
                column: "ElectionId",
                principalTable: "Elections",
                principalColumn: "ElectionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateCountStates_Elections_ElectionId",
                table: "CandidateCountStates",
                column: "ElectionId",
                principalTable: "Elections",
                principalColumn: "ElectionId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BallotCurrentStates_Elections_ElectionId",
                table: "BallotCurrentStates");

            migrationBuilder.DropForeignKey(
                name: "FK_CandidateCountStates_Elections_ElectionId",
                table: "CandidateCountStates");

            migrationBuilder.DropTable(
                name: "Elections");

            migrationBuilder.DropIndex(
                name: "IX_CandidateCountStates_ElectionId",
                table: "CandidateCountStates");

            migrationBuilder.DropIndex(
                name: "IX_BallotCurrentStates_ElectionId",
                table: "BallotCurrentStates");

            migrationBuilder.DropColumn(
                name: "ElectionId",
                table: "CandidateCountStates");

            migrationBuilder.DropColumn(
                name: "ElectionId",
                table: "BallotCurrentStates");
        }
    }
}
