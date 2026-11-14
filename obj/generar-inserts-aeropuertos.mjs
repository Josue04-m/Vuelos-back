import fs from 'node:fs';
import crypto from 'node:crypto';
const root='C:/Users/Josue/Desktop/Aplicaciones Ditribuidas/Proyecto/Vuelos_Backend/Vuelos_Backend';
const source=root+'/docs/airports_laboratorio/airports_laboratorio.csv';
const csvText=fs.readFileSync(source,'utf8');
function parse(text){const rows=[];let row=[],s='',quoted=false;for(let i=0;i<text.length;i++){const c=text[i];if(c==='"'){if(quoted&&text[i+1]===c){s+=c;i++;}else quoted=!quoted;}else if(c===','&&!quoted){row.push(s);s='';}else if(c==='\n'&&!quoted){row.push(s.replace(/\r$/,''));rows.push(row);row=[];s='';}else s+=c;}if(row.length||s){row.push(s);rows.push(row);}if(quoted)throw Error('CSV mal formado');return rows;}
const parsed=parse(csvText),headers=parsed.shift();
const data=parsed.map(r=>{if(r.length!==headers.length)throw Error('Columnas CSV diferentes');return Object.fromEntries(headers.map((h,i)=>[h,r[i]]));});
if(data.length!==8799||new Set(data.map(a=>a.iata_code)).size!==data.length)throw Error('Conteo/IATA duplicado inesperado');
const optional=x=>x==null||x.trim()===''||x.trim()==='\\N'?null:x.trim();
function str(value,type='text'){if(value===null)return 'NULL::'+type;if(String(value).includes('\0'))throw Error('Carácter NUL');return "'"+String(value).replaceAll("'","''")+"'"+(type==='json'?'::json':'');}
function number(value,type){const s=optional(value);if(s===null)return 'NULL::'+type;if(!Number.isFinite(Number(s)))throw Error('Número inválido');if(type==='bigint'&&!/^\d+$/.test(s))throw Error('ID inválido');return s;}
const columns=['codigo_iata','nombre','ciudad','pais','id_origen','identificador','tipo','latitud','longitud','elevacion_pies','continente','region_iso','servicio_regular','codigo_icao','codigo_gps','codigo_local','sitio_web','wikipedia','palabras_clave','datos_origen_json','activo','fecha_desactivacion'];
const rows=data.map(a=>{
 if(!/^[A-Z]{3}$/.test(a.iata_code)||!/^[A-Z]{2}$/.test(a.iso_country)||!a.name||a.name.length>150||a.municipality.length>100)throw Error('Dato no válido');
 const values=[str(a.iata_code),str(a.name.trim()),str(optional(a.municipality)),str(a.iso_country),
  number(a.id,'bigint'),str(optional(a.ident)),str(optional(a.type)),number(a.latitude_deg,'double precision'),number(a.longitude_deg,'double precision'),number(a.elevation_ft,'double precision'),
  str(optional(a.continent)),str(optional(a.iso_region)),a.scheduled_service==='yes'?'TRUE':a.scheduled_service==='no'?'FALSE':'NULL::boolean',
  str(optional(a.icao_code)),str(optional(a.gps_code)),str(optional(a.local_code)),str(optional(a.home_link)),str(optional(a.wikipedia_link)),str(optional(a.keywords)),
  str(JSON.stringify(a),'json'),'TRUE','NULL::timestamp with time zone'];
 if(values.length!==columns.length)throw Error('Columnas INSERT diferentes');
 return '    ('+values.join(', ')+')';
});
const sql=`BEGIN;
SET LOCAL standard_conforming_strings = on;
SET LOCAL lock_timeout = '10s';
SELECT pg_advisory_xact_lock(1347305793, 1);

-- Solo se comprueban estructuras existentes; no se crean ni alteran tablas.
DO $pna_check$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_trigger t
        JOIN pg_class c ON c.oid = t.tgrelid
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_proc p ON p.oid = t.tgfoid
        WHERE n.nspname = 'pna' AND c.relname = 'aeropuertos'
          AND p.proname = 'generar_version_fila'
          AND NOT t.tgisinternal AND t.tgenabled IN ('O', 'A')
          AND (t.tgtype::integer & 7) = 7
    ) THEN
        RAISE EXCEPTION 'Falta el trigger BEFORE INSERT de row_version en pna.aeropuertos. Revise la migración inicial antes de cargar datos.';
    END IF;
END $pna_check$;

-- Las 8799 filas están incluidas aquí; no requiere archivos externos.
WITH datos (${columns.join(', ')}) AS (
    VALUES
${rows.join(',\n')}
), insertados AS (
    INSERT INTO pna.aeropuertos (${columns.join(', ')})
    SELECT ${columns.map(c=>'d.'+c).join(', ')}
    FROM datos d
    WHERE NOT EXISTS (
        SELECT 1 FROM pna.aeropuertos a
        WHERE upper(btrim(a.codigo_iata)) = d.codigo_iata
    )
    ON CONFLICT (codigo_iata) DO NOTHING
    RETURNING aeropuerto_id
)
SELECT 8799 AS registros_del_archivo,
       count(*) AS aeropuertos_insertados,
       8799 - count(*) AS omitidos_por_iata_existente
FROM insertados;

COMMIT;
`;
const hash=crypto.createHash('sha256').update(csvText).digest('hex');
const markdown=`# INSERT de aeropuertos para Supabase — laboratorio

Este script carga hasta **8799 aeropuertos** del archivo \\docs/airports_laboratorio/airports_laboratorio.csv. Incluye **359 ciudades ficticias**, identificadas como \\Localidad ficticia IATA, según lo solicitado para el laboratorio. Los aeropuertos, IATA y el resto de los datos conservan los valores del CSV preparado.

## Cómo ejecutarlo

1. Abre **Supabase → SQL Editor → New query** en la base del proyecto.
2. Copia únicamente el contenido del bloque SQL completo, desde BEGIN hasta COMMIT, sin las marcas de Markdown.
3. Ejecuta el bloque completo. El resultado muestra insertados y omitidos. Si aparece un error, ejecuta ROLLBACK antes de volver a intentar.

No requiere subir CSV, usar Swagger, modificar el backend ni aplicar otro cambio de esquema. Utiliza la tabla pna.aeropuertos y el trigger de versiones ya existentes. Incluye todos los metadatos, el JSON original de cada fila, activo=true y fecha_desactivacion=NULL. El identificador interno y row_version los genera PostgreSQL; no se inventan ni se reutilizan identificadores.

Los IATA ya registrados se omiten, incluso si están inactivos. No se actualizan, reactivan ni eliminan datos existentes. Puede repetirse sin duplicar IATA. El número nuevo depende de los registros actuales de tu base; si exactamente 48 de estos códigos ya existen, insertará 8751 y omitirá 48.

No se inventaron IATA para las instalaciones sin código; esas filas no están incluidas. No se aplican filtros por país ni por servicio regular. Este archivo solo prepara SQL: **no se ejecutó contra ninguna base**.

Referencia del comportamiento de omisión: [PostgreSQL INSERT / ON CONFLICT](https://www.postgresql.org/docs/current/sql-insert.html).

Fuente: CSV de laboratorio preparado desde airports.csv (OurAirports), con las sustituciones ficticias indicadas. SHA-256 del CSV usado: ${hash}.

## SQL completo

\`\`\`sql
${sql}\`\`\`
`;
const file=root+'/docs/INSERTS_AEROPUERTOS_SUPABASE.md';
if(fs.existsSync(file))throw Error('El archivo ya existe; no se sobrescribe.');
fs.writeFileSync(file,markdown.replaceAll('\\docs/','docs/').replaceAll('\\Localidad','Localidad'),'utf8');
// Verificación offline: interpretar los literales de cada fila y recuperar exactamente sus 22 valores.
function decodeTuple(line){const inner=line.trim().slice(1,-1);const out=[];let value='',quoted=false;for(let i=0;i<inner.length;i++){const c=inner[i];if(c==="'"){value+=c;if(quoted&&inner[i+1]===c){value+=c;i++;}else quoted=!quoted;}else if(c===','&&!quoted){out.push(value.trim());value='';}else value+=c;}out.push(value.trim());if(quoted)throw Error('Literal SQL sin cerrar');return out;}
function decodeString(value){if(value.startsWith('NULL::'))return null;const s=value.replace(/::json$/,'');if(!s.startsWith("'")||!s.endsWith("'"))throw Error('Literal inesperado');return s.slice(1,-1).replaceAll("''","'");}
let verified=0;
for(let i=0;i<rows.length;i++){const r=decodeTuple(rows[i]);if(r.length!==columns.length)throw Error('No se recuperan las 22 columnas');if(decodeString(r[0])!==data[i].iata_code||decodeString(r[1])!==data[i].name.trim()||decodeString(r[2])!==optional(data[i].municipality)||JSON.stringify(JSON.parse(decodeString(r[19])))!==JSON.stringify(data[i]))throw Error('Pérdida de datos SQL');verified++;}
if(sql.includes('DELETE FROM')||sql.includes('UPDATE SET')||sql.includes('ALTER TABLE')||sql.includes('CREATE TABLE'))throw Error('Modificación inesperada');
console.log(JSON.stringify({archivo:file,registros:verified,columnas:columns.length,ciudadesFicticias:data.filter(a=>a.municipality.startsWith('Localidad ficticia ')).length,tamanoBytes:Buffer.byteLength(markdown),sinEjecucionSql:true}));
