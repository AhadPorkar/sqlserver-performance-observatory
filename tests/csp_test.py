"""Regression controls for the Chromium CSP probe, not an ASP.NET/SQL test.

A synthetic document with a real CSP meta policy is created in each context.
Positive AND negative controls prevent a permanently-true assertion from
appearing to prove security. CI still runs the separate native ASP.NET UI test.
"""
from __future__ import annotations

import html
import json
import os
from pathlib import Path
import unittest
from unittest.mock import Mock

from playwright.sync_api import sync_playwright
from csp_probe import (
    probe_string_compilation,
    require_csp_header,
    require_string_compilation_blocked,
)

ROOT = Path(__file__).resolve().parents[1]
RECORDS = []
BROWSER_VERSION = None


class CspBrowserControls(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        global BROWSER_VERSION
        cls.playwright = sync_playwright().start()
        cls.browser = cls.playwright.chromium.launch(
            headless=True, args=['--no-sandbox'],
            executable_path=os.environ.get('SPO_BROWSER_EXECUTABLE'),
        )
        BROWSER_VERSION = cls.browser.version

    @classmethod
    def tearDownClass(cls):
        cls.browser.close()
        cls.playwright.stop()

    def setUp(self):
        self.context = self.browser.new_context()
        self.page = self.context.new_page()

    def tearDown(self):
        self.context.close()

    def document(self, policy=None, extra=''):
        meta = '' if policy is None else (
            '<meta http-equiv="Content-Security-Policy" content="' +
            html.escape(policy, quote=True) + '">')
        self.page.set_content('<!doctype html><html><head>'+meta+
                              '</head><body><h1>Synthetic CSP control</h1>'+extra+'</body></html>')

    def observe(self):
        result = probe_string_compilation(self.page)
        RECORDS.append({'test': self._testMethodName, **result})
        return result

    def assert_allowed(self, result):
        for name in ('eval', 'functionConstructor'):
            self.assertEqual(result['results'][name], {'status': 'allowed', 'value': 4})
        with self.assertRaises(AssertionError):
            require_string_compilation_blocked(result)

    def test_strict_script_src_blocks_both_apis(self):
        self.document("default-src 'self'; script-src 'self'")
        require_string_compilation_blocked(self.observe())

    def test_default_src_fallback_blocks_both_apis(self):
        self.document("default-src 'self'")
        require_string_compilation_blocked(self.observe())

    def test_missing_policy_is_not_reported_as_blocking(self):
        self.document()
        self.assert_allowed(self.observe())

    def test_unsafe_eval_is_not_reported_as_blocking(self):
        self.document("script-src 'self' 'unsafe-eval'")
        self.assert_allowed(self.observe())

    def test_script_src_can_override_default_src(self):
        self.document("default-src 'none'; script-src 'self' 'unsafe-eval'")
        self.assert_allowed(self.observe())

    def test_strict_script_src_overrides_permissive_default(self):
        self.document("default-src 'self' 'unsafe-eval'; script-src 'self'")
        require_string_compilation_blocked(self.observe())

    def test_unsafe_inline_does_not_enable_eval(self):
        self.document("script-src 'self' 'unsafe-inline'")
        require_string_compilation_blocked(self.observe())

    def test_inspector_call_does_not_change_later_document_policy(self):
        self.document("script-src 'self'")
        # Diagnostic only: the inspector default may change across releases.
        # Do not make the old exemption's presence an enduring pass criterion.
        old = self.page.evaluate("() => {try {window.eval('2+2'); return false;} catch(e){return e instanceof EvalError;}}")
        result = self.observe()
        RECORDS[-1]['originalPageEvaluateReportedBlocked'] = old
        require_string_compilation_blocked(result)
        require_string_compilation_blocked(probe_string_compilation(self.page))

    def test_explicit_probe_agrees_with_native_click_handler(self):
        nonce = 'dGVzdC1vbmx5LWNzcC1jb250cm9s'
        self.document("script-src 'self' 'nonce-"+nonce+"'", '''
          <button id="native-probe" type="button">Run native event handler</button>
          <script nonce="'''+nonce+'''">
            document.getElementById('native-probe').addEventListener('click', event => {
              try {
                event.currentTarget.dataset.verdict = String(window.eval('2 + 2'));
              } catch (error) {
                event.currentTarget.dataset.verdict = error.name;
              }
            });
          </script>''')
        # Real input dispatch, not calling this handler via page.evaluate.
        self.page.locator('#native-probe').click()
        self.assertEqual(self.page.locator('#native-probe').get_attribute('data-verdict'), 'EvalError')
        require_string_compilation_blocked(self.observe())


class CspGuardControls(unittest.TestCase):
    def test_missing_or_irrelevant_header_rejected(self):
        for value in (None, '', '  ', "object-src 'none'", "script-src 'self' 'unsafe-eval'"):
            with self.subTest(value=value), self.assertRaises(AssertionError):
                require_csp_header(value)
        self.assertEqual(require_csp_header("script-src 'self'"), "script-src 'self'")

    def test_malformed_evidence_and_unexpected_errors_rejected(self):
        for evidence in ({}, {'allowUnsafeEvalBlockedByCSP': True},
                         {'allowUnsafeEvalBlockedByCSP': False, 'results': {}},
                         {'allowUnsafeEvalBlockedByCSP': False, 'results': {
                             'eval': {'status': 'blocked', 'errorName': 'SyntaxError'}}}):
            with self.subTest(evidence=evidence), self.assertRaises(AssertionError):
                require_string_compilation_blocked(evidence)

    def test_protocol_failure_cannot_count_as_blocked(self):
        for response in ({'exceptionDetails': {'text': 'test failure'}},
                         {'result': {'type': 'boolean', 'value': True}},
                         {'result': {'type': 'object', 'value': {}}}):
            with self.subTest(response=response):
                page = Mock()
                session = page.context.new_cdp_session.return_value
                session.send.return_value = response
                with self.assertRaises(AssertionError):
                    probe_string_compilation(page)
                session.detach.assert_called_once()
                self.assertIs(session.send.call_args.args[1]['allowUnsafeEvalBlockedByCSP'], False)


if __name__ == '__main__':
    suite = unittest.defaultTestLoader.loadTestsFromModule(__import__(__name__))
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    out = ROOT/'artifacts/csp-controls'
    out.mkdir(parents=True, exist_ok=True)
    report = {
        'kind': 'synthetic-document-csp-regression-controls',
        'browserVersion': BROWSER_VERSION,
        'testsRun': result.testsRun,
        'failures': len(result.failures),
        'errors': len(result.errors),
        'skipped': len(result.skipped),
        'passed': result.wasSuccessful(),
        'realAspNetHost': False,
        'nativeHttpNavigation': False,
        'sqlServerUsed': False,
        'observations': RECORDS,
    }
    (out/'report.json').write_text(json.dumps(report, indent=2)+'\n')
    raise SystemExit(0 if result.wasSuccessful() else 1)
