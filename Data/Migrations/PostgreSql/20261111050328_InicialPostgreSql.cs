using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Vuelos_Backend.Data.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class InicialPostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Solo una instalación nueva: no adoptar tablas/historial SQL Server
            // ni modificar un esquema pna usado por otra instalación.
            migrationBuilder.Sql("""
                DO $pna_guard$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_catalog.pg_class c
                        JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
                        WHERE n.nspname = 'pna' AND c.relkind NOT IN ('i', 'I')
                          AND c.relname <> '__EFMigrationsHistory'
                    ) OR EXISTS (
                        SELECT 1 FROM pg_catalog.pg_proc p
                        JOIN pg_catalog.pg_namespace n ON n.oid = p.pronamespace
                        WHERE n.nspname = 'pna'
                    ) OR EXISTS (
                        SELECT 1 FROM pg_catalog.pg_type t
                        JOIN pg_catalog.pg_namespace n ON n.oid = t.typnamespace
                        WHERE n.nspname = 'pna' AND t.typtype IN ('e', 'd')
                    ) THEN
                        RAISE EXCEPTION 'El esquema pna no esta vacio. Revise su esquema e historial antes de aplicar InicialPostgreSql.';
                    END IF;
                END;
                $pna_guard$;
                """);

            migrationBuilder.EnsureSchema(
                name: "pna");

            migrationBuilder.CreateTable(
                name: "aeropuertos",
                schema: "pna",
                columns: table => new
                {
                    aeropuerto_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    fecha_desactivacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    codigo_iata = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ciudad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    pais = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    id_origen = table.Column<long>(type: "bigint", nullable: true),
                    identificador = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    tipo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    latitud = table.Column<double>(type: "double precision", nullable: true),
                    longitud = table.Column<double>(type: "double precision", nullable: true),
                    elevacion_pies = table.Column<double>(type: "double precision", nullable: true),
                    continente = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    region_iso = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    servicio_regular = table.Column<bool>(type: "boolean", nullable: true),
                    codigo_icao = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    codigo_gps = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    codigo_local = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    sitio_web = table.Column<string>(type: "text", nullable: true),
                    wikipedia = table.Column<string>(type: "text", nullable: true),
                    palabras_clave = table.Column<string>(type: "text", nullable: true),
                    datos_origen_json = table.Column<string>(type: "json", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aeropuertos", x => x.aeropuerto_id);
                    table.UniqueConstraint("UQ_aeropuertos_codigo_iata", x => x.codigo_iata);
                    table.CheckConstraint("CK_aeropuertos_datos_origen_json", "\"datos_origen_json\" IS NULL OR json_typeof(\"datos_origen_json\") IN ('object', 'array')");
                    table.CheckConstraint("CK_aeropuertos_latitud", "\"latitud\" IS NULL OR \"latitud\" BETWEEN -90 AND 90");
                    table.CheckConstraint("CK_aeropuertos_longitud", "\"longitud\" IS NULL OR \"longitud\" BETWEEN -180 AND 180");
                    table.CheckConstraint("CK_aeropuertos_version_8_bytes", "octet_length(\"row_version\") = 8");
                });

            migrationBuilder.CreateTable(
                name: "aviones",
                schema: "pna",
                columns: table => new
                {
                    avion_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    fecha_desactivacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    modelo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    matricula = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    capacidad_total = table.Column<int>(type: "integer", nullable: false),
                    codigo_iata = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    codigo_icao = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    cantidad_primera_clase = table.Column<int>(type: "integer", nullable: true),
                    cantidad_segunda_clase = table.Column<int>(type: "integer", nullable: true),
                    cantidad_tercera_clase = table.Column<int>(type: "integer", nullable: true),
                    letras_por_fila = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true),
                    datos_origen_json = table.Column<string>(type: "json", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aviones", x => x.avion_id);
                    table.UniqueConstraint("UQ_aviones_matricula", x => x.matricula);
                    table.CheckConstraint("CK_aviones_capacidad_total", "\"capacidad_total\" > 0");
                    table.CheckConstraint("CK_aviones_datos_origen_json", "\"datos_origen_json\" IS NULL OR json_typeof(\"datos_origen_json\") IN ('object', 'array')");
                    table.CheckConstraint("CK_aviones_distribucion_importada", "(\"cantidad_primera_clase\" IS NULL AND \"cantidad_segunda_clase\" IS NULL AND \"cantidad_tercera_clase\" IS NULL) OR (\"cantidad_primera_clase\" IS NOT NULL AND \"cantidad_segunda_clase\" IS NOT NULL AND \"cantidad_tercera_clase\" IS NOT NULL AND \"cantidad_primera_clase\" >= 0 AND \"cantidad_segunda_clase\" >= 0 AND \"cantidad_tercera_clase\" >= 0 AND CAST(\"cantidad_primera_clase\" AS bigint) + CAST(\"cantidad_segunda_clase\" AS bigint) + CAST(\"cantidad_tercera_clase\" AS bigint) = \"capacidad_total\")");
                    table.CheckConstraint("CK_aviones_version_8_bytes", "octet_length(\"row_version\") = 8");
                });

            migrationBuilder.CreateTable(
                name: "categorias_asiento",
                schema: "pna",
                columns: table => new
                {
                    categoria_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    fecha_desactivacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias_asiento", x => x.categoria_id);
                    table.UniqueConstraint("UQ_categorias_asiento_nombre", x => x.nombre);
                    table.CheckConstraint("CK_categorias_asiento_version_8_bytes", "octet_length(\"row_version\") = 8");
                });

            migrationBuilder.CreateTable(
                name: "eventos_paypal",
                schema: "pna",
                columns: table => new
                {
                    EventoId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FechaProcesado = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventos_paypal", x => x.EventoId);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                schema: "pna",
                columns: table => new
                {
                    usuario_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    apellido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    rol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    eliminado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    fecha_eliminacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.usuario_id);
                    table.UniqueConstraint("UQ_usuarios_email", x => x.email);
                    table.CheckConstraint("CK_usuarios_rol", "\"rol\" IN ('cliente', 'admin')");
                });

            migrationBuilder.CreateTable(
                name: "vuelos",
                schema: "pna",
                columns: table => new
                {
                    vuelo_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    numero_vuelo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    avion_id = table.Column<int>(type: "integer", nullable: false),
                    aeropuerto_origen_id = table.Column<int>(type: "integer", nullable: false),
                    aeropuerto_destino_id = table.Column<int>(type: "integer", nullable: false),
                    salida_programada = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    llegada_programada = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    salida_real = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    llegada_real = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "programado"),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vuelos", x => x.vuelo_id);
                    table.CheckConstraint("CK_vuelos_estado", "\"estado\" IN ('programado', 'embarcando', 'retrasado', 'en vuelo', 'finalizado', 'cancelado')");
                    table.CheckConstraint("CK_vuelos_horarios", "\"llegada_programada\" > \"salida_programada\"");
                    table.CheckConstraint("CK_vuelos_origen_destino", "\"aeropuerto_origen_id\" <> \"aeropuerto_destino_id\"");
                    table.ForeignKey(
                        name: "FK_vuelos_aeropuerto_destino_id",
                        column: x => x.aeropuerto_destino_id,
                        principalSchema: "pna",
                        principalTable: "aeropuertos",
                        principalColumn: "aeropuerto_id");
                    table.ForeignKey(
                        name: "FK_vuelos_aeropuerto_origen_id",
                        column: x => x.aeropuerto_origen_id,
                        principalSchema: "pna",
                        principalTable: "aeropuertos",
                        principalColumn: "aeropuerto_id");
                    table.ForeignKey(
                        name: "FK_vuelos_avion_id",
                        column: x => x.avion_id,
                        principalSchema: "pna",
                        principalTable: "aviones",
                        principalColumn: "avion_id");
                });

            migrationBuilder.CreateTable(
                name: "asientos_fisicos",
                schema: "pna",
                columns: table => new
                {
                    asiento_fisico_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    fecha_desactivacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    avion_id = table.Column<int>(type: "integer", nullable: false),
                    categoria_id = table.Column<int>(type: "integer", nullable: false),
                    codigo_asiento = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asientos_fisicos", x => x.asiento_fisico_id);
                    table.UniqueConstraint("UQ_asientos_fisicos_avion_id_codigo_asiento", x => new { x.avion_id, x.codigo_asiento });
                    table.CheckConstraint("CK_asientos_fisicos_version_8_bytes", "octet_length(\"row_version\") = 8");
                    table.ForeignKey(
                        name: "FK_asientos_fisicos_avion_id",
                        column: x => x.avion_id,
                        principalSchema: "pna",
                        principalTable: "aviones",
                        principalColumn: "avion_id");
                    table.ForeignKey(
                        name: "FK_asientos_fisicos_categoria_id",
                        column: x => x.categoria_id,
                        principalSchema: "pna",
                        principalTable: "categorias_asiento",
                        principalColumn: "categoria_id");
                });

            migrationBuilder.CreateTable(
                name: "Tokens",
                schema: "pna",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    FechaExpiracion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tokens", x => x.Id);
                    table.UniqueConstraint("UQ_Tokens_Token", x => x.Token);
                    table.CheckConstraint("CK_Tokens_Fechas", "\"FechaExpiracion\" > \"FechaCreacion\"");
                    table.ForeignKey(
                        name: "FK_Tokens_usuarios",
                        column: x => x.UsuarioId,
                        principalSchema: "pna",
                        principalTable: "usuarios",
                        principalColumn: "usuario_id");
                });

            migrationBuilder.CreateTable(
                name: "historial_estados_vuelo",
                schema: "pna",
                columns: table => new
                {
                    historial_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    vuelo_id = table.Column<int>(type: "integer", nullable: false),
                    estado_anterior = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    estado_nuevo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_cambio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    usuario_admin_id = table.Column<int>(type: "integer", nullable: false),
                    observacion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historial_estados_vuelo", x => x.historial_id);
                    table.ForeignKey(
                        name: "FK_historial_estados_vuelo_usuario_admin_id",
                        column: x => x.usuario_admin_id,
                        principalSchema: "pna",
                        principalTable: "usuarios",
                        principalColumn: "usuario_id");
                    table.ForeignKey(
                        name: "FK_historial_estados_vuelo_vuelo_id",
                        column: x => x.vuelo_id,
                        principalSchema: "pna",
                        principalTable: "vuelos",
                        principalColumn: "vuelo_id");
                });

            migrationBuilder.CreateTable(
                name: "reservas",
                schema: "pna",
                columns: table => new
                {
                    reserva_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo_reserva = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    vuelo_id = table.Column<int>(type: "integer", nullable: false),
                    fecha_reserva = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    fecha_expiracion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    monto_total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    subtotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    porcentaje_iva = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    monto_iva = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "confirmada"),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservas", x => x.reserva_id);
                    table.UniqueConstraint("UQ_reservas_codigo_reserva", x => x.codigo_reserva);
                    table.CheckConstraint("CK_reservas_desglose_iva", "(\"subtotal\" IS NULL AND \"porcentaje_iva\" IS NULL AND \"monto_iva\" IS NULL) OR (\"subtotal\" IS NOT NULL AND \"porcentaje_iva\" IS NOT NULL AND \"monto_iva\" IS NOT NULL AND \"subtotal\" >= 0 AND \"porcentaje_iva\" BETWEEN 0 AND 100 AND \"monto_iva\" >= 0 AND \"monto_iva\" = ROUND(\"subtotal\" * \"porcentaje_iva\" / 100.0, 2) AND \"monto_total\" = \"subtotal\" + \"monto_iva\")");
                    table.CheckConstraint("CK_reservas_estado", "\"estado\" IN ('confirmada', 'cancelada', 'modificada', 'pendiente_pago', 'pagada', 'expirada')");
                    table.CheckConstraint("CK_reservas_vencimiento", "\"estado\" NOT IN ('pendiente_pago', 'pagada', 'expirada') OR \"fecha_expiracion\" IS NOT NULL");
                    table.CheckConstraint("CK_reservas_version_8_bytes", "octet_length(\"row_version\") = 8");
                    table.ForeignKey(
                        name: "FK_reservas_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "pna",
                        principalTable: "usuarios",
                        principalColumn: "usuario_id");
                    table.ForeignKey(
                        name: "FK_reservas_vuelo_id",
                        column: x => x.vuelo_id,
                        principalSchema: "pna",
                        principalTable: "vuelos",
                        principalColumn: "vuelo_id");
                });

            migrationBuilder.CreateTable(
                name: "asientos_vuelo",
                schema: "pna",
                columns: table => new
                {
                    asiento_vuelo_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    vuelo_id = table.Column<int>(type: "integer", nullable: false),
                    asiento_fisico_id = table.Column<int>(type: "integer", nullable: false),
                    precio = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "disponible"),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asientos_vuelo", x => x.asiento_vuelo_id);
                    table.UniqueConstraint("UQ_asientos_vuelo_vuelo_id_asiento_fisico_id", x => new { x.vuelo_id, x.asiento_fisico_id });
                    table.CheckConstraint("CK_asientos_vuelo_estado", "\"estado\" IN ('disponible', 'reservado', 'bloqueado')");
                    table.CheckConstraint("CK_asientos_vuelo_precio", "\"precio\" >= 0");
                    table.CheckConstraint("CK_asientos_vuelo_version_8_bytes", "octet_length(\"row_version\") = 8");
                    table.ForeignKey(
                        name: "FK_asientos_vuelo_asiento_fisico_id",
                        column: x => x.asiento_fisico_id,
                        principalSchema: "pna",
                        principalTable: "asientos_fisicos",
                        principalColumn: "asiento_fisico_id");
                    table.ForeignKey(
                        name: "FK_asientos_vuelo_vuelo_id",
                        column: x => x.vuelo_id,
                        principalSchema: "pna",
                        principalTable: "vuelos",
                        principalColumn: "vuelo_id");
                });

            migrationBuilder.CreateTable(
                name: "compras_simuladas",
                schema: "pna",
                columns: table => new
                {
                    compra_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reserva_id = table.Column<int>(type: "integer", nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    fecha_compra = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    monto = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    moneda = table.Column<string>(type: "character varying(3)", nullable: false),
                    es_simulada = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compras_simuladas", x => x.compra_id);
                    table.CheckConstraint("CK_compras_simuladas_laboratorio", "\"es_simulada\" = TRUE AND \"moneda\" = 'USD' AND \"monto\" >= 0");
                    table.ForeignKey(
                        name: "FK_compras_simuladas_reservas_reserva_id",
                        column: x => x.reserva_id,
                        principalSchema: "pna",
                        principalTable: "reservas",
                        principalColumn: "reserva_id");
                    table.ForeignKey(
                        name: "FK_compras_simuladas_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "pna",
                        principalTable: "usuarios",
                        principalColumn: "usuario_id");
                });

            migrationBuilder.CreateTable(
                name: "pagos_paypal",
                schema: "pna",
                columns: table => new
                {
                    PagoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservaId = table.Column<int>(type: "integer", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Entorno = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OrdenId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CapturaId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UrlAprobacion = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    MerchantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BloqueaReserva = table.Column<bool>(type: "boolean", nullable: false),
                    CapturaIniciada = table.Column<bool>(type: "boolean", nullable: false),
                    OperacionToken = table.Column<Guid>(type: "uuid", nullable: true),
                    OperacionHasta = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaCaptura = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagos_paypal", x => x.PagoId);
                    table.CheckConstraint("CK_pagos_paypal_captura", "\"FechaCaptura\" IS NULL OR \"CapturaId\" IS NOT NULL");
                    table.CheckConstraint("CK_pagos_paypal_entorno", "\"Entorno\" IN ('Sandbox','Live')");
                    table.CheckConstraint("CK_pagos_paypal_estado", "\"Estado\" IN ('creando','creada','verificando','capturando','pendiente','completado','fallido','revision')");
                    table.CheckConstraint("CK_pagos_paypal_importe", "\"Monto\" > 0 AND \"Moneda\" = 'USD'");
                    table.CheckConstraint("CK_pagos_paypal_version_8_bytes", "octet_length(\"RowVersion\") = 8");
                    table.ForeignKey(
                        name: "FK_pagos_paypal_reservas_ReservaId",
                        column: x => x.ReservaId,
                        principalSchema: "pna",
                        principalTable: "reservas",
                        principalColumn: "reserva_id");
                });

            migrationBuilder.CreateTable(
                name: "detalle_reservas",
                schema: "pna",
                columns: table => new
                {
                    detalle_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reserva_id = table.Column<int>(type: "integer", nullable: false),
                    asiento_vuelo_id = table.Column<int>(type: "integer", nullable: false),
                    primer_nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    segundo_nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    primer_apellido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    segundo_apellido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    documento_identidad = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tipo_pasajero = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    precio_congelado = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "activo")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detalle_reservas", x => x.detalle_id);
                    table.CheckConstraint("CK_detalle_reservas_estado", "\"estado\" IN ('activo', 'liberado')");
                    table.CheckConstraint("CK_detalle_reservas_tipo_pasajero", "\"tipo_pasajero\" IS NULL OR \"tipo_pasajero\" IN ('adulto', 'joven', 'nino', 'bebe')");
                    table.ForeignKey(
                        name: "FK_detalle_reservas_asiento_vuelo_id",
                        column: x => x.asiento_vuelo_id,
                        principalSchema: "pna",
                        principalTable: "asientos_vuelo",
                        principalColumn: "asiento_vuelo_id");
                    table.ForeignKey(
                        name: "FK_detalle_reservas_reserva_id",
                        column: x => x.reserva_id,
                        principalSchema: "pna",
                        principalTable: "reservas",
                        principalColumn: "reserva_id");
                });

            migrationBuilder.CreateTable(
                name: "historial_cambios_reserva",
                schema: "pna",
                columns: table => new
                {
                    historial_reserva_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reserva_id = table.Column<int>(type: "integer", nullable: false),
                    asiento_vuelo_anterior_id = table.Column<int>(type: "integer", nullable: true),
                    asiento_vuelo_nuevo_id = table.Column<int>(type: "integer", nullable: true),
                    tipo_accion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    fecha_accion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    observacion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historial_cambios_reserva", x => x.historial_reserva_id);
                    table.ForeignKey(
                        name: "FK_historial_cambios_reserva_asiento_vuelo_anterior_id",
                        column: x => x.asiento_vuelo_anterior_id,
                        principalSchema: "pna",
                        principalTable: "asientos_vuelo",
                        principalColumn: "asiento_vuelo_id");
                    table.ForeignKey(
                        name: "FK_historial_cambios_reserva_asiento_vuelo_nuevo_id",
                        column: x => x.asiento_vuelo_nuevo_id,
                        principalSchema: "pna",
                        principalTable: "asientos_vuelo",
                        principalColumn: "asiento_vuelo_id");
                    table.ForeignKey(
                        name: "FK_historial_cambios_reserva_reserva_id",
                        column: x => x.reserva_id,
                        principalSchema: "pna",
                        principalTable: "reservas",
                        principalColumn: "reserva_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_asientos_fisicos_categoria_id",
                schema: "pna",
                table: "asientos_fisicos",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_asientos_vuelo_asiento_fisico_id",
                schema: "pna",
                table: "asientos_vuelo",
                column: "asiento_fisico_id");

            migrationBuilder.CreateIndex(
                name: "IX_compras_simuladas_usuario_id",
                schema: "pna",
                table: "compras_simuladas",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "UX_compras_simuladas_reserva",
                schema: "pna",
                table: "compras_simuladas",
                column: "reserva_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_detalle_reservas_reserva_id",
                schema: "pna",
                table: "detalle_reservas",
                column: "reserva_id");

            migrationBuilder.CreateIndex(
                name: "UX_detalle_reservas_asiento_activo",
                schema: "pna",
                table: "detalle_reservas",
                column: "asiento_vuelo_id",
                unique: true,
                filter: "\"estado\" = 'activo'");

            migrationBuilder.CreateIndex(
                name: "IX_historial_cambios_reserva_asiento_vuelo_anterior_id",
                schema: "pna",
                table: "historial_cambios_reserva",
                column: "asiento_vuelo_anterior_id");

            migrationBuilder.CreateIndex(
                name: "IX_historial_cambios_reserva_asiento_vuelo_nuevo_id",
                schema: "pna",
                table: "historial_cambios_reserva",
                column: "asiento_vuelo_nuevo_id");

            migrationBuilder.CreateIndex(
                name: "IX_historial_cambios_reserva_reserva_id",
                schema: "pna",
                table: "historial_cambios_reserva",
                column: "reserva_id");

            migrationBuilder.CreateIndex(
                name: "IX_historial_estados_vuelo_usuario_admin_id",
                schema: "pna",
                table: "historial_estados_vuelo",
                column: "usuario_admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_historial_estados_vuelo_vuelo_id",
                schema: "pna",
                table: "historial_estados_vuelo",
                column: "vuelo_id");

            migrationBuilder.CreateIndex(
                name: "IX_pagos_paypal_BloqueaReserva_FechaActualizacion",
                schema: "pna",
                table: "pagos_paypal",
                columns: new[] { "BloqueaReserva", "FechaActualizacion" });

            migrationBuilder.CreateIndex(
                name: "IX_pagos_paypal_CapturaId",
                schema: "pna",
                table: "pagos_paypal",
                column: "CapturaId",
                unique: true,
                filter: "\"CapturaId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_pagos_paypal_OrdenId",
                schema: "pna",
                table: "pagos_paypal",
                column: "OrdenId",
                unique: true,
                filter: "\"OrdenId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_pagos_paypal_ReservaId",
                schema: "pna",
                table: "pagos_paypal",
                column: "ReservaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reservas_usuario_id",
                schema: "pna",
                table: "reservas",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_reservas_vencimiento",
                schema: "pna",
                table: "reservas",
                columns: new[] { "estado", "fecha_expiracion", "vuelo_id" });

            migrationBuilder.CreateIndex(
                name: "IX_reservas_vuelo_id",
                schema: "pna",
                table: "reservas",
                column: "vuelo_id");

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_UsuarioId",
                schema: "pna",
                table: "Tokens",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_vuelos_aeropuerto_destino_id",
                schema: "pna",
                table: "vuelos",
                column: "aeropuerto_destino_id");

            migrationBuilder.CreateIndex(
                name: "IX_vuelos_aeropuerto_origen_id",
                schema: "pna",
                table: "vuelos",
                column: "aeropuerto_origen_id");

            migrationBuilder.CreateIndex(
                name: "IX_vuelos_avion_id",
                schema: "pna",
                table: "vuelos",
                column: "avion_id");
            ConfigurarVersionesYDatosIniciales(migrationBuilder);
        }

        private static void ConfigurarVersionesYDatosIniciales(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE SEQUENCE pna.row_version_seq AS bigint
                    INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807
                    START WITH 1 CACHE 1 NO CYCLE;

                CREATE FUNCTION pna.generar_version_fila() RETURNS trigger
                LANGUAGE plpgsql SECURITY INVOKER SET search_path = pg_catalog
                AS $pna_version$
                DECLARE
                    nueva_version bytea;
                BEGIN
                    IF TG_NARGS <> 1 OR TG_ARGV[0] NOT IN ('row_version', 'RowVersion') THEN
                        RAISE EXCEPTION 'Columna de version de fila no admitida.';
                    END IF;
                    nueva_version := pg_catalog.decode(pg_catalog.lpad(
                        pg_catalog.to_hex(pg_catalog.nextval('pna.row_version_seq'::pg_catalog.regclass)), 16, '0'), 'hex');
                    NEW := pg_catalog.jsonb_populate_record(NEW,
                        pg_catalog.jsonb_build_object(TG_ARGV[0], nueva_version));
                    RETURN NEW;
                END;
                $pna_version$;
                """);

            foreach (var (table, column) in new[]
            {
                ("aeropuertos", "row_version"), ("aviones", "row_version"),
                ("categorias_asiento", "row_version"), ("asientos_fisicos", "row_version"),
                ("asientos_vuelo", "row_version"), ("reservas", "row_version"),
                ("pagos_paypal", "RowVersion")
            })
            {
                // Identificadores fijos de la migración, nunca entrada del usuario.
                migrationBuilder.Sql($"""
                    CREATE TRIGGER "TR_{table}_version"
                    BEFORE INSERT OR UPDATE ON pna."{table}"
                    FOR EACH ROW EXECUTE FUNCTION pna.generar_version_fila('{column}');
                    """);
            }

            // No se fija ningún ID ni se copian cuentas o precios de laboratorio.
            // Los triggers ya deben existir antes de este INSERT.
            migrationBuilder.Sql("""
                INSERT INTO pna.categorias_asiento (nombre, activo)
                VALUES ('Primera Clase', TRUE), ('Segunda Clase', TRUE), ('Tercera Clase', TRUE);
                """);

            migrationBuilder.Sql("""
                REVOKE ALL ON SCHEMA pna FROM PUBLIC;
                REVOKE ALL ON ALL TABLES IN SCHEMA pna FROM PUBLIC;
                REVOKE ALL ON ALL SEQUENCES IN SCHEMA pna FROM PUBLIC;
                REVOKE ALL ON ALL FUNCTIONS IN SCHEMA pna FROM PUBLIC;
                DO $pna_privilegios$
                DECLARE
                    rol text;
                BEGIN
                    FOREACH rol IN ARRAY ARRAY['anon', 'authenticated', 'service_role'] LOOP
                        IF EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = rol) THEN
                            EXECUTE pg_catalog.format('REVOKE ALL ON SCHEMA pna FROM %I', rol);
                            EXECUTE pg_catalog.format('REVOKE ALL ON ALL TABLES IN SCHEMA pna FROM %I', rol);
                            EXECUTE pg_catalog.format('REVOKE ALL ON ALL SEQUENCES IN SCHEMA pna FROM %I', rol);
                            EXECUTE pg_catalog.format('REVOKE ALL ON ALL FUNCTIONS IN SCHEMA pna FROM %I', rol);
                        END IF;
                    END LOOP;
                END;
                $pna_privilegios$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Una correccion hacia adelante conserva reservas, pagos y catalogos.
            // Bloquear el rollback antes de que EF retire el historial aplicado.
            migrationBuilder.Sql("""
                DO $pna_down$
                BEGIN
                    RAISE EXCEPTION 'InicialPostgreSql no admite rollback destructivo. Prepare una migracion correctiva que conserve los datos.';
                END;
                $pna_down$;
                """);
        }
    }
}
