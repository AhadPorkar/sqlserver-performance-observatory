import sys,time,urllib.request,urllib.error
url=sys.argv[1]
for attempt in range(60):
    try:
        with urllib.request.urlopen(url,timeout=2) as r:
            if r.status==200:print("HTTP service ready");sys.exit(0)
    except (urllib.error.URLError,TimeoutError):pass
    time.sleep(1)
raise SystemExit("HTTP service did not become ready in 60 seconds")
