# Contributing

[Deutsch](CONTRIBUTING.de.md)

Open an issue with a small scope, behavior to reproduce and acceptance criteria. Never attach production XEL, query text, dumps, credentials, state files or Data Protection keys. Prefer synthetic fixtures.

Run source/Node/Python checks, the .NET unit suite, and the relevant actual ASP.NET/SQL acceptance tests. State exactly which commands ran and which did not. Failing SQL/security tests must not be replaced by skipped or mocked success. Changes to grants, SQL, XML, authentication or lab guards require an explicit threat/permission review.

Keep English, German and Persian UI catalogs complete. Keep paired engineering docs synchronized. Add a decision record for architectural tradeoffs, and a failure-mode test for bug fixes. No generated activity commits or invented benchmark results. The source ZIP hash manifest records the delivery snapshot; it is not a signed attestation and is not expected to describe later edited working trees.
