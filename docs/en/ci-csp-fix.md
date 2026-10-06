# CI correction: test the document CSP, not the inspector exemption

[Project](../../README.md) | [Deutsch](../de/ci-csp-fix.md) | [Verification](testing.md)

## Diagnosis from the uploaded logs
The supplied archive `logs_101358504729.zip` belongs to
`AhadPorkar/sqlserver-performance-observatory`, commit
`60dc9a465d326ccc8d0835187e305f80c8af2a83`, dated 6 October 2026.
It is not a transactional-messaging-lab log.

Both .NET jobs built and published successfully. Each ran **51 xUnit cases:
51 passed, zero failed, zero skipped**, on Ubuntu 24.04 and Windows 2022.
The real ASP.NET process started and **20 API smoke checks passed**.
The source/frontend/support job passed. The native browser job failed at
`tests/ui_test.py:88`, `assert blocked`, after evaluating `window.eval('2+2')`
through Playwright's inspector execution path.

See the [sanitized log summary](../qa/ci-log-summary-2026-10-06.json).
Raw logs, machine tokens and environment secrets are not included in this source
update. Existing xUnit and action-runtime warnings were non-fatal; they are not
silenced by this patch.

## Why the original assertion was wrong
`page.evaluate` is not a faithful way to determine whether page-originated
string compilation is denied by CSP in Chromium. Inspector execution can
permit it. The [Chrome DevTools Protocol definition](https://raw.githubusercontent.com/ChromeDevTools/devtools-protocol/master/json/js_protocol.json)
provides `Runtime.evaluate.allowUnsafeEvalBlockedByCSP`; its default is true.
The [V8 inspector implementation](https://chromium.googlesource.com/v8/v8/+/1664d8a3e546da06918f9e47463bfd4fd9e5f2be/src/inspector/v8-runtime-agent-impl.cc)
also documents temporarily allowing string compilation for inspector calls.
A false `blocked` value from the old test therefore did not establish that the
application's CSP was absent or ineffective.

## What changed
- `tests/csp_probe.py` calls `Runtime.evaluate` on the actual page with
  **`allowUnsafeEvalBlockedByCSP: false`**. It checks `eval` and the Function
  constructor. Only a specific `EvalError` qualifies as blocking; malformed
  protocol responses and unrelated errors fail the test.
- The UI test checks the enforcing CSP header from the **actual navigation
  response**, rather than a separate HTTP request. Missing headers fail closed.
  It saves `csp-probe.json` and a partial report even when a later check fails.
- Twelve controls exercise strict and permissive synthetic policies, missing
  policy, directive precedence, malformed evidence and protocol failures. A real
  browser click invokes a nonce-authorized page handler as an independent check
  of the explicit CDP result. The nonce exists only in that isolated fixture.
- CI runs these controls, then retains the normal API/native UI test against the
  actual ASP.NET process. There is no automatic fixture fallback.

No application C#, SQL, production CSP, authentication, package versions,
Docker configuration or runtime permissions changed. No assertions were skipped;
no context-wide `bypass_csp` was enabled. This helper is Chromium-specific because
it uses CDP. A different browser needs its own correctly scoped probe.

## Local verification versus new CI acceptance
The [regression report](../qa/csp-regression-controls.json) records **12 passing
controls** on Chromium 144.0.7559.96 / Playwright 1.57.0. The original assertion
reported `false` under a strict synthetic CSP, while the corrected probe and the
native-click control reported blocking. Missing policy and an explicit
`unsafe-eval` policy were correctly rejected as security evidence.

The update environment also passed 14 frontend tests, 6 Python tests and 31
presentation-only UI checks. The latter use the existing explicitly isolated
DOM/HTTP fixture bridge, not ASP.NET. Localhost navigation is blocked by the
sandbox. The .NET SDK, SQL Server and Docker are not available here; **the
updated complete native ASP.NET workflow has not been rerun in this environment**.
The successful .NET/API results above are evidence from the uploaded original CI
logs, not a new run. The source update still requires a fresh CI execution.

```bash
pip install -r tests/requirements.txt
python -m playwright install chromium
python tests/csp_test.py
# With the real ASP.NET Replay host started and SPO_ACCESS_KEY set:
python tests/ui_test.py --base-url http://localhost:5080
```

Do not rerun only the old commit and expect new code to execute: commit and push
the patch, then inspect the new Verify run. A green Replay run still says nothing
about live Query Store/DMV/XEL permissions or real SQL workload performance.
