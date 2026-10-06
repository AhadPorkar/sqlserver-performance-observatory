"""Chromium-only CSP regression probe; this is test code, never an app endpoint.

Playwright's page.evaluate uses the inspector execution path, which can allow
string compilation even when the document CSP forbids it. Runtime.evaluate has
an explicit opt-out. Keep allowUnsafeEvalBlockedByCSP=False: deleting it changes
what the test measures. No context-wide CSP bypass is used.
"""
from __future__ import annotations

from typing import Any
from playwright.sync_api import Page

# Constant harmless expressions only. Unexpected errors must not count as CSP
# enforcement. Probe both string-to-code APIs; do not alter any page built-ins.
EXPRESSION = """(() => {
  const attempt = (action) => {
    try {
      return {status: 'allowed', value: action()};
    } catch (error) {
      return {
        status: error instanceof EvalError ? 'blocked' : 'unexpected-error',
        errorName: error.name
      };
    }
  };
  return {
    eval: attempt(() => globalThis.eval('2 + 2')),
    functionConstructor: attempt(() => new Function('return 2 + 2;')())
  };
})()"""


def require_csp_header(value: str | None) -> str:
    """Guard against a missing header passing a negative substring check.

    This is intentionally not a complete CSP parser. Runtime enforcement is
    tested separately against the actual loaded document.
    """
    if not isinstance(value, str) or not value.strip():
        raise AssertionError('The document has no enforcing Content-Security-Policy header')
    names = {part.strip().split()[0].lower() for part in value.split(';') if part.strip()}
    if not names.intersection({'script-src', 'default-src'}):
        raise AssertionError('The CSP header does not restrict script execution')
    if "'unsafe-eval'" in value.lower():
        raise AssertionError('The CSP header explicitly allows unsafe-eval')
    return value


def probe_string_compilation(page: Page) -> dict[str, Any]:
    """Evaluate under the document CSP instead of the inspector exemption.

    A missing/invalid response is a test failure, never evidence of blocking.
    The CDP session is attached to this page, not an unrelated frame.
    """
    session = page.context.new_cdp_session(page)
    try:
        response = session.send('Runtime.evaluate', {
            'expression': EXPRESSION,
            'returnByValue': True,
            'allowUnsafeEvalBlockedByCSP': False,
            'timeout': 5000,
        })
    finally:
        session.detach()
    if response.get('exceptionDetails'):
        raise AssertionError('CSP probe itself failed; this is not evidence of CSP enforcement')
    result = response.get('result', {})
    value = result.get('value')
    if result.get('type') != 'object' or not isinstance(value, dict):
        raise AssertionError('CSP probe did not return a structured result')
    for name in ('eval', 'functionConstructor'):
        if not isinstance(value.get(name), dict):
            raise AssertionError(f'CSP probe response is missing {name}')
    return {
        'mechanism': 'Chromium Runtime.evaluate with inspector CSP exemption disabled',
        'allowUnsafeEvalBlockedByCSP': False,
        'results': value,
    }


def require_string_compilation_blocked(evidence: dict[str, Any]) -> None:
    """Fail closed on allowed compilation, a malformed result or another error."""
    if evidence.get('allowUnsafeEvalBlockedByCSP') is not False:
        raise AssertionError('CSP inspector exemption was not explicitly disabled')
    results = evidence.get('results', {})
    for name in ('eval', 'functionConstructor'):
        item = results.get(name, {})
        if item.get('status') != 'blocked' or item.get('errorName') != 'EvalError':
            raise AssertionError(f'{name} was not blocked by the document CSP: {item}')
