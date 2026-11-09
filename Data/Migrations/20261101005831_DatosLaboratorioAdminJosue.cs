using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vuelos_Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class DatosLaboratorioAdminJosue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Solo laboratorio. Hashes estables, generados una vez con PasswordHasher.
            // Nunca reemplazar datos, roles o contraseñas de cuentas existentes.
            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1 FROM [dbo].[usuarios] WITH (UPDLOCK, HOLDLOCK)
                    WHERE LOWER(LTRIM(RTRIM([email]))) = 'admin@pacificnexus.test'
                )
                BEGIN
                    INSERT INTO [dbo].[usuarios]
                        ([nombre], [apellido], [email], [password_hash], [rol],
                         [fecha_creacion], [eliminado], [fecha_eliminacion])
                    VALUES
                        ('Administrador', 'Nexus', 'admin@pacificnexus.test',
                         'AQAAAAIAAYagAAAAEFU7mwItGWNLpJqEAQkzmTX/4/mVeb0EUCH+9VALyv72rAoSbFSryWD8aqGr/1sfAw==', 'admin', SYSUTCDATETIME(), 0, NULL);
                END;
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1 FROM [dbo].[usuarios] WITH (UPDLOCK, HOLDLOCK)
                    WHERE LOWER(LTRIM(RTRIM([email]))) = 'josue@pacificnexus.test'
                )
                BEGIN
                    INSERT INTO [dbo].[usuarios]
                        ([nombre], [apellido], [email], [password_hash], [rol],
                         [fecha_creacion], [eliminado], [fecha_eliminacion])
                    VALUES
                        ('Josue', 'Laboratorio', 'josue@pacificnexus.test',
                         'AQAAAAIAAYagAAAAEKHJq6eC3ePmaGDplniohxlMcjr9hR9WD7Dlw2BL/TYMxZ/reQzGCuDazfdLXxdEJg==', 'cliente', SYSUTCDATETIME(), 0, NULL);
                END;
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Conservar cuentas e historial al revertir; no borrar ni desactivar
            // usuarios que pudieron adquirir reservas o cambiar sus datos.

        }
    }
}
