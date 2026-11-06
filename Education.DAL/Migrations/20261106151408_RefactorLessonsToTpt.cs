using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Education.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RefactorLessonsToTpt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuizLessons_Lessons_LessonId",
                table: "QuizLessons");

            migrationBuilder.DropForeignKey(
                name: "FK_TestQuestions_QuizLessons_QuizLessonId",
                table: "TestQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_TextLessons_Lessons_LessonId",
                table: "TextLessons");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoLessons_Lessons_LessonId",
                table: "VideoLessons");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VideoLessons",
                table: "VideoLessons");

            migrationBuilder.DropIndex(
                name: "IX_VideoLessons_LessonId",
                table: "VideoLessons");

            migrationBuilder.DropCheckConstraint(
                name: "CK_VideoLessons_DurationSeconds",
                table: "VideoLessons");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TextLessons",
                table: "TextLessons");

            migrationBuilder.DropIndex(
                name: "IX_TextLessons_LessonId",
                table: "TextLessons");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QuizLessons",
                table: "QuizLessons");

            migrationBuilder.DropIndex(
                name: "IX_QuizLessons_LessonId",
                table: "QuizLessons");

            migrationBuilder.DropCheckConstraint(
                name: "CK_QuizLessons_PassingScore",
                table: "QuizLessons");

            migrationBuilder.DropCheckConstraint(
                name: "CK_QuizLessons_TimeLimitMinutes",
                table: "QuizLessons");

            migrationBuilder.DropColumn(
                name: "LessonId",
                table: "VideoLessons");

            migrationBuilder.DropColumn(
                name: "LessonId",
                table: "TextLessons");

            migrationBuilder.DropColumn(
                name: "LessonId",
                table: "QuizLessons");

            migrationBuilder.RenameTable(
                name: "VideoLessons",
                newName: "VideoLesson");

            migrationBuilder.RenameTable(
                name: "TextLessons",
                newName: "TextLesson");

            migrationBuilder.RenameTable(
                name: "QuizLessons",
                newName: "QuizLesson");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VideoLesson",
                table: "VideoLesson",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TextLesson",
                table: "TextLesson",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QuizLesson",
                table: "QuizLesson",
                column: "Id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_VideoLessons_DurationSeconds",
                table: "VideoLesson",
                sql: "\"DurationSeconds\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_QuizLessons_PassingScore",
                table: "QuizLesson",
                sql: "\"PassingScore\" >= 0 AND \"PassingScore\" <= 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_QuizLessons_TimeLimitMinutes",
                table: "QuizLesson",
                sql: "\"TimeLimitMinutes\" IS NULL OR \"TimeLimitMinutes\" > 0");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizLesson_Lessons_Id",
                table: "QuizLesson",
                column: "Id",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestQuestions_QuizLesson_QuizLessonId",
                table: "TestQuestions",
                column: "QuizLessonId",
                principalTable: "QuizLesson",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TextLesson_Lessons_Id",
                table: "TextLesson",
                column: "Id",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VideoLesson_Lessons_Id",
                table: "VideoLesson",
                column: "Id",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuizLesson_Lessons_Id",
                table: "QuizLesson");

            migrationBuilder.DropForeignKey(
                name: "FK_TestQuestions_QuizLesson_QuizLessonId",
                table: "TestQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_TextLesson_Lessons_Id",
                table: "TextLesson");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoLesson_Lessons_Id",
                table: "VideoLesson");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VideoLesson",
                table: "VideoLesson");

            migrationBuilder.DropCheckConstraint(
                name: "CK_VideoLessons_DurationSeconds",
                table: "VideoLesson");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TextLesson",
                table: "TextLesson");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QuizLesson",
                table: "QuizLesson");

            migrationBuilder.DropCheckConstraint(
                name: "CK_QuizLessons_PassingScore",
                table: "QuizLesson");

            migrationBuilder.DropCheckConstraint(
                name: "CK_QuizLessons_TimeLimitMinutes",
                table: "QuizLesson");

            migrationBuilder.RenameTable(
                name: "VideoLesson",
                newName: "VideoLessons");

            migrationBuilder.RenameTable(
                name: "TextLesson",
                newName: "TextLessons");

            migrationBuilder.RenameTable(
                name: "QuizLesson",
                newName: "QuizLessons");

            migrationBuilder.AddColumn<Guid>(
                name: "LessonId",
                table: "VideoLessons",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "LessonId",
                table: "TextLessons",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "LessonId",
                table: "QuizLessons",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_VideoLessons",
                table: "VideoLessons",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TextLessons",
                table: "TextLessons",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QuizLessons",
                table: "QuizLessons",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_VideoLessons_LessonId",
                table: "VideoLessons",
                column: "LessonId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_VideoLessons_DurationSeconds",
                table: "VideoLessons",
                sql: "\"DurationSeconds\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_TextLessons_LessonId",
                table: "TextLessons",
                column: "LessonId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuizLessons_LessonId",
                table: "QuizLessons",
                column: "LessonId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_QuizLessons_PassingScore",
                table: "QuizLessons",
                sql: "\"PassingScore\" >= 0 AND \"PassingScore\" <= 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_QuizLessons_TimeLimitMinutes",
                table: "QuizLessons",
                sql: "\"TimeLimitMinutes\" IS NULL OR \"TimeLimitMinutes\" > 0");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizLessons_Lessons_LessonId",
                table: "QuizLessons",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestQuestions_QuizLessons_QuizLessonId",
                table: "TestQuestions",
                column: "QuizLessonId",
                principalTable: "QuizLessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TextLessons_Lessons_LessonId",
                table: "TextLessons",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VideoLessons_Lessons_LessonId",
                table: "VideoLessons",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
