using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vuelos_Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReservasTemporalesCompraSimulada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_reservas_estado",
                schema: "dbo",
                table: "reservas");

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_expiracion",
                schema: "dbo",
                table: "reservas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "compras_simuladas",
                schema: "dbo",
                columns: table => new
                {
                    compra_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    reserva_id = table.Column<int>(type: "int", nullable: false),
                    usuario_id = table.Column<int>(type: "int", nullable: false),
                    fecha_compra = table.Column<DateTime>(type: "datetime2", nullable: false),
                    monto = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    moneda = table.Column<string>(type: "char(3)", nullable: false),
                    es_simulada = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compras_simuladas", x => x.compra_id);
                    table.CheckConstraint("CK_compras_simuladas_laboratorio", "[es_simulada] = 1 AND [moneda] = 'USD' AND [monto] >= 0");
                    table.ForeignKey(
                        name: "FK_compras_simuladas_reservas_reserva_id",
                        column: x => x.reserva_id,
                        principalSchema: "dbo",
                        principalTable: "reservas",
                        principalColumn: "reserva_id");
                    table.ForeignKey(
                        name: "FK_compras_simuladas_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "dbo",
                        principalTable: "usuarios",
                        principalColumn: "usuario_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_reservas_vencimiento",
                schema: "dbo",
                table: "reservas",
                columns: new[] { "estado", "fecha_expiracion", "vuelo_id" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_reservas_estado",
                schema: "dbo",
                table: "reservas",
                sql: "[estado] IN ('confirmada', 'cancelada', 'modificada', 'pendiente_pago', 'pagada', 'expirada')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_reservas_vencimiento",
                schema: "dbo",
                table: "reservas",
                sql: "[estado] NOT IN ('pendiente_pago', 'pagada', 'expirada') OR [fecha_expiracion] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_compras_simuladas_usuario_id",
                schema: "dbo",
                table: "compras_simuladas",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "UX_compras_simuladas_reserva",
                schema: "dbo",
                table: "compras_simuladas",
                column: "reserva_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM dbo.compras_simuladas)
                OR EXISTS (SELECT 1 FROM dbo.reservas WHERE fecha_expiracion IS NOT NULL
                    OR estado IN ('pendiente_pago', 'pagada', 'expirada'))
                    THROW 51003, 'No puede retirar el flujo temporal sin revisar reservas y comprobantes existentes.', 1;
                """);
            migrationBuilder.DropTable(
                name: "compras_simuladas",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "IX_reservas_vencimiento",
                schema: "dbo",
                table: "reservas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_reservas_estado",
                schema: "dbo",
                table: "reservas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_reservas_vencimiento",
                schema: "dbo",
                table: "reservas");

            migrationBuilder.DropColumn(
                name: "fecha_expiracion",
                schema: "dbo",
                table: "reservas");

            migrationBuilder.AddCheckConstraint(
                name: "CK_reservas_estado",
                schema: "dbo",
                table: "reservas",
                sql: "[estado] IN ('confirmada', 'cancelada', 'modificada')");
        }
    }
}
