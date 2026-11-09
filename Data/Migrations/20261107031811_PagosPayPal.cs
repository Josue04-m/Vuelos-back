using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vuelos_Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class PagosPayPal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "eventos_paypal",
                schema: "dbo",
                columns: table => new
                {
                    EventoId = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Tipo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    FechaProcesado = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventos_paypal", x => x.EventoId);
                });

            migrationBuilder.CreateTable(
                name: "pagos_paypal",
                schema: "dbo",
                columns: table => new
                {
                    PagoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservaId = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Moneda = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    Entorno = table.Column<string>(type: "varchar(7)", unicode: false, maxLength: 7, nullable: false),
                    Estado = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    OrdenId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    CapturaId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    UrlAprobacion = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    MerchantId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    BloqueaReserva = table.Column<bool>(type: "bit", nullable: false),
                    CapturaIniciada = table.Column<bool>(type: "bit", nullable: false),
                    OperacionToken = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OperacionHasta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCaptura = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagos_paypal", x => x.PagoId);
                    table.CheckConstraint("CK_pagos_paypal_captura", "[FechaCaptura] IS NULL OR [CapturaId] IS NOT NULL");
                    table.CheckConstraint("CK_pagos_paypal_entorno", "[Entorno] IN ('Sandbox','Live')");
                    table.CheckConstraint("CK_pagos_paypal_estado", "[Estado] IN ('creando','creada','verificando','capturando','pendiente','completado','fallido','revision')");
                    table.CheckConstraint("CK_pagos_paypal_importe", "[Monto] > 0 AND [Moneda] = 'USD'");
                    table.ForeignKey(
                        name: "FK_pagos_paypal_reservas_ReservaId",
                        column: x => x.ReservaId,
                        principalSchema: "dbo",
                        principalTable: "reservas",
                        principalColumn: "reserva_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_pagos_paypal_BloqueaReserva_FechaActualizacion",
                schema: "dbo",
                table: "pagos_paypal",
                columns: new[] { "BloqueaReserva", "FechaActualizacion" });

            migrationBuilder.CreateIndex(
                name: "IX_pagos_paypal_CapturaId",
                schema: "dbo",
                table: "pagos_paypal",
                column: "CapturaId",
                unique: true,
                filter: "[CapturaId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_pagos_paypal_OrdenId",
                schema: "dbo",
                table: "pagos_paypal",
                column: "OrdenId",
                unique: true,
                filter: "[OrdenId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_pagos_paypal_ReservaId",
                schema: "dbo",
                table: "pagos_paypal",
                column: "ReservaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [dbo].[pagos_paypal]) OR EXISTS (SELECT 1 FROM [dbo].[eventos_paypal]) THROW 51000, 'No se permite eliminar el historial PayPal. Conserve los datos y prepare una migracion correctiva.', 1;");
            migrationBuilder.DropTable(
                name: "eventos_paypal",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "pagos_paypal",
                schema: "dbo");
        }
    }
}
