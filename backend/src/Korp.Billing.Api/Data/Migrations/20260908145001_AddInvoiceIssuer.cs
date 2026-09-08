using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Korp.Billing.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceIssuer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IssuedByEmployeeId",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuedByName",
                table: "invoices",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_IssuedByEmployeeId",
                table: "invoices",
                column: "IssuedByEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_employees_IssuedByEmployeeId",
                table: "invoices",
                column: "IssuedByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invoices_employees_IssuedByEmployeeId",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "IX_invoices_IssuedByEmployeeId",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "IssuedByEmployeeId",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "IssuedByName",
                table: "invoices");
        }
    }
}
