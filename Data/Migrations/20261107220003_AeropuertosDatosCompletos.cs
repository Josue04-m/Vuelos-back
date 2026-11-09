using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vuelos_Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AeropuertosDatosCompletos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "pais",
                schema: "dbo",
                table: "aeropuertos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "nombre",
                schema: "dbo",
                table: "aeropuertos",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldUnicode: false,
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "ciudad",
                schema: "dbo",
                table: "aeropuertos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "codigo_gps",
                schema: "dbo",
                table: "aeropuertos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "codigo_icao",
                schema: "dbo",
                table: "aeropuertos",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "codigo_local",
                schema: "dbo",
                table: "aeropuertos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "continente",
                schema: "dbo",
                table: "aeropuertos",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "datos_origen_json",
                schema: "dbo",
                table: "aeropuertos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "elevacion_pies",
                schema: "dbo",
                table: "aeropuertos",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "id_origen",
                schema: "dbo",
                table: "aeropuertos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "identificador",
                schema: "dbo",
                table: "aeropuertos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "latitud",
                schema: "dbo",
                table: "aeropuertos",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "longitud",
                schema: "dbo",
                table: "aeropuertos",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "palabras_clave",
                schema: "dbo",
                table: "aeropuertos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "region_iso",
                schema: "dbo",
                table: "aeropuertos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "servicio_regular",
                schema: "dbo",
                table: "aeropuertos",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sitio_web",
                schema: "dbo",
                table: "aeropuertos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tipo",
                schema: "dbo",
                table: "aeropuertos",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "wikipedia",
                schema: "dbo",
                table: "aeropuertos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_aeropuertos_datos_origen_json",
                schema: "dbo",
                table: "aeropuertos",
                sql: "[datos_origen_json] IS NULL OR ISJSON([datos_origen_json]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_aeropuertos_latitud",
                schema: "dbo",
                table: "aeropuertos",
                sql: "[latitud] IS NULL OR [latitud] BETWEEN -90 AND 90");

            migrationBuilder.AddCheckConstraint(
                name: "CK_aeropuertos_longitud",
                schema: "dbo",
                table: "aeropuertos",
                sql: "[longitud] IS NULL OR [longitud] BETWEEN -180 AND 180");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [dbo].[aeropuertos] WHERE [id_origen] IS NOT NULL OR [identificador] IS NOT NULL OR [tipo] IS NOT NULL OR [latitud] IS NOT NULL OR [longitud] IS NOT NULL OR [elevacion_pies] IS NOT NULL OR [continente] IS NOT NULL OR [region_iso] IS NOT NULL OR [servicio_regular] IS NOT NULL OR [codigo_icao] IS NOT NULL OR [codigo_gps] IS NOT NULL OR [codigo_local] IS NOT NULL OR [sitio_web] IS NOT NULL OR [wikipedia] IS NOT NULL OR [palabras_clave] IS NOT NULL OR [datos_origen_json] IS NOT NULL OR CONVERT(varbinary(max), [nombre]) <> CONVERT(varbinary(max), CONVERT(nvarchar(150), CONVERT(varchar(150), [nombre]))) OR CONVERT(varbinary(max), [ciudad]) <> CONVERT(varbinary(max), CONVERT(nvarchar(100), CONVERT(varchar(100), [ciudad]))) OR CONVERT(varbinary(max), [pais]) <> CONVERT(varbinary(max), CONVERT(nvarchar(100), CONVERT(varchar(100), [pais])))) THROW 51000, 'No se permite perder metadatos ni caracteres Unicode de aeropuertos. Prepare una migracion correctiva que conserve los datos.', 1;" );

            migrationBuilder.DropCheckConstraint(
                name: "CK_aeropuertos_datos_origen_json",
                schema: "dbo",
                table: "aeropuertos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_aeropuertos_latitud",
                schema: "dbo",
                table: "aeropuertos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_aeropuertos_longitud",
                schema: "dbo",
                table: "aeropuertos");

            migrationBuilder.DropColumn(
                name: "codigo_gps",
                schema: "dbo",
                table: "aeropuertos");

            migrationBuilder.DropColumn(
                name: "codigo_icao",
                schema: "dbo",
                table: "aeropuertos");

            migrationBuilder.DropColumn(
                name: "codigo_local",
                schema: "dbo",
                table: "aeropuertos");

            migrationBuilder.DropColumn(
                name: "continente",
                schema: "dbo",
                table: "aeropuertos");

            migrationBuilder.DropColumn(
                name: "datos_origen_json",
                schema: "dbo",
                table: "aeropuertos");

            migrationBuilder.DropColumn(
                name: "elevacion_pies",
                schema: "dbo",
                table: "aeropuertos");

            migrationBuilder.DropColumn(
                name: "id_origen",
                schema: "dbo",
                table: "aeropuertos");

            migrationBuilder.DropColumn(
                name: "identificador",
                schema: "dbo",
                table: "aeropuertos");

            migrationBuilder.DropColumn(
                name: "latitud",
                schema: "dbo",
                table: "aeropuertos");

            migrationBuilder.DropColumn(
                name: "longitud",
                schema: "dbo",
                table: "aeropuertos");

            migrationBuilder.DropColumn(
                name: "palabras_clave",
                schema: "dbo",
                table: "aeropuertos");

            migrationBuilder.DropColumn(
                name: "region_iso",
                schema: "dbo",
                table: "aeropuertos");

            migrationBuilder.DropColumn(
                name: "servicio_regular",
                schema: "dbo",
                table: "aeropuertos");

            migrationBuilder.DropColumn(
                name: "sitio_web",
                schema: "dbo",
                table: "aeropuertos");

            migrationBuilder.DropColumn(
                name: "tipo",
                schema: "dbo",
                table: "aeropuertos");

            migrationBuilder.DropColumn(
                name: "wikipedia",
                schema: "dbo",
                table: "aeropuertos");

            migrationBuilder.AlterColumn<string>(
                name: "pais",
                schema: "dbo",
                table: "aeropuertos",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "nombre",
                schema: "dbo",
                table: "aeropuertos",
                type: "varchar(150)",
                unicode: false,
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "ciudad",
                schema: "dbo",
                table: "aeropuertos",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
