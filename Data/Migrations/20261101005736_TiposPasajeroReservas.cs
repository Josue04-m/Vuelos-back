using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vuelos_Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class TiposPasajeroReservas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tipo_pasajero",
                schema: "dbo",
                table: "detalle_reservas",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_detalle_reservas_tipo_pasajero",
                schema: "dbo",
                table: "detalle_reservas",
                sql: "[tipo_pasajero] IS NULL OR [tipo_pasajero] IN ('adulto', 'joven', 'nino', 'bebe')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_detalle_reservas_tipo_pasajero",
                schema: "dbo",
                table: "detalle_reservas");

            migrationBuilder.DropColumn(
                name: "tipo_pasajero",
                schema: "dbo",
                table: "detalle_reservas");
        }
    }
}
