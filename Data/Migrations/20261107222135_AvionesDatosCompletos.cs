using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vuelos_Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AvionesDatosCompletos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "modelo",
                schema: "dbo",
                table: "aviones",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100);

            migrationBuilder.AddColumn<int>(
                name: "cantidad_primera_clase",
                schema: "dbo",
                table: "aviones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cantidad_segunda_clase",
                schema: "dbo",
                table: "aviones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cantidad_tercera_clase",
                schema: "dbo",
                table: "aviones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "codigo_iata",
                schema: "dbo",
                table: "aviones",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "codigo_icao",
                schema: "dbo",
                table: "aviones",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "datos_origen_json",
                schema: "dbo",
                table: "aviones",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "letras_por_fila",
                schema: "dbo",
                table: "aviones",
                type: "nvarchar(26)",
                maxLength: 26,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_aviones_datos_origen_json",
                schema: "dbo",
                table: "aviones",
                sql: "[datos_origen_json] IS NULL OR ISJSON([datos_origen_json]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_aviones_distribucion_importada",
                schema: "dbo",
                table: "aviones",
                sql: "([cantidad_primera_clase] IS NULL AND [cantidad_segunda_clase] IS NULL AND [cantidad_tercera_clase] IS NULL) OR ([cantidad_primera_clase] IS NOT NULL AND [cantidad_segunda_clase] IS NOT NULL AND [cantidad_tercera_clase] IS NOT NULL AND [cantidad_primera_clase] >= 0 AND [cantidad_segunda_clase] >= 0 AND [cantidad_tercera_clase] >= 0 AND CAST([cantidad_primera_clase] AS bigint) + CAST([cantidad_segunda_clase] AS bigint) + CAST([cantidad_tercera_clase] AS bigint) = [capacidad_total])");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [dbo].[aviones] WHERE [codigo_iata] IS NOT NULL OR [codigo_icao] IS NOT NULL OR [cantidad_primera_clase] IS NOT NULL OR [cantidad_segunda_clase] IS NOT NULL OR [cantidad_tercera_clase] IS NOT NULL OR [letras_por_fila] IS NOT NULL OR [datos_origen_json] IS NOT NULL OR CONVERT(varbinary(max), [modelo]) <> CONVERT(varbinary(max), CONVERT(nvarchar(100), CONVERT(varchar(100), [modelo])))) THROW 51000, 'No se permite perder metadatos ni caracteres Unicode de aviones. Prepare una migracion correctiva que conserve los datos.', 1;");

            migrationBuilder.DropCheckConstraint(
                name: "CK_aviones_datos_origen_json",
                schema: "dbo",
                table: "aviones");

            migrationBuilder.DropCheckConstraint(
                name: "CK_aviones_distribucion_importada",
                schema: "dbo",
                table: "aviones");

            migrationBuilder.DropColumn(
                name: "cantidad_primera_clase",
                schema: "dbo",
                table: "aviones");

            migrationBuilder.DropColumn(
                name: "cantidad_segunda_clase",
                schema: "dbo",
                table: "aviones");

            migrationBuilder.DropColumn(
                name: "cantidad_tercera_clase",
                schema: "dbo",
                table: "aviones");

            migrationBuilder.DropColumn(
                name: "codigo_iata",
                schema: "dbo",
                table: "aviones");

            migrationBuilder.DropColumn(
                name: "codigo_icao",
                schema: "dbo",
                table: "aviones");

            migrationBuilder.DropColumn(
                name: "datos_origen_json",
                schema: "dbo",
                table: "aviones");

            migrationBuilder.DropColumn(
                name: "letras_por_fila",
                schema: "dbo",
                table: "aviones");

            migrationBuilder.AlterColumn<string>(
                name: "modelo",
                schema: "dbo",
                table: "aviones",
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
