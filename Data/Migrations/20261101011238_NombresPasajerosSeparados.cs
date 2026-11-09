using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vuelos_Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class NombresPasajerosSeparados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "nombre_pasajero",
                schema: "dbo",
                table: "detalle_reservas",
                newName: "primer_nombre");

            migrationBuilder.RenameColumn(
                name: "apellido_pasajero",
                schema: "dbo",
                table: "detalle_reservas",
                newName: "primer_apellido");

            migrationBuilder.AddColumn<string>(
                name: "segundo_apellido",
                schema: "dbo",
                table: "detalle_reservas",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "segundo_nombre",
                schema: "dbo",
                table: "detalle_reservas",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No descartar segundos nombres/apellidos escritos después de aplicar Up.
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM [dbo].[detalle_reservas]
                    WHERE [segundo_nombre] IS NOT NULL OR [segundo_apellido] IS NOT NULL
                )
                    THROW 51000, 'No se puede revertir: existen segundos nombres o apellidos que deben conservarse.', 1;
                """);

            migrationBuilder.DropColumn(
                name: "segundo_apellido",
                schema: "dbo",
                table: "detalle_reservas");

            migrationBuilder.DropColumn(
                name: "segundo_nombre",
                schema: "dbo",
                table: "detalle_reservas");

            migrationBuilder.RenameColumn(
                name: "primer_nombre",
                schema: "dbo",
                table: "detalle_reservas",
                newName: "nombre_pasajero");

            migrationBuilder.RenameColumn(
                name: "primer_apellido",
                schema: "dbo",
                table: "detalle_reservas",
                newName: "apellido_pasajero");
        }
    }
}
