export function number(value, language='en', digits=1) {
  if (value==null || value==='') return '\u2014';
  if (typeof value==='string' && /^-?\d+$/.test(value)) return new Intl.NumberFormat(language).format(BigInt(value));
  const n=Number(value); return Number.isFinite(n) ? new Intl.NumberFormat(language,{maximumFractionDigits:digits}).format(n) : '\u2014';
}
export function instant(value,language='en') {
  if(!value)return '\u2014'; const date=new Date(value);
  return Number.isNaN(date.valueOf())?'\u2014':new Intl.DateTimeFormat(language,{dateStyle:'medium',timeStyle:'short',timeZone:'UTC'}).format(date)+' UTC';
}
export function safeLanguage(value){return ['en','de','fa'].includes(value)?value:'en';}
export function isStale(status,now,seconds){const last=Date.parse(status?.lastSuccessUtc);return !status||status.state!=='ok'||!Number.isFinite(last)||!Number.isFinite(now)||(now-last)/1000>seconds;}
export function sumExecutions(queries){return queries.reduce((n,q)=>n+BigInt(q.recent.executions),0n).toString();}
export function compareIds(a,b){return BigInt(a)<BigInt(b)?-1:BigInt(a)>BigInt(b)?1:0;}
