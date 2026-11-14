import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
const root='C:/Users/Josue/Desktop/Aplicaciones Ditribuidas/Proyecto/Vuelos_Backend/Vuelos_Backend';
const originalHash=crypto.createHash('sha256').update(fs.readFileSync(path.join(root,'docs/airports.csv'))).digest('hex');
export function csv(text){
  const rows=[];let row=[],value='',quoted=false;
  for(let i=0;i<text.length;i++){
    const c=text[i];
    if(c==='"'){if(quoted&&text[i+1]===c){value+=c;i++;}else quoted=!quoted;}
    else if(c===','&&!quoted){row.push(value);value='';}
    else if(c==='\n'&&!quoted){row.push(value.replace(/\r$/,''));rows.push(row);row=[];value='';}
    else value+=c;
  }
  if(quoted)throw new Error('CSV con comillas sin cerrar');
  if(row.length||value){row.push(value);rows.push(row);}
  return rows;
}
const rows=csv(fs.readFileSync(path.join(root,'docs/airports.csv'),'utf8'));
const headers=rows.shift();const original=rows.map(row=>Object.fromEntries(headers.map((h,i)=>[h,row[i]??''])));
const standard=csv(fs.readFileSync(path.join(root,'obj/openflights-referencias.dat'),'utf8'));
const extended=csv(fs.readFileSync(path.join(root,'obj/openflights-extended-referencias.dat'),'utf8'));
const references=[...standard,...extended.filter(a=>!standard.some(b=>a[0]===b[0]))];
const missing=original.filter(a=>['small_airport','medium_airport','large_airport'].includes(a.type)&&/^[A-Z]{3}$/.test(a.iata_code)&&!a.municipality.trim());
function km(a,b){const rad=x=>x*Math.PI/180,lat=rad(+b[6]- +a.latitude_deg),lon=rad(+b[7]- +a.longitude_deg);const t=Math.sin(lat/2)**2+Math.cos(rad(+a.latitude_deg))*Math.cos(rad(+b[6]))*Math.sin(lon/2)**2;return 6371*2*Math.atan2(Math.sqrt(t),Math.sqrt(1-t));}
const english=new Intl.DisplayNames(['en'],{type:'region'});
const aliases={CG:'Congo (Brazzaville)',CD:'Congo (Kinshasa)',IR:'Iran',RU:'Russia',LA:'Laos',VE:'Venezuela',CI:'Cote d\'Ivoire'};
const normalize=x=>x.normalize('NFD').replace(/\p{M}/gu,'').toLowerCase().replace(/[^a-z0-9]/g,'');
const matches=[],pending=[];
for(const a of missing){
 const candidates=references.filter(b=>b[4]===a.iata_code&&b[2]&&b[2]!=='\\N'&&b[2].length<=100&&['airport','unknown'].includes(b[12])&&
  [english.of(a.iso_country),aliases[a.iso_country]].filter(Boolean).some(x=>normalize(x)===normalize(b[3]))&&km(a,b)<=2);
 const cities=[...new Set(candidates.map(b=>b[2]))];
 if(cities.length===1){const b=candidates[0];const codes=[a.ident,a.icao_code,a.gps_code].filter(Boolean);
  const sameIcao=b[5]!=='\\N'&&codes.includes(b[5]);
  // Dos identificadores + coordenadas, o nombre idéntico y coordenadas del mismo aeropuerto.
  const sameName=normalize(a.name)===normalize(b[1]);
  if(sameIcao||sameName){matches.push({id:a.id,iata:a.iata_code,ident:a.ident,pais:a.iso_country,nombre:a.name,ciudad:b[2],fuente:'https://raw.githubusercontent.com/jpatokal/openflights/master/data/'+(standard.some(x=>x[0]===b[0])?'airports.dat':'airports-extended.dat'),idFuente:b[0],nombreFuente:b[1],icaoFuente:b[5],distanciaKm:Math.round(km(a,b)*1000)/1000,criterio:sameIcao?'IATA+ICAO+pais+coordenadas':'IATA+nombre+pais+coordenadas'});continue;}
 }
 pending.push(a);
}
fs.writeFileSync(path.join(root,'obj/airports-coincidencias.json'),JSON.stringify(matches,null,2));
fs.writeFileSync(path.join(root,'obj/airports-pendientes-restantes.json'),JSON.stringify(pending,null,2));
if(process.argv.includes('--export')){
 const req=createRequire('C:/Users/Josue/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/package.json');
 const {Workbook}=await import(pathToFileURL(req.resolve('@oai/artifact-tool')).href);
 const workbook=Workbook.create(),sheet=workbook.worksheets.add('Ciudades recuperadas');
 const reportHeaders=Object.keys(matches[0]);
 sheet.getRange('A1').write([reportHeaders,...matches.map(a=>reportHeaders.map(h=>a[h]))]);
 const checkedReport=sheet.getRange(`A1:L${matches.length+1}`).values;
 // El CSV final preserva todas las columnas originales; la trazabilidad se entrega por separado.
 const report=checkedReport.slice(1).map(r=>Object.fromEntries(reportHeaders.map((h,i)=>[h,r[i]])));
 const byId=new Map(report.map(a=>[String(a.id),a]));
 const completed=original.map(a=>byId.has(a.id)?{...a,municipality:byId.get(a.id).ciudad}:a);
 const serialize=(h,items)=>[h,...items.map(a=>h.map(k=>a[k]??''))].map(r=>r.map(v=>{
   const s=String(v);return /[",\r\n]/.test(s)?'"'+s.replaceAll('"','""')+'"':s;
 })).join('\r\n')+'\r\n';
 const valid=completed.filter(a=>['small_airport','medium_airport','large_airport'].includes(a.type)&&/^[A-Z]{3}$/.test(a.iata_code)&&a.municipality.trim());
 const files={
  'airports_completado.csv':serialize(headers,completed),
  'airports_importables.csv':serialize(headers,valid),
  'airports_ciudades_completadas.csv':serialize(reportHeaders,report),
  'airports_ciudades_pendientes.csv':serialize([...headers,'motivo'],pending.map(a=>({...a,motivo:'Sin coincidencia confiable en las fuentes consultadas; no se infirió la ciudad.'})))
 };
 const target=path.join(root,'docs/airports_preparado');fs.mkdirSync(target,{recursive:true});
 for(const [name,text] of Object.entries(files)){if(fs.existsSync(path.join(target,name)))throw new Error('No se sobrescribe archivo existente: '+name);fs.writeFileSync(path.join(target,name),text,'utf8');}
 const roundtrip=csv(fs.readFileSync(path.join(target,'airports_completado.csv'),'utf8'));roundtrip.shift();
 if(roundtrip.length!==original.length)throw new Error('Cambió el total de filas');
 let changes=0;
 for(let i=0;i<rows.length;i++)for(let j=0;j<headers.length;j++)if(rows[i][j]!==roundtrip[i][j]){
  if(headers[j]!=='municipality'||rows[i][j].trim()||!roundtrip[i][j].trim())throw new Error('Cambió un dato no autorizado');changes++;
 }
 if(changes!==matches.length)throw new Error('Conteo de modificaciones incorrecto');
 if(new Set(valid.map(a=>a.iata_code)).size!==valid.length)throw new Error('IATA duplicado en salida');
 if(crypto.createHash('sha256').update(fs.readFileSync(path.join(root,'docs/airports.csv'))).digest('hex')!==originalHash)throw new Error('Cambió el archivo original');
 const result={registrosOrigen:original.length,municipiosCompletados:matches.length,pendientes:pending.length,
  importablesSinFiltroServicio:valid.length,importablesSoloServicioRegular:valid.filter(a=>a.scheduled_service==='yes').length,
  originalSha256:originalHash,fechaConsulta:'2026-11-13',archivos:Object.keys(files)};
 fs.writeFileSync(path.join(target,'resumen.json'),JSON.stringify(result,null,2)+'\n');
 console.log(JSON.stringify(result,null,2));
}else console.log(JSON.stringify({original:original.length,sinCiudad:missing.length,completables:matches.length,pendientes:pending.length}));
