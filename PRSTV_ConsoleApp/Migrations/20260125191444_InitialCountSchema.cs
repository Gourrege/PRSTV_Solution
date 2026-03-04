using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRSTV_ConsoleApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCountSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BallotCurrentStates",
                columns: table => new
                {
                    BallotCurrentStateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountNumber = table.Column<int>(type: "int", nullable: false),
                    RandomBallotId = table.Column<long>(type: "bigint", nullable: false),
                    CurrentCandidateId = table.Column<int>(type: "int", nullable: true),
                    CurrentPreference = table.Column<int>(type: "int", nullable: false),
                    IsExhausted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BallotCurrentStates", x => x.BallotCurrentStateId);
                });

            migrationBuilder.CreateTable(
                name: "CandidateCountStates",
                columns: table => new
                {
                    CandidateCountStateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Surplus = table.Column<int>(type: "int", nullable: false),
                    TotalVotes = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateCountStates", x => x.CandidateCountStateId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BallotCurrentStates_CountNumber_CurrentCandidateId",
                table: "BallotCurrentStates",
                columns: new[] { "CountNumber", "CurrentCandidateId" });

            migrationBuilder.CreateIndex(
                name: "IX_BallotCurrentStates_RandomBallotId",
                table: "BallotCurrentStates",
                column: "RandomBallotId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCountStates_CandidateCountStateId",
                table: "CandidateCountStates",
                column: "CandidateCountStateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BallotCurrentStates");

            migrationBuilder.DropTable(
                name: "CandidateCountStates");
        }
    }
}
