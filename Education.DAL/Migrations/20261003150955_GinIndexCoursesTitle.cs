using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Education.DAL.Migrations
{
    /// <inheritdoc />
    public partial class GinIndexCoursesTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Индекс для полнотекстового поиска по названию на русском
            migrationBuilder.Sql("CREATE INDEX idx_courses_title ON \"Courses\" USING gin(to_tsvector('russian', \"Title\"));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
