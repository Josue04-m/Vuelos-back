using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vuelos_Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class DesgloseIvaReservas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "monto_iva",
                schema: "dbo",
                table: "reservas",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "porcentaje_iva",
                schema: "dbo",
                table: "reservas",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "subtotal",
                schema: "dbo",
                table: "reservas",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_reservas_desglose_iva",
                schema: "dbo",
                table: "reservas",
                sql: "([subtotal] IS NULL AND [porcentaje_iva] IS NULL AND [monto_iva] IS NULL) OR ([subtotal] IS NOT NULL AND [porcentaje_iva] IS NOT NULL AND [monto_iva] IS NOT NULL AND [subtotal] >= 0 AND [porcentaje_iva] BETWEEN 0 AND 100 AND [monto_iva] >= 0 AND [monto_iva] = ROUND([subtotal] * [porcentaje_iva] / 100.0, 2) AND [monto_total] = [subtotal] + [monto_iva])");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [dbo].[reservas] WHERE [subtotal] IS NOT NULL OR [porcentaje_iva] IS NOT NULL OR [monto_iva] IS NOT NULL) THROW 51000, 'No se permite perder el desglose IVA de las reservas. Prepare una migracion correctiva que conserve los importes.', 1;");
            migrationBuilder.DropCheckConstraint(
                name: "CK_reservas_desglose_iva",
                schema: "dbo",
                table: "reservas");

            migrationBuilder.DropColumn(
                name: "monto_iva",
                schema: "dbo",
                table: "reservas");

            migrationBuilder.DropColumn(
                name: "porcentaje_iva",
                schema: "dbo",
                table: "reservas");

            migrationBuilder.DropColumn(
                name: "subtotal",
                schema: "dbo",
                table: "reservas");
        }
    }
}
