using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Education.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPassed",
                table: "UserProgresses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "UserProgresses",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"UserProgresses\" SET \"IsPassed\" = TRUE;");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserProgresses_Score",
                table: "UserProgresses",
                sql: "\"Score\" IS NULL OR (\"Score\" >= 0 AND \"Score\" <= 100)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_UserProgresses_Score",
                table: "UserProgresses");

            migrationBuilder.DropColumn(
                name: "IsPassed",
                table: "UserProgresses");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "UserProgresses");
        }
    }
}
