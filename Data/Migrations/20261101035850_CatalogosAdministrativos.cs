using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vuelos_Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class CatalogosAdministrativos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "activo",
                schema: "dbo",
                table: "categorias_asiento",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_desactivacion",
                schema: "dbo",
                table: "categorias_asiento",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                schema: "dbo",
                table: "categorias_asiento",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<bool>(
                name: "activo",
                schema: "dbo",
                table: "aviones",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_desactivacion",
                schema: "dbo",
                table: "aviones",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                schema: "dbo",
                table: "aviones",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<bool>(
                name: "activo",
                schema: "dbo",
                table: "asientos_fisicos",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_desactivacion",
                schema: "dbo",
                table: "asientos_fisicos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                schema: "dbo",
                table: "asientos_fisicos",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<bool>(
                name: "activo",
                schema: "dbo",
                table: "aeropuertos",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_desactivacion",
                schema: "dbo",
                table: "aeropuertos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                schema: "dbo",
                table: "aeropuertos",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM dbo.aeropuertos WHERE activo = 0 OR fecha_desactivacion IS NOT NULL)
                OR EXISTS (SELECT 1 FROM dbo.aviones WHERE activo = 0 OR fecha_desactivacion IS NOT NULL)
                OR EXISTS (SELECT 1 FROM dbo.categorias_asiento WHERE activo = 0 OR fecha_desactivacion IS NOT NULL)
                OR EXISTS (SELECT 1 FROM dbo.asientos_fisicos WHERE activo = 0 OR fecha_desactivacion IS NOT NULL)
                    THROW 51002, 'No puede retirar las marcas de desactivación sin revisar los datos existentes.', 1;
                """);
            migrationBuilder.DropColumn(
                name: "activo",
                schema: "dbo",
                table: "categorias_asiento");

            migrationBuilder.DropColumn(
                name: "fecha_desactivacion",
                schema: "dbo",
                table: "categorias_asiento");

            migrationBuilder.DropColumn(
                name: "row_version",
                schema: "dbo",
                table: "categorias_asiento");

            migrationBuilder.DropColumn(
                name: "activo",
                schema: "dbo",
                table: "aviones");

            migrationBuilder.DropColumn(
                name: "fecha_desactivacion",
                schema: "dbo",
                table: "aviones");

            migrationBuilder.DropColumn(
                name: "row_version",
                schema: "dbo",
                table: "aviones");

            migrationBuilder.DropColumn(
                name: "activo",
                schema: "dbo",
                table: "asientos_fisicos");

            migrationBuilder.DropColumn(
                name: "fecha_desactivacion",
                schema: "dbo",
                table: "asientos_fisicos");

            migrationBuilder.DropColumn(
                name: "row_version",
                schema: "dbo",
                table: "asientos_fisicos");

            migrationBuilder.DropColumn(
                name: "activo",
                schema: "dbo",
                table: "aeropuertos");

            migrationBuilder.DropColumn(
                name: "fecha_desactivacion",
                schema: "dbo",
                table: "aeropuertos");

            migrationBuilder.DropColumn(
                name: "row_version",
                schema: "dbo",
                table: "aeropuertos");
        }
    }
}
