using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vuelos_Backend.Data.Migrations.PostgreSql;

public partial class CiudadAeropuertoOpcional : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "ciudad", schema: "pna", table: "aeropuertos",
            type: "character varying(100)", maxLength: 100, nullable: true,
            oldClrType: typeof(string), oldType: "character varying(100)", oldMaxLength: 100);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // No inventar ciudades ni sustituir NULL por texto vacío al revertir.
        migrationBuilder.Sql("""
            DO $ciudad$
            BEGIN
                IF EXISTS (SELECT 1 FROM pna.aeropuertos WHERE ciudad IS NULL) THEN
                    RAISE EXCEPTION 'Existen aeropuertos sin ciudad. Complete sus datos antes de revertir esta migración.';
                END IF;
            END $ciudad$;
            """);
        migrationBuilder.AlterColumn<string>(
            name: "ciudad", schema: "pna", table: "aeropuertos",
            type: "character varying(100)", maxLength: 100, nullable: false,
            oldClrType: typeof(string), oldType: "character varying(100)", oldMaxLength: 100, oldNullable: true);
    }
}
