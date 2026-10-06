# Synthetic fixtures

Every file here is authored test data, not exported customer data and not a measured benchmark.
`replay.json` uses fixed UTC timestamps, decimal-string bigint fields and deliberately different execution counts.
`deadlock.xml` contains sentinel sensitive values so tests can verify that the exported summary omits them.
Do not replace these with production exports. Real lab reports belong in ignored `artifacts/`.
