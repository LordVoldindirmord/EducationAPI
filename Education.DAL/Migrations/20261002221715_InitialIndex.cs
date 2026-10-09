using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Education.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProgresses_UserId",
                table: "UserProgresses");

            migrationBuilder.DropIndex(
                name: "IX_TestQuestions_QuizLessonId",
                table: "TestQuestions");

            migrationBuilder.DropIndex(
                name: "IX_Modules_CourseId",
                table: "Modules");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_ModuleId",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_CoursePurchases_UserId",
                table: "CoursePurchases");

            migrationBuilder.AddCheckConstraint(
                name: "CK_VideoLessons_DurationSeconds",
                table: "VideoLessons",
                sql: "\"DurationSeconds\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_UserProgresses_UserId_LessonId",
                table: "UserProgresses",
                columns: new[] { "UserId", "LessonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestQuestions_QuizLessonId_OrderNumber",
                table: "TestQuestions",
                columns: new[] { "QuizLessonId", "OrderNumber" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_QuizLessons_PassingScore",
                table: "QuizLessons",
                sql: "\"PassingScore\" >= 0 AND \"PassingScore\" <= 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_QuizLessons_TimeLimitMinutes",
                table: "QuizLessons",
                sql: "\"TimeLimitMinutes\" IS NULL OR \"TimeLimitMinutes\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_CourseId_OrderNumber",
                table: "Modules",
                columns: new[] { "CourseId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_ModuleId_OrderNumber",
                table: "Lessons",
                columns: new[] { "ModuleId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoursePurchases_UserId_CourseId",
                table: "CoursePurchases",
                columns: new[] { "UserId", "CourseId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_VideoLessons_DurationSeconds",
                table: "VideoLessons");

            migrationBuilder.DropIndex(
                name: "IX_UserProgresses_UserId_LessonId",
                table: "UserProgresses");

            migrationBuilder.DropIndex(
                name: "IX_TestQuestions_QuizLessonId_OrderNumber",
                table: "TestQuestions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_QuizLessons_PassingScore",
                table: "QuizLessons");

            migrationBuilder.DropCheckConstraint(
                name: "CK_QuizLessons_TimeLimitMinutes",
                table: "QuizLessons");

            migrationBuilder.DropIndex(
                name: "IX_Modules_CourseId_OrderNumber",
                table: "Modules");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_ModuleId_OrderNumber",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_CoursePurchases_UserId_CourseId",
                table: "CoursePurchases");

            migrationBuilder.CreateIndex(
                name: "IX_UserProgresses_UserId",
                table: "UserProgresses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TestQuestions_QuizLessonId",
                table: "TestQuestions",
                column: "QuizLessonId");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_CourseId",
                table: "Modules",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_ModuleId",
                table: "Lessons",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_CoursePurchases_UserId",
                table: "CoursePurchases",
                column: "UserId");
        }
    }
}
