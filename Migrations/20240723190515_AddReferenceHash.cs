using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Wallet_System.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferenceHash",
                table: "DepositTransactions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferenceHash",
                table: "DepositTransactions");
        }
    }
}
