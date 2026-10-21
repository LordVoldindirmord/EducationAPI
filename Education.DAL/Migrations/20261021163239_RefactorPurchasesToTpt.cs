using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Education.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPurchasesToTpt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CoursePurchases_AspNetUsers_UserId",
                table: "CoursePurchases");

            migrationBuilder.DropForeignKey(
                name: "FK_CoursePurchases_Courses_CourseId",
                table: "CoursePurchases");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePurchases_AspNetRoles_RoleId",
                table: "RolePurchases");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePurchases_AspNetUsers_UserId",
                table: "RolePurchases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RolePurchases",
                table: "RolePurchases");

            migrationBuilder.DropIndex(
                name: "IX_RolePurchases_UserId",
                table: "RolePurchases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CoursePurchases",
                table: "CoursePurchases");

            migrationBuilder.DropIndex(
                name: "IX_CoursePurchases_UserId_CourseId",
                table: "CoursePurchases");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "RolePurchases");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "RolePurchases");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "RolePurchases");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "RolePurchases");

            migrationBuilder.DropColumn(
                name: "PricePaid",
                table: "RolePurchases");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "RolePurchases");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "RolePurchases");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "RolePurchases");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "CoursePurchases");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CoursePurchases");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "CoursePurchases");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "CoursePurchases");

            migrationBuilder.DropColumn(
                name: "PricePaid",
                table: "CoursePurchases");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CoursePurchases");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "CoursePurchases");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "CoursePurchases");

            migrationBuilder.RenameTable(
                name: "RolePurchases",
                newName: "RolePurchase");

            migrationBuilder.RenameTable(
                name: "CoursePurchases",
                newName: "CoursePurchase");

            migrationBuilder.RenameIndex(
                name: "IX_RolePurchases_RoleId",
                table: "RolePurchase",
                newName: "IX_RolePurchase_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_CoursePurchases_CourseId",
                table: "CoursePurchase",
                newName: "IX_CoursePurchase_CourseId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RolePurchase",
                table: "RolePurchase",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CoursePurchase",
                table: "CoursePurchase",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Purchase",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PricePaid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TransactionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PaymentMethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PurchaseType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purchase", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Purchase_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Purchase_UserId",
                table: "Purchase",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CoursePurchase_Courses_CourseId",
                table: "CoursePurchase",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CoursePurchase_Purchase_Id",
                table: "CoursePurchase",
                column: "Id",
                principalTable: "Purchase",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePurchase_AspNetRoles_RoleId",
                table: "RolePurchase",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePurchase_Purchase_Id",
                table: "RolePurchase",
                column: "Id",
                principalTable: "Purchase",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CoursePurchase_Courses_CourseId",
                table: "CoursePurchase");

            migrationBuilder.DropForeignKey(
                name: "FK_CoursePurchase_Purchase_Id",
                table: "CoursePurchase");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePurchase_AspNetRoles_RoleId",
                table: "RolePurchase");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePurchase_Purchase_Id",
                table: "RolePurchase");

            migrationBuilder.DropTable(
                name: "Purchase");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RolePurchase",
                table: "RolePurchase");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CoursePurchase",
                table: "CoursePurchase");

            migrationBuilder.RenameTable(
                name: "RolePurchase",
                newName: "RolePurchases");

            migrationBuilder.RenameTable(
                name: "CoursePurchase",
                newName: "CoursePurchases");

            migrationBuilder.RenameIndex(
                name: "IX_RolePurchase_RoleId",
                table: "RolePurchases",
                newName: "IX_RolePurchases_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_CoursePurchase_CourseId",
                table: "CoursePurchases",
                newName: "IX_CoursePurchases_CourseId");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "RolePurchases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "RolePurchases",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "RolePurchases",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "RolePurchases",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePaid",
                table: "RolePurchases",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "RolePurchases",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TransactionId",
                table: "RolePurchases",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "RolePurchases",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "CoursePurchases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "CoursePurchases",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "CoursePurchases",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "CoursePurchases",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePaid",
                table: "CoursePurchases",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "CoursePurchases",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TransactionId",
                table: "CoursePurchases",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "CoursePurchases",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_RolePurchases",
                table: "RolePurchases",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CoursePurchases",
                table: "CoursePurchases",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_RolePurchases_UserId",
                table: "RolePurchases",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CoursePurchases_UserId_CourseId",
                table: "CoursePurchases",
                columns: new[] { "UserId", "CourseId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CoursePurchases_AspNetUsers_UserId",
                table: "CoursePurchases",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CoursePurchases_Courses_CourseId",
                table: "CoursePurchases",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePurchases_AspNetRoles_RoleId",
                table: "RolePurchases",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePurchases_AspNetUsers_UserId",
                table: "RolePurchases",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
