using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vuelos_Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class InicialPacificNexus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "aeropuertos",
                schema: "dbo",
                columns: table => new
                {
                    aeropuerto_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codigo_iata = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    nombre = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    ciudad = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    pais = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aeropuertos", x => x.aeropuerto_id);
                    table.UniqueConstraint("UQ_aeropuertos_codigo_iata", x => x.codigo_iata);
                });

            migrationBuilder.CreateTable(
                name: "aviones",
                schema: "dbo",
                columns: table => new
                {
                    avion_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    modelo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    matricula = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    capacidad_total = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aviones", x => x.avion_id);
                    table.UniqueConstraint("UQ_aviones_matricula", x => x.matricula);
                    table.CheckConstraint("CK_aviones_capacidad_total", "[capacidad_total] > 0");
                });

            migrationBuilder.CreateTable(
                name: "categorias_asiento",
                schema: "dbo",
                columns: table => new
                {
                    categoria_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias_asiento", x => x.categoria_id);
                    table.UniqueConstraint("UQ_categorias_asiento_nombre", x => x.nombre);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                schema: "dbo",
                columns: table => new
                {
                    usuario_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    apellido = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    password_hash = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    rol = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    eliminado = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                        .Annotation("Relational:DefaultConstraintName", "DF_usuarios_eliminado"),
                    fecha_eliminacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.usuario_id);
                    table.UniqueConstraint("UQ_usuarios_email", x => x.email);
                    table.CheckConstraint("CK_usuarios_rol", "[rol] IN ('cliente', 'admin')");
                });

            migrationBuilder.CreateTable(
                name: "vuelos",
                schema: "dbo",
                columns: table => new
                {
                    vuelo_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    numero_vuelo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    avion_id = table.Column<int>(type: "int", nullable: false),
                    aeropuerto_origen_id = table.Column<int>(type: "int", nullable: false),
                    aeropuerto_destino_id = table.Column<int>(type: "int", nullable: false),
                    salida_programada = table.Column<DateTime>(type: "datetime2", nullable: false),
                    llegada_programada = table.Column<DateTime>(type: "datetime2", nullable: false),
                    salida_real = table.Column<DateTime>(type: "datetime2", nullable: true),
                    llegada_real = table.Column<DateTime>(type: "datetime2", nullable: true),
                    estado = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "programado"),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vuelos", x => x.vuelo_id);
                    table.CheckConstraint("CK_vuelos_estado", "[estado] IN ('programado', 'embarcando', 'retrasado', 'en vuelo', 'finalizado', 'cancelado')");
                    table.CheckConstraint("CK_vuelos_horarios", "[llegada_programada] > [salida_programada]");
                    table.CheckConstraint("CK_vuelos_origen_destino", "[aeropuerto_origen_id] <> [aeropuerto_destino_id]");
                    table.ForeignKey(
                        name: "FK_vuelos_aeropuerto_destino_id",
                        column: x => x.aeropuerto_destino_id,
                        principalSchema: "dbo",
                        principalTable: "aeropuertos",
                        principalColumn: "aeropuerto_id");
                    table.ForeignKey(
                        name: "FK_vuelos_aeropuerto_origen_id",
                        column: x => x.aeropuerto_origen_id,
                        principalSchema: "dbo",
                        principalTable: "aeropuertos",
                        principalColumn: "aeropuerto_id");
                    table.ForeignKey(
                        name: "FK_vuelos_avion_id",
                        column: x => x.avion_id,
                        principalSchema: "dbo",
                        principalTable: "aviones",
                        principalColumn: "avion_id");
                });

            migrationBuilder.CreateTable(
                name: "asientos_fisicos",
                schema: "dbo",
                columns: table => new
                {
                    asiento_fisico_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    avion_id = table.Column<int>(type: "int", nullable: false),
                    categoria_id = table.Column<int>(type: "int", nullable: false),
                    codigo_asiento = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asientos_fisicos", x => x.asiento_fisico_id);
                    table.UniqueConstraint("UQ_asientos_fisicos_avion_id_codigo_asiento", x => new { x.avion_id, x.codigo_asiento });
                    table.ForeignKey(
                        name: "FK_asientos_fisicos_avion_id",
                        column: x => x.avion_id,
                        principalSchema: "dbo",
                        principalTable: "aviones",
                        principalColumn: "avion_id");
                    table.ForeignKey(
                        name: "FK_asientos_fisicos_categoria_id",
                        column: x => x.categoria_id,
                        principalSchema: "dbo",
                        principalTable: "categorias_asiento",
                        principalColumn: "categoria_id");
                });

            migrationBuilder.CreateTable(
                name: "Tokens",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tokens", x => x.Id);
                    table.UniqueConstraint("UQ_Tokens_Token", x => x.Token);
                    table.CheckConstraint("CK_Tokens_Fechas", "[FechaExpiracion] > [FechaCreacion]");
                    table.ForeignKey(
                        name: "FK_Tokens_usuarios",
                        column: x => x.UsuarioId,
                        principalSchema: "dbo",
                        principalTable: "usuarios",
                        principalColumn: "usuario_id");
                });

            migrationBuilder.CreateTable(
                name: "historial_estados_vuelo",
                schema: "dbo",
                columns: table => new
                {
                    historial_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    vuelo_id = table.Column<int>(type: "int", nullable: false),
                    estado_anterior = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    estado_nuevo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    fecha_cambio = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    usuario_admin_id = table.Column<int>(type: "int", nullable: false),
                    observacion = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historial_estados_vuelo", x => x.historial_id);
                    table.ForeignKey(
                        name: "FK_historial_estados_vuelo_usuario_admin_id",
                        column: x => x.usuario_admin_id,
                        principalSchema: "dbo",
                        principalTable: "usuarios",
                        principalColumn: "usuario_id");
                    table.ForeignKey(
                        name: "FK_historial_estados_vuelo_vuelo_id",
                        column: x => x.vuelo_id,
                        principalSchema: "dbo",
                        principalTable: "vuelos",
                        principalColumn: "vuelo_id");
                });

            migrationBuilder.CreateTable(
                name: "reservas",
                schema: "dbo",
                columns: table => new
                {
                    reserva_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codigo_reserva = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    usuario_id = table.Column<int>(type: "int", nullable: false),
                    vuelo_id = table.Column<int>(type: "int", nullable: false),
                    fecha_reserva = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    monto_total = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    estado = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "confirmada"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservas", x => x.reserva_id);
                    table.UniqueConstraint("UQ_reservas_codigo_reserva", x => x.codigo_reserva);
                    table.CheckConstraint("CK_reservas_estado", "[estado] IN ('confirmada', 'cancelada', 'modificada')");
                    table.ForeignKey(
                        name: "FK_reservas_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "dbo",
                        principalTable: "usuarios",
                        principalColumn: "usuario_id");
                    table.ForeignKey(
                        name: "FK_reservas_vuelo_id",
                        column: x => x.vuelo_id,
                        principalSchema: "dbo",
                        principalTable: "vuelos",
                        principalColumn: "vuelo_id");
                });

            migrationBuilder.CreateTable(
                name: "asientos_vuelo",
                schema: "dbo",
                columns: table => new
                {
                    asiento_vuelo_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    vuelo_id = table.Column<int>(type: "int", nullable: false),
                    asiento_fisico_id = table.Column<int>(type: "int", nullable: false),
                    precio = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "disponible"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asientos_vuelo", x => x.asiento_vuelo_id);
                    table.UniqueConstraint("UQ_asientos_vuelo_vuelo_id_asiento_fisico_id", x => new { x.vuelo_id, x.asiento_fisico_id });
                    table.CheckConstraint("CK_asientos_vuelo_estado", "[estado] IN ('disponible', 'reservado', 'bloqueado')");
                    table.CheckConstraint("CK_asientos_vuelo_precio", "[precio] >= 0");
                    table.ForeignKey(
                        name: "FK_asientos_vuelo_asiento_fisico_id",
                        column: x => x.asiento_fisico_id,
                        principalSchema: "dbo",
                        principalTable: "asientos_fisicos",
                        principalColumn: "asiento_fisico_id");
                    table.ForeignKey(
                        name: "FK_asientos_vuelo_vuelo_id",
                        column: x => x.vuelo_id,
                        principalSchema: "dbo",
                        principalTable: "vuelos",
                        principalColumn: "vuelo_id");
                });

            migrationBuilder.CreateTable(
                name: "detalle_reservas",
                schema: "dbo",
                columns: table => new
                {
                    detalle_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    reserva_id = table.Column<int>(type: "int", nullable: false),
                    asiento_vuelo_id = table.Column<int>(type: "int", nullable: false),
                    nombre_pasajero = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    apellido_pasajero = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    documento_identidad = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    precio_congelado = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "activo")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detalle_reservas", x => x.detalle_id);
                    table.CheckConstraint("CK_detalle_reservas_estado", "[estado] IN ('activo', 'liberado')");
                    table.ForeignKey(
                        name: "FK_detalle_reservas_asiento_vuelo_id",
                        column: x => x.asiento_vuelo_id,
                        principalSchema: "dbo",
                        principalTable: "asientos_vuelo",
                        principalColumn: "asiento_vuelo_id");
                    table.ForeignKey(
                        name: "FK_detalle_reservas_reserva_id",
                        column: x => x.reserva_id,
                        principalSchema: "dbo",
                        principalTable: "reservas",
                        principalColumn: "reserva_id");
                });

            migrationBuilder.CreateTable(
                name: "historial_cambios_reserva",
                schema: "dbo",
                columns: table => new
                {
                    historial_reserva_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    reserva_id = table.Column<int>(type: "int", nullable: false),
                    asiento_vuelo_anterior_id = table.Column<int>(type: "int", nullable: true),
                    asiento_vuelo_nuevo_id = table.Column<int>(type: "int", nullable: true),
                    tipo_accion = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    fecha_accion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    observacion = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historial_cambios_reserva", x => x.historial_reserva_id);
                    table.ForeignKey(
                        name: "FK_historial_cambios_reserva_asiento_vuelo_anterior_id",
                        column: x => x.asiento_vuelo_anterior_id,
                        principalSchema: "dbo",
                        principalTable: "asientos_vuelo",
                        principalColumn: "asiento_vuelo_id");
                    table.ForeignKey(
                        name: "FK_historial_cambios_reserva_asiento_vuelo_nuevo_id",
                        column: x => x.asiento_vuelo_nuevo_id,
                        principalSchema: "dbo",
                        principalTable: "asientos_vuelo",
                        principalColumn: "asiento_vuelo_id");
                    table.ForeignKey(
                        name: "FK_historial_cambios_reserva_reserva_id",
                        column: x => x.reserva_id,
                        principalSchema: "dbo",
                        principalTable: "reservas",
                        principalColumn: "reserva_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_asientos_fisicos_categoria_id",
                schema: "dbo",
                table: "asientos_fisicos",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_asientos_vuelo_asiento_fisico_id",
                schema: "dbo",
                table: "asientos_vuelo",
                column: "asiento_fisico_id");

            migrationBuilder.CreateIndex(
                name: "IX_detalle_reservas_reserva_id",
                schema: "dbo",
                table: "detalle_reservas",
                column: "reserva_id");

            migrationBuilder.CreateIndex(
                name: "UX_detalle_reservas_asiento_activo",
                schema: "dbo",
                table: "detalle_reservas",
                column: "asiento_vuelo_id",
                unique: true,
                filter: "[estado] = 'activo'");

            migrationBuilder.CreateIndex(
                name: "IX_historial_cambios_reserva_asiento_vuelo_anterior_id",
                schema: "dbo",
                table: "historial_cambios_reserva",
                column: "asiento_vuelo_anterior_id");

            migrationBuilder.CreateIndex(
                name: "IX_historial_cambios_reserva_asiento_vuelo_nuevo_id",
                schema: "dbo",
                table: "historial_cambios_reserva",
                column: "asiento_vuelo_nuevo_id");

            migrationBuilder.CreateIndex(
                name: "IX_historial_cambios_reserva_reserva_id",
                schema: "dbo",
                table: "historial_cambios_reserva",
                column: "reserva_id");

            migrationBuilder.CreateIndex(
                name: "IX_historial_estados_vuelo_usuario_admin_id",
                schema: "dbo",
                table: "historial_estados_vuelo",
                column: "usuario_admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_historial_estados_vuelo_vuelo_id",
                schema: "dbo",
                table: "historial_estados_vuelo",
                column: "vuelo_id");

            migrationBuilder.CreateIndex(
                name: "IX_reservas_usuario_id",
                schema: "dbo",
                table: "reservas",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_reservas_vuelo_id",
                schema: "dbo",
                table: "reservas",
                column: "vuelo_id");

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_UsuarioId",
                schema: "dbo",
                table: "Tokens",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_vuelos_aeropuerto_destino_id",
                schema: "dbo",
                table: "vuelos",
                column: "aeropuerto_destino_id");

            migrationBuilder.CreateIndex(
                name: "IX_vuelos_aeropuerto_origen_id",
                schema: "dbo",
                table: "vuelos",
                column: "aeropuerto_origen_id");

            migrationBuilder.CreateIndex(
                name: "IX_vuelos_avion_id",
                schema: "dbo",
                table: "vuelos",
                column: "avion_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "detalle_reservas",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "historial_cambios_reserva",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "historial_estados_vuelo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Tokens",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "asientos_vuelo",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "reservas",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "asientos_fisicos",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "usuarios",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "vuelos",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "categorias_asiento",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "aeropuertos",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "aviones",
                schema: "dbo");
        }
    }
}
