using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication_ClothingEcommerce.Migrations
{
    /// <inheritdoc />
    public partial class AddCompareAtPriceToProductVariant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CompareAtPrice",
                table: "ProductVariants",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompareAtPrice",
                table: "ProductVariants");
        }
    }
}
