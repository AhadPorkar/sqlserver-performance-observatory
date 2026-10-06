"""Run against the real ASP.NET process. Never uses the fixture server."""
import http.cookiejar,json,os,urllib.request,urllib.error,pathlib
BASE=os.environ.get('SPO_BASE_URL','http://localhost:5080').rstrip('/')
KEY=os.environ['SPO_ACCESS_KEY']
checks=[]
opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
def request(path,method='GET',body=None,headers=None):
    data=json.dumps(body).encode() if body is not None else None
    h={'Content-Type':'application/json',**(headers or {})}
    req=urllib.request.Request(BASE+path,data=data,headers=h,method=method)
    try:r=opener.open(req,timeout=20)
    except urllib.error.HTTPError as e:r=e
    with r:return r.status,dict(r.headers),r.read()
def require(label,truth):
    if not truth:raise AssertionError(label)
    checks.append(label);print('PASS',label)
status,h,body=request('/health/live');require('liveness reachable',status==200)
require('CSP present and no unsafe-eval',"script-src 'self'" in h.get('Content-Security-Policy','') and 'unsafe-eval' not in h.get('Content-Security-Policy',''))
require('API rejects anonymous',request('/api/targets')[0]==401)
require('logout rejects anonymous',request('/auth/logout','POST',{}, {'Origin':BASE})[0]==401)
require('foreign origin rejected',request('/auth/login','POST',{'accessKey':KEY},{'Origin':'https://untrusted.invalid'})[0]==403)
require('missing antiforgery token rejected',request('/auth/login','POST',{'accessKey':KEY},{'Origin':BASE})[0]==400)
_,_,b=request('/auth/csrf');token=json.loads(b)['token'];headers={'Origin':BASE,'X-CSRF-TOKEN':token}
require('incorrect key rejected',request('/auth/login','POST',{'accessKey':'wrong'},headers)[0]==401)
status,h,_=request('/auth/login','POST',{'accessKey':KEY},headers);require('valid login accepted',status==200)
require('cookie is HttpOnly and SameSite strict','httponly' in h.get('Set-Cookie','').lower() and 'samesite=strict' in h.get('Set-Cookie','').lower())
status,h,b=request('/api/targets');targets=json.loads(b)['targets'];require('authenticated target list',status==200 and len(targets)==1)
id=targets[0]['targetId'];prefix='/api/targets/'+id
status,h,b=request(prefix+'/snapshot');s=json.loads(b);require('replay explicitly labeled',status==200 and s['mode']=='Replay')
require('bigint IDs are strings',all(isinstance(x['queryId'],str) for x in s['buckets']))
require('API data is not cached',h.get('Cache-Control')=='no-store')
status,_,b=request(prefix+'/analysis');a=json.loads(b);require('real C# analysis finds two synthetic candidates',status==200 and sum(x['classification']=='candidate' for x in a['queries'])==2)
require('invalid windows rejected',request(prefix+'/analysis?baselineMinutes=-1')[0]==400)
status,_,b=request(prefix+'/blocking');require('real C# blocking parser responds',status==200 and len(json.loads(b)['chains'])==3)
status,_,b=request(prefix+'/evidence');e=json.loads(b);require('unsigned export has explicit kind',status==200 and e['kind']=='diagnostic-evidence-unsigned')
require('export does not contain sentinel secrets',not any(x in b.decode() for x in ['SENSITIVE_SQL_SENTINEL','SENSITIVE_LOGIN_SENTINEL',KEY]))
_,_,b=request('/auth/csrf');token=json.loads(b)['token']
require('logout with fresh token accepted',request('/auth/logout','POST',{}, {'Origin':BASE,'X-CSRF-TOKEN':token})[0]==200)
require('API rejects logged-out client',request('/api/targets')[0]==401)
out=pathlib.Path('artifacts/api-smoke.json');out.parent.mkdir(exist_ok=True)
out.write_text(json.dumps({'runtime':'real-aspnet','mode':'Replay','sqlServerUsed':False,'passed':len(checks),'checks':checks},indent=2)+'\n')
