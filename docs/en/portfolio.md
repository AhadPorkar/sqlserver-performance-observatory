# Portfolio and interview guide

[Project](../../README.md) | [Deutsch](../de/portfolio.md)

## A credible engineering story
Use a real failed experiment or measured regression, not a fictional customer result. A concise demonstration is: show source coverage; identify a duration/read change; inspect plan identity and sample count; show the controlled workload; compare restored-index measurements; explain why correlation is not causation.

Suggested English introduction:
> I built a read-only SQL Server diagnostic workspace in C# and ASP.NET Core. Its emphasis is correct aggregation, explicit observation gaps and least-privilege collection. A separate guarded lab reproduces query regression, blocking and deadlocks. I distinguish synthetic UI fixtures from measured SQL evidence.

This is a project description, not evidence of production deployment or quantified savings. Add measured claims only after retaining the raw results, workload, machine details and image digest.

## Interview questions worth answering
Why is an average of averages wrong? Why can't you reconstruct p95 from Query Store means? What happens when a sleeping session holds locks? Why can negative blocker IDs not be followed as sessions? What does the server grant reveal beyond the chosen database? Can a TOP clause guarantee low XEL I/O? What happens after repeated polling, a disk error or a restarted collector? Why omit raw SQL instead of claiming perfect regex redaction?

Explain what you rejected: auto-index creation, terminating sessions, arbitrary SQL endpoints, an AI-generated root cause without evidence, microservices for a small installation and fake benchmark badges.

## Demo sequence
Start replay for the UI tour and point at the synthetic banner. Then run the real local Developer experiment separately. Show the CI run and exact commit. Present actual reader-denial tests and actual before/after JSON. If any stage is not run, say so. The default thresholds need not fire in a short fast-machine experiment.

Potential next contribution: capture monitoring overhead and bounded XEL cursor recovery on a real workload. Treat every enhancement as an issue with an acceptance test, not a list of technologies.
