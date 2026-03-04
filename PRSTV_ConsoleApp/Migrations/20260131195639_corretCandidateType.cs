using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRSTV_ConsoleApp.Migrations
{
    /// <inheritdoc />
    public partial class corretCandidateType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CandidateId",
                table: "CandidateCountStates",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "CandidateId",
                table: "CandidateCountStates",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
