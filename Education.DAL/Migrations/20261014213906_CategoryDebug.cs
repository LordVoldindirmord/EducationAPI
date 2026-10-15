using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Education.DAL.Migrations
{
    /// <inheritdoc />
    public partial class CategoryDebug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Courses_CategoryId",
                table: "Courses");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CategoryId_Title",
                table: "Courses",
                columns: new[] { "CategoryId", "Title" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Courses_CategoryId_Title",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CategoryId",
                table: "Courses",
                column: "CategoryId");
        }
    }
}
