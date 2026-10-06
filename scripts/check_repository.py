"""Source/packaging checks, not a C# compiler, runtime or SQL integration test."""
import json,pathlib,re,sys,xml.etree.ElementTree as ET
ROOT=pathlib.Path(__file__).resolve().parents[1]
checks=[]
def check(name,condition):
    if not condition:raise AssertionError(name)
    checks.append(name)
def main():
    for path in ROOT.glob('**/*.csproj'):
        if 'obj' in path.parts:continue
        xml=ET.parse(path)
        for item in xml.findall('.//ProjectReference'):
            check('project ref '+str(path.relative_to(ROOT))+' '+item.attrib['Include'],(path.parent/item.attrib['Include']).is_file())
        check('valid project XML '+str(path.relative_to(ROOT)),True)
    ET.parse(ROOT/'Directory.Build.props');ET.parse(ROOT/'Observatory.slnx');check('solution and shared props XML',True)
    for path in ROOT.glob('**/*.json'):
        if any(p in path.parts for p in ['artifacts','state','bin','obj']):continue
        json.loads(path.read_text());check('JSON '+str(path.relative_to(ROOT)),True)
    locales=ROOT/'src/Observatory.Web/wwwroot/locales'
    en=json.loads((locales/'en.json').read_text())
    for language in ['de','fa']:
        d=json.loads((locales/(language+'.json')).read_text());check('translation parity '+language,set(d)==set(en) and all(isinstance(v,str) and v.strip() for v in d.values()))
    scripts=list((ROOT/'src/Observatory.SqlServer/Queries').glob('*.sql'))
    for path in scripts:
        sql=re.sub(r'--[^\n]*','',path.read_text())
        check('read-only source SQL '+path.name,not re.search(r'\b(INSERT|UPDATE|DELETE|MERGE|CREATE|ALTER|DROP|TRUNCATE|KILL|DBCC|EXEC)\b',sql,re.I))
        check('no raw text/plan retrieval '+path.name,not re.search(r'query_sql_text|sql_text\(|query_plan\b|input_buffer',sql,re.I))
    query=(ROOT/'src/Observatory.SqlServer/Queries/QueryBuckets.sql').read_text()
    check('weighted averages in SQL','rs.count_executions) * rs.avg_duration' in query)
    check('closed intervals only','i.end_time <= @ToUtc' in query)
    check('execution-type filtering','rs.execution_type = 0' in query)
    code=(ROOT/'src/Observatory.Web/Program.cs').read_text()
    check('authentication required for whole API group','MapGroup("/api").RequireAuthorization()' in code)
    check('CSP no unsafe-eval or unsafe-inline','unsafe-eval' not in code and 'unsafe-inline' not in code)
    check('origin allowlist enforced','suppliedOrigin != origin' in code)
    check('constant-time key comparison','CryptographicOperations.FixedTimeEquals' in code)
    check('browser API has no arbitrary SQL or execution routes',not re.search(r'api\.Map(Post|Put|Delete|Patch)',code))
    check('lab cannot be accidentally referenced by Web','Observatory.Lab' not in (ROOT/'src/Observatory.Web/Observatory.Web.csproj').read_text())
    check('lab guarded by Developer edition','Developer' in (ROOT/'tools/Observatory.Lab/LabEnvironment.cs').read_text())
    check('lab requires explicit acceptance','--accept-lab-only' in (ROOT/'tools/Observatory.Lab/Program.cs').read_text())
    check('bigint-safe serialization','Int64StringConverter' in (ROOT/'src/Observatory.Core/JsonDefaults.cs').read_text())
    parser=(ROOT/'src/Observatory.Core/DeadlockParser.cs').read_text();check('XML DTD disabled','DtdProcessing.Prohibit' in parser and 'XmlResolver = null' in parser)
    # Check local Markdown references. External links deliberately aren't fetched here.
    count=0
    for path in ROOT.rglob('*.md'):
        if 'artifacts' in path.parts:continue
        for match in re.finditer(r'!?\[[^\]]*\]\(([^)\s]+)(?:\s+"[^"]*")?\)',path.read_text()):
            link=match.group(1).split('#')[0]
            if not link or re.match(r'[a-z]+:',link):continue
            count+=1
            if not (path.parent/link).exists():raise AssertionError(f'Broken link {path.relative_to(ROOT)}: {link}')
    check(f'{count} local Markdown links',True)
    # No pasted customer credentials, generated logs or local binary build claims.
    check('no committed environment file',not (ROOT/'.env').exists())
    report={'kind':'source-structure-only','csharpCompilation':False,'sqlExecution':False,'passed':len(checks),'checks':checks}
    print(json.dumps(report,indent=2));return report
if __name__=='__main__':
    try:main()
    except (AssertionError,ValueError,ET.ParseError) as ex:print('FAIL:',ex,file=sys.stderr);sys.exit(1)
