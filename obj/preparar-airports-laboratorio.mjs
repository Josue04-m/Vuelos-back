import fs from 'node:fs';
import crypto from 'node:crypto';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
const root='C:/Users/Josue/Desktop/Aplicaciones Ditribuidas/Proyecto/Vuelos_Backend/Vuelos_Backend';
const source=root+'/docs/airports.csv';
const input=fs.readFileSync(source,'utf8');
const hash=crypto.createHash('sha256').update(input).digest('hex');
function parse(text){const rows=[];let row=[],s='',quoted=false;for(let i=0;i<text.length;i++){const c=text[i];if(c==='"'){if(quoted&&text[i+1]===c){s+=c;i++;}else quoted=!quoted;}else if(c===','&&!quoted){row.push(s);s='';}else if(c==='\n'&&!quoted){row.push(s.replace(/\r$/,''));rows.push(row);row=[];s='';}else s+=c;}if(row.length||s){row.push(s);rows.push(row);}if(quoted)throw Error('CSV mal formado');return rows;}
function stringify(rows){return rows.map(r=>r.map(v=>{const s=String(v??'');return /[",\r\n]/.test(s)?'"'+s.replaceAll('"','""')+'"':s;}).join(',')).join('\r\n')+'\r\n';}
const parsed=parse(input),headers=parsed.shift();
const original=parsed.map(r=>Object.fromEntries(headers.map((h,i)=>[h,r[i]??''])));
const admitted=a=>['small_airport','medium_airport','large_airport'].includes(a.type)&&/^[A-Z]{3}$/.test(a.iata_code);
const changes=[];
const completed=original.map(a=>{if(!admitted(a)||a.municipality.trim()&&a.municipality!=='\\N')return {...a};
 const ciudad='Localidad ficticia '+a.iata_code;
 changes.push([a.id,a.ident,a.iata_code,a.iso_country,a.name,a.municipality,ciudad,'FICTICIO: dato exclusivo del laboratorio, no corresponde a una ciudad verificada.']);
 return {...a,municipality:ciudad};});
const req=createRequire('C:/Users/Josue/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/package.json');
const {Workbook}=await import(pathToFileURL(req.resolve('@oai/artifact-tool')).href);
const workbook=Workbook.create(),sheet=workbook.worksheets.add('Datos ficticios');
const reportHeaders=['id','ident','iata_code','iso_country','name','municipality_original','municipality_laboratorio','observacion'];
sheet.getRange('A1').write([reportHeaders,...changes]);
const report=sheet.getRange(`A1:H${changes.length+1}`).values;
const valid=completed.filter(admitted);
if(new Set(valid.map(a=>a.iata_code)).size!==valid.length)throw Error('IATA duplicado');
if(valid.some(a=>!a.municipality.trim()||a.municipality.length>100))throw Error('Ciudad no válida');
const outputs={
 'airports_laboratorio.csv':stringify([headers,...valid.map(a=>headers.map(h=>a[h]))]),
 'airports_completo_laboratorio.csv':stringify([headers,...completed.map(a=>headers.map(h=>a[h]))]),
 'airports_datos_ficticios.csv':stringify(report)
};
const target=root+'/docs/airports_laboratorio';fs.mkdirSync(target,{recursive:true});
for(const [name,data] of Object.entries(outputs)){if(fs.existsSync(target+'/'+name))throw Error('No se sobrescriben archivos existentes: '+name);fs.writeFileSync(target+'/'+name,data,'utf8');}
const actual=parse(fs.readFileSync(target+'/airports_completo_laboratorio.csv','utf8'));actual.shift();let count=0;
for(let r=0;r<parsed.length;r++)for(let c=0;c<headers.length;c++)if(parsed[r][c]!==actual[r][c]){
 if(headers[c]!=='municipality'||parsed[r][c].trim()&&parsed[r][c]!=='\\N')throw Error('Cambio no autorizado');count++;}
if(count!==changes.length||actual.length!==original.length)throw Error('No coinciden cambios/filas');
if(crypto.createHash('sha256').update(fs.readFileSync(source,'utf8')).digest('hex')!==hash)throw Error('Se alteró original');
console.log(JSON.stringify({original:original.length,importables:valid.length,ciudadesFicticias:changes.length,soloServicioRegular:valid.filter(a=>a.scheduled_service==='yes').length,originalConservado:true,archivos:Object.keys(outputs)}));
