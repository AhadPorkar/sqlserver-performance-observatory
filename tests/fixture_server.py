"""UI fixture server, NOT the .NET application or a SQL Server substitute.
The Python arithmetic below only shapes known synthetic presentation data.
Actual analysis logic is tested separately by the C# tests.
"""
import argparse, json, mimetypes, pathlib, threading
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from datetime import datetime, timedelta
ROOT=pathlib.Path(__file__).resolve().parents[1]
CSP="default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; object-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'"
def summary(rows):
    count=sum(int(x['executions']) for x in rows)
    duration=sum(x['totalDurationMs'] for x in rows)
    return dict(executions=str(count),meanMs=duration/count if count else None,totalMs=duration,meanCpuMs=sum(x['totalCpuMs'] for x in rows)/count if count else None,meanReads=sum(x['totalLogicalReads'] for x in rows)/count if count else None,maxMs=max([x['maxDurationMs'] for x in rows],default=0),plans=sorted(set(x['planId'] for x in rows),key=int))
def analysis(s):
    end=datetime.fromisoformat(s['queryStore']['latestClosedEndUtc'].replace('Z','+00:00'));split=end-timedelta(hours=1);start=split-timedelta(hours=1)
    iso=lambda d:d.isoformat().replace('+00:00','Z')
    rows=[]
    for id in dict.fromkeys(b['queryId'] for b in s['buckets']):
        bs=[b for b in s['buckets'] if b['queryId']==id]
        before=summary([b for b in bs if b['endUtc']<=iso(split)])
        after=summary([b for b in bs if b['startUtc']>=iso(split)])
        enough=min(int(before['executions']),int(after['executions']))>=10
        ratio=after['meanMs']/before['meanMs'] if before['meanMs'] and before['meanMs']>=1 else None
        delta=after['meanMs']-before['meanMs'] if after['meanMs'] is not None and before['meanMs'] is not None else None
        changed=before['plans']!=after['plans']
        category='insufficient' if not enough else 'candidate' if ratio is not None and ratio>=2 and delta>=25 else 'stable'
        rows.append(dict(queryId=id,contextId=bs[0]['contextId'],queryHash=bs[0]['queryHash'],baseline=before,recent=after,ratio=ratio,deltaMs=delta,estimatedExtraMs=max(0,delta or 0)*int(after['executions']),planSetChanged=changed,classification=category,evidenceCodes=['PLAN_SET_CHANGED_CORRELATION_ONLY'] if changed else []))
    rows.sort(key=lambda q:(q['classification']!='candidate',-q['estimatedExtraMs']))
    return dict(baselineStartUtc=iso(start),recentStartUtc=iso(split),endUtc=iso(end),baselineMinutes=60,recentMinutes=60,queries=rows,warnings=['SYNTHETIC_FIXTURE_NOT_A_BENCHMARK','AGGREGATED_SUCCESSFUL_EXECUTIONS_ONLY_NO_PERCENTILES'],comparisonsEnabled=True)
def make_server(port=0):
    snapshot=json.loads((ROOT/'fixtures/replay.json').read_text())
    result=analysis(snapshot)
    class Handler(BaseHTTPRequestHandler):
        def log_message(self,*_):pass
        def send(self,status,data,kind='application/json',headers=None):
            if isinstance(data,(dict,list)):data=json.dumps(data,ensure_ascii=False).encode()
            if isinstance(data,str):data=data.encode()
            self.send_response(status);self.send_header('Content-Type',kind);self.send_header('Content-Security-Policy',CSP);self.send_header('Cache-Control','no-store');self.send_header('Content-Length',str(len(data)))
            for k,v in (headers or {}).items():self.send_header(k,v)
            self.end_headers();self.wfile.write(data)
        def do_GET(self):
            path=self.path.split('?')[0]
            if path=='/health/live':return self.send(200,{'status':'fixture-only'})
            if path=='/auth/csrf':return self.send(200,{'token':'fixture-csrf'})
            if path.startswith('/api/'):
                if 'spo.fixture=1' not in self.headers.get('Cookie',''):return self.send(401,{'code':'UNAUTHORIZED'})
                if path=='/api/targets':return self.send(200,{'mode':'Replay','targets':[dict(targetId='demo',displayName=snapshot['displayName'],mode='Replay',capturedUtc=snapshot['capturedUtc'])]})
                if path.endswith('/snapshot'):return self.send(200,snapshot)
                if path.endswith('/analysis'):return self.send(200,result)
                if path.endswith('/blocking'):return self.send(200,{'status':snapshot['blockingStatus'],'chains':[dict(sessionId=81,path=[81,63,55],terminal='root-observed',rootStatus='sleeping',rootOpenTransactions=1,waitMs='24800'),dict(sessionId=63,path=[63,55],terminal='root-observed',rootStatus='sleeping',rootOpenTransactions=1,waitMs='19100'),dict(sessionId=99,path=[99,-2],terminal='special-owner:-2',rootStatus='',rootOpenTransactions=None,waitMs='1900')]})
                if path.endswith('/evidence'):return self.send(200,{'kind':'ui-fixture-not-runtime-evidence','snapshot':snapshot,'analysis':result},headers={'Content-Disposition':'attachment; filename="fixture-evidence.json"'})
                return self.send(404,{})
            static=ROOT/'src/Observatory.Web/wwwroot';file=static/('index.html' if path=='/' else path.lstrip('/'))
            if static.resolve() not in file.resolve().parents or not file.is_file():return self.send(404,{})
            return self.send(200,file.read_bytes(),mimetypes.guess_type(str(file))[0] or 'application/octet-stream')
        def do_POST(self):
            if self.headers.get('X-CSRF-TOKEN')!='fixture-csrf':return self.send(400,{'code':'CSRF_REJECTED'})
            body=self.rfile.read(int(self.headers.get('Content-Length','0')))
            if self.path=='/auth/logout':return self.send(200,{'ok':True},headers={'Set-Cookie':'spo.fixture=; Max-Age=0; Path=/'})
            if self.path=='/auth/login':
                try:value=json.loads(body)['accessKey']
                except (ValueError,KeyError):value=''
                if value!='ui-fixture-key-not-valid-in-the-real-application':return self.send(401,{'code':'INVALID_ACCESS_KEY'})
                return self.send(200,{'ok':True},headers={'Set-Cookie':'spo.fixture=1; Path=/; HttpOnly; SameSite=Strict'})
            return self.send(404,{})
    server=ThreadingHTTPServer(('127.0.0.1',port),Handler)
    threading.Thread(target=server.serve_forever,daemon=True).start()
    return server
if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--port',type=int,default=5099);args=parser.parse_args();server=make_server(args.port)
    print('UI fixture server at http://127.0.0.1:'+str(server.server_port),flush=True)
    try:threading.Event().wait()
    except KeyboardInterrupt:server.shutdown()
