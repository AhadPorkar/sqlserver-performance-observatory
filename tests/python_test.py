"""Tests for the fixture contracts and packaging checker only, not .NET logic."""
import json,pathlib,unittest,sys,importlib.util
ROOT=pathlib.Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('fixture_server',ROOT/'tests/fixture_server.py');module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
class FixtureContract(unittest.TestCase):
    def setUp(self):self.s=json.loads((ROOT/'fixtures/replay.json').read_text())
    def test_replay_label(self):self.assertEqual(self.s['mode'],'Replay')
    def test_fixture_candidates(self):self.assertEqual(sum(q['classification']=='candidate' for q in module.analysis(self.s)['queries']),2)
    def test_fixture_provenance_warning(self):self.assertIn('SYNTHETIC_FIXTURE_NOT_A_BENCHMARK',module.analysis(self.s)['warnings'])
    def test_large_id_preserved(self):self.assertIn('9007199254740993',[x['queryId'] for x in module.analysis(self.s)['queries']])
    def test_known_weighting(self):self.assertEqual(module.summary([{'executions':'100','totalDurationMs':1000,'totalCpuMs':500,'totalLogicalReads':100,'maxDurationMs':100,'planId':'1'},{'executions':'1','totalDurationMs':1000,'totalCpuMs':500,'totalLogicalReads':100,'maxDurationMs':1000,'planId':'2'}])['meanMs'],2000/101)
    def test_fixture_server_enforces_its_mock_login(self):
        import urllib.request,urllib.error
        server=module.make_server()
        try:
            with self.assertRaises(urllib.error.HTTPError) as e:urllib.request.urlopen('http://127.0.0.1:'+str(server.server_port)+'/api/targets')
            self.assertEqual(e.exception.code,401)
        finally:server.shutdown();server.server_close()
if __name__=='__main__':unittest.main()
