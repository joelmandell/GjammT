using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GjammT.Models.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenantCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add ClientCustomerId column to Customers table
            migrationBuilder.AddColumn<Guid>(
                name: "ClientCustomerId",
                table: "Customers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Create index on ClientCustomerId for performance
            migrationBuilder.CreateIndex(
                name: "IX_Customers_ClientCustomerId",
                table: "Customers",
                column: "ClientCustomerId");

            // Add foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_Customers_ClientCustomer_ClientCustomerId",
                table: "Customers",
                column: "ClientCustomerId",
                principalTable: "ClientCustomer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_ClientCustomer_ClientCustomerId",
                table: "Customers");

            // Drop index
            migrationBuilder.DropIndex(
                name: "IX_Customers_ClientCustomerId",
                table: "Customers");

            // Remove ClientCustomerId column
            migrationBuilder.DropColumn(
                name: "ClientCustomerId",
                table: "Customers");
        }
    }
}
