"""Real browser checks. CI uses --base-url against ASP.NET, not --fixture.
--route-transport is an explicit local sandbox workaround, never the CI default.
"""
import argparse,json,os,pathlib,sys,urllib.request,urllib.error,http.cookiejar,re
from playwright.sync_api import sync_playwright,expect
from fixture_server import make_server
ROOT=pathlib.Path(__file__).resolve().parents[1]
def main():
    parser=argparse.ArgumentParser();parser.add_argument('--base-url',default='http://localhost:5080');parser.add_argument('--fixture',action='store_true');parser.add_argument('--route-transport',action='store_true');parser.add_argument('--output',default='artifacts/ui');parser.add_argument('--dom-bridge',action='store_true');args=parser.parse_args()
    if args.dom_bridge and not args.fixture:parser.error('--dom-bridge is restricted to synthetic fixtures')
    out=ROOT/args.output;out.mkdir(parents=True,exist_ok=True)
    server=make_server() if args.fixture else None
    base=('http://127.0.0.1:'+str(server.server_port)) if server else args.base_url.rstrip('/')
    key='ui-fixture-key-not-valid-in-the-real-application' if server else os.environ['SPO_ACCESS_KEY']
    checks=[]
    def check(label):checks.append(label)
    with sync_playwright() as p:
        browser=p.chromium.launch(headless=True,args=['--no-sandbox'],executable_path=os.environ.get('SPO_BROWSER_EXECUTABLE'));ctx=browser.new_context(viewport={'width':1512,'height':1000},locale='de-DE')
        ctx.tracing.start(screenshots=True,snapshots=True,sources=True)
        if args.route_transport:
            def transport(route):
                req=route.request;url=req.url
                if not url.startswith(base+'/'):return route.abort()
                headers={k:v for k,v in req.all_headers().items() if k not in ('host','content-length','accept-encoding')}
                request=urllib.request.Request(url,data=req.post_data_buffer,headers=headers,method=req.method)
                try:response=urllib.request.urlopen(request,timeout=10)
                except urllib.error.HTTPError as e:response=e
                with response:
                    route.fulfill(status=response.status,headers={k:v for k,v in response.headers.items() if k.lower() not in ('transfer-encoding','connection')},body=response.read())
            ctx.route('**/*',transport)
        page=ctx.new_page();errors=[];page.on('pageerror',lambda e:errors.append(str(e)))
        if args.dom_bridge:
            # Explicit presentation-only sandbox harness. No native navigation,
            # browser cookie/origin/CSP, download or reload qualification is claimed.
            opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
            def bridge(url,method='GET',headers=None,body=None):
                if not url.startswith('/'):raise ValueError('Only local fixture paths allowed')
                request=urllib.request.Request(base+url,data=body.encode() if body else None,headers=headers or {},method=method)
                try:response=opener.open(request,timeout=10)
                except urllib.error.HTTPError as e:response=e
                with response:return {'status':response.status,'text':response.read().decode(),'headers':dict(response.headers)}
            page.expose_function('__fixtureRequest',bridge)
            static=ROOT/'src/Observatory.Web/wwwroot'
            markup=(static/'index.html').read_text();markup=re.sub(r'<script[^>]*>.*?</script>','',markup);markup=re.sub(r'<link[^>]*>','',markup)
            def load_dom():
                page.set_content(markup)
                page.add_style_tag(content=(static/'styles.css').read_text())
                page.evaluate("""() => {window.fetch=async (url,o={})=>{const r=await window.__fixtureRequest(String(url),o.method||'GET',o.headers||{},o.body||null);return {ok:r.status>=200&&r.status<300,status:r.status,json:async()=>JSON.parse(r.text),text:async()=>r.text};};}""")
                helpers=(static/'ui.mjs').read_text().replace('export function','function')
                app=re.sub(r"import .*?from './ui.mjs';",'',(static/'app.js').read_text(),count=1)
                page.add_script_tag(content='(async()=>{'+helpers+'\n'+app+'})();')
        else:
            load_dom=lambda:page.goto(base,wait_until='networkidle')
        try:
            load_dom()
            expect(page.locator('html')).to_have_attribute('lang','en');check('English default despite German browser')
            expect(page.locator('#loginView')).to_be_visible();check('Authentication screen')
            page.locator('#accessKey').fill('wrong-key');page.locator('#loginForm button[type=submit]').click();expect(page.locator('#loginError')).not_to_have_text('');check('Wrong key rejected')
            page.locator('#accessKey').fill(key);page.locator('#loginForm button[type=submit]').click();expect(page.locator('#workspace')).to_be_visible();expect(page.locator('.metric')).to_have_count(4);check('Login and overview')
            expect(page.locator('#accessKey')).to_have_value('');check('Password cleared after authentication')
            if not args.dom_bridge:
                assert 'unsafe-eval' not in (page.request.get(base+'/').headers.get('content-security-policy',''));check('CSP does not allow unsafe-eval')
            for language in ['en','de','fa']:
                page.locator('#workspace .language-picker').select_option(language)
                expect(page.locator('html')).to_have_attribute('lang',language);expect(page.locator('html')).to_have_attribute('dir','rtl' if language=='fa' else 'ltr');check('Language and direction: '+language)
                page.screenshot(path=str(out/f'overview-{language}.png'),full_page=True)
                for tab in ['queries','blocking','deadlocks','method','overview']:
                    page.locator(f'[data-page="{tab}"]').click();expect(page.locator(f'[data-page="{tab}"]')).to_have_attribute('aria-current','page');expect(page.locator('#content')).not_to_be_empty();check(language+' page '+tab)
            page.locator('#workspace .language-picker').select_option('en');expect(page.locator('html')).to_have_attribute('lang','en')
            page.locator('[data-page=queries]').click();page.locator('.query-button').first.click();expect(page.locator('#queryDialog')).to_be_visible();check('Query detail dialog opens')
            page.screenshot(path=str(out/'query-detail-en.png'),full_page=True);page.keyboard.press('Escape');expect(page.locator('#queryDialog')).not_to_be_visible();check('Dialog closes with keyboard')
            page.locator('[data-page=blocking]').click();expect(page.locator('.chain')).to_have_count(3);page.screenshot(path=str(out/'blocking-en.png'),full_page=True);check('Synthetic blocking chains rendered')
            page.locator('[data-page=deadlocks]').click();expect(page.locator('.resource-row')).to_have_count(2);page.screenshot(path=str(out/'deadlocks-en.png'),full_page=True);check('Normalized deadlock resources rendered')
            if not args.dom_bridge:
                with page.expect_download() as download:page.locator('#export').click()
                path=out/'export.json';download.value.save_as(path);data=json.loads(path.read_text());assert data['snapshot']['mode']=='Replay';check('Evidence downloads and preserves mode')
            page.locator('[data-page=overview]').click()
            for language in ['de','fa']:
                page.locator('#workspace .language-picker').select_option(language);expect(page.locator('html')).to_have_attribute('lang',language)
                page.set_viewport_size({'width':390,'height':844});page.screenshot(path=str(out/f'mobile-{language}.png'),full_page=True)
                assert page.locator('body').bounding_box()['width']<=391;check('Mobile page width '+language)
                page.set_viewport_size({'width':1512,'height':1000})
            if not args.dom_bridge:
                page.reload(wait_until='networkidle');expect(page.locator('html')).to_have_attribute('lang','fa');expect(page.locator('#workspace')).to_be_visible();check('Language and session survive reload')
            # An actual eval request should remain blocked. Do not enable bypass_csp.
            if not args.dom_bridge:
                blocked=page.evaluate("() => {try {window.eval('2+2'); return false;} catch(e){return e instanceof EvalError;}}")
                assert blocked;check('Eval is genuinely blocked by the page CSP')
            page.locator('#logout').click();expect(page.locator('#loginView')).to_be_visible();check('Logout returns to login')
            assert not errors,errors;check('No page JavaScript errors')
        except Exception:
            page.screenshot(path=str(out/'failure.png'),full_page=True);raise
        finally:
            ctx.tracing.stop(path=str(out/'trace.zip'));browser.close()
            if server:server.shutdown()
    report={'mode':'synthetic-fixture' if args.fixture else 'real-aspnet-replay','transport':'isolated-dom-http-bridge' if args.dom_bridge else 'explicit-http-route' if args.route_transport else 'native-browser-navigation','nativeCookieCspDownloadReloadChecksExecuted':not args.dom_bridge,'checks':checks,'passed':len(checks),'sqlServerUsed':False}
    (out/'report.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
if __name__=='__main__':main()
