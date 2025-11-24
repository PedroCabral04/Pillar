using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace erp.Migrations
{
    /// <inheritdoc />
    public partial class StandardizeTableNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SaleItems_Products_product_id",
                table: "SaleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleItems_Sales_sale_id",
                table: "SaleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_AspNetUsers_user_id",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Customers_customer_id",
                table: "Sales");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Sales",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "Sales",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Sales",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "Sales",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Sales",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "total_amount",
                table: "Sales",
                newName: "TotalAmount");

            migrationBuilder.RenameColumn(
                name: "sale_number",
                table: "Sales",
                newName: "SaleNumber");

            migrationBuilder.RenameColumn(
                name: "sale_date",
                table: "Sales",
                newName: "SaleDate");

            migrationBuilder.RenameColumn(
                name: "payment_method",
                table: "Sales",
                newName: "PaymentMethod");

            migrationBuilder.RenameColumn(
                name: "net_amount",
                table: "Sales",
                newName: "NetAmount");

            migrationBuilder.RenameColumn(
                name: "discount_amount",
                table: "Sales",
                newName: "DiscountAmount");

            migrationBuilder.RenameColumn(
                name: "customer_id",
                table: "Sales",
                newName: "CustomerId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Sales",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Sales_status",
                table: "Sales",
                newName: "IX_Sales_Status");

            migrationBuilder.RenameIndex(
                name: "IX_Sales_user_id",
                table: "Sales",
                newName: "IX_Sales_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Sales_sale_number",
                table: "Sales",
                newName: "IX_Sales_SaleNumber");

            migrationBuilder.RenameIndex(
                name: "IX_Sales_sale_date",
                table: "Sales",
                newName: "IX_Sales_SaleDate");

            migrationBuilder.RenameIndex(
                name: "IX_Sales_customer_id",
                table: "Sales",
                newName: "IX_Sales_CustomerId");

            migrationBuilder.RenameColumn(
                name: "total",
                table: "SaleItems",
                newName: "Total");

            migrationBuilder.RenameColumn(
                name: "quantity",
                table: "SaleItems",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "discount",
                table: "SaleItems",
                newName: "Discount");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "SaleItems",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "unit_price",
                table: "SaleItems",
                newName: "UnitPrice");

            migrationBuilder.RenameColumn(
                name: "sale_id",
                table: "SaleItems",
                newName: "SaleId");

            migrationBuilder.RenameColumn(
                name: "product_id",
                table: "SaleItems",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_SaleItems_sale_id_product_id",
                table: "SaleItems",
                newName: "IX_SaleItems_SaleId_ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_SaleItems_product_id",
                table: "SaleItems",
                newName: "IX_SaleItems_ProductId");

            migrationBuilder.RenameColumn(
                name: "state",
                table: "Customers",
                newName: "State");

            migrationBuilder.RenameColumn(
                name: "phone",
                table: "Customers",
                newName: "Phone");

            migrationBuilder.RenameColumn(
                name: "number",
                table: "Customers",
                newName: "Number");

            migrationBuilder.RenameColumn(
                name: "neighborhood",
                table: "Customers",
                newName: "Neighborhood");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Customers",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "mobile",
                table: "Customers",
                newName: "Mobile");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Customers",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "document",
                table: "Customers",
                newName: "Document");

            migrationBuilder.RenameColumn(
                name: "complement",
                table: "Customers",
                newName: "Complement");

            migrationBuilder.RenameColumn(
                name: "city",
                table: "Customers",
                newName: "City");

            migrationBuilder.RenameColumn(
                name: "address",
                table: "Customers",
                newName: "Address");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Customers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "zip_code",
                table: "Customers",
                newName: "ZipCode");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Customers",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "Customers",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Customers",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_name",
                table: "Customers",
                newName: "IX_Customers_Name");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_email",
                table: "Customers",
                newName: "IX_Customers_Email");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_document",
                table: "Customers",
                newName: "IX_Customers_Document");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleItems_Products_ProductId",
                table: "SaleItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleItems_Sales_SaleId",
                table: "SaleItems",
                column: "SaleId",
                principalTable: "Sales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_AspNetUsers_UserId",
                table: "Sales",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Customers_CustomerId",
                table: "Sales",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SaleItems_Products_ProductId",
                table: "SaleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleItems_Sales_SaleId",
                table: "SaleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_AspNetUsers_UserId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Customers_CustomerId",
                table: "Sales");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Sales",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Sales",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Sales",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Sales",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Sales",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "Sales",
                newName: "total_amount");

            migrationBuilder.RenameColumn(
                name: "SaleNumber",
                table: "Sales",
                newName: "sale_number");

            migrationBuilder.RenameColumn(
                name: "SaleDate",
                table: "Sales",
                newName: "sale_date");

            migrationBuilder.RenameColumn(
                name: "PaymentMethod",
                table: "Sales",
                newName: "payment_method");

            migrationBuilder.RenameColumn(
                name: "NetAmount",
                table: "Sales",
                newName: "net_amount");

            migrationBuilder.RenameColumn(
                name: "DiscountAmount",
                table: "Sales",
                newName: "discount_amount");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "Sales",
                newName: "customer_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Sales",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Sales_Status",
                table: "Sales",
                newName: "IX_Sales_status");

            migrationBuilder.RenameIndex(
                name: "IX_Sales_UserId",
                table: "Sales",
                newName: "IX_Sales_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_Sales_SaleNumber",
                table: "Sales",
                newName: "IX_Sales_sale_number");

            migrationBuilder.RenameIndex(
                name: "IX_Sales_SaleDate",
                table: "Sales",
                newName: "IX_Sales_sale_date");

            migrationBuilder.RenameIndex(
                name: "IX_Sales_CustomerId",
                table: "Sales",
                newName: "IX_Sales_customer_id");

            migrationBuilder.RenameColumn(
                name: "Total",
                table: "SaleItems",
                newName: "total");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "SaleItems",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "Discount",
                table: "SaleItems",
                newName: "discount");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "SaleItems",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "SaleItems",
                newName: "unit_price");

            migrationBuilder.RenameColumn(
                name: "SaleId",
                table: "SaleItems",
                newName: "sale_id");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "SaleItems",
                newName: "product_id");

            migrationBuilder.RenameIndex(
                name: "IX_SaleItems_SaleId_ProductId",
                table: "SaleItems",
                newName: "IX_SaleItems_sale_id_product_id");

            migrationBuilder.RenameIndex(
                name: "IX_SaleItems_ProductId",
                table: "SaleItems",
                newName: "IX_SaleItems_product_id");

            migrationBuilder.RenameColumn(
                name: "State",
                table: "Customers",
                newName: "state");

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "Customers",
                newName: "phone");

            migrationBuilder.RenameColumn(
                name: "Number",
                table: "Customers",
                newName: "number");

            migrationBuilder.RenameColumn(
                name: "Neighborhood",
                table: "Customers",
                newName: "neighborhood");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Customers",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Mobile",
                table: "Customers",
                newName: "mobile");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "Customers",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Document",
                table: "Customers",
                newName: "document");

            migrationBuilder.RenameColumn(
                name: "Complement",
                table: "Customers",
                newName: "complement");

            migrationBuilder.RenameColumn(
                name: "City",
                table: "Customers",
                newName: "city");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Customers",
                newName: "address");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Customers",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ZipCode",
                table: "Customers",
                newName: "zip_code");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Customers",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Customers",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Customers",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_Name",
                table: "Customers",
                newName: "IX_Customers_name");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_Email",
                table: "Customers",
                newName: "IX_Customers_email");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_Document",
                table: "Customers",
                newName: "IX_Customers_document");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleItems_Products_product_id",
                table: "SaleItems",
                column: "product_id",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleItems_Sales_sale_id",
                table: "SaleItems",
                column: "sale_id",
                principalTable: "Sales",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_AspNetUsers_user_id",
                table: "Sales",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Customers_customer_id",
                table: "Sales",
                column: "customer_id",
                principalTable: "Customers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
