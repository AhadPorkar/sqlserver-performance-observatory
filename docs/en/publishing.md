# Publish to GitHub

[Project](../../README.md) | [Deutsch](../de/publishing.md)

The source ZIP contains the repository root, including hidden `.github` files. Do not upload the ZIP itself as the entire repository. Do not initialize a nested repository inside `src`.

Create an empty repository named `sqlserver-performance-observatory` in your GitHub account. The following URL is the intended new location, not a claim that it already exists. Git is required; GitHub CLI and winget are not required.

```powershell
Set-Location 'D:\GITHUB\sqlserver-performance-observatory'
git init -b main
git config user.name 'AhadPorkar'
# Use your actual verified GitHub email or exact GitHub-provided noreply address.
git config user.email '<your-verified-or-noreply-email>'
git add --dry-run .
# Review every proposed file before staging. No .env, state, logs or credentials.
git add .
git diff --cached --check
git diff --cached --stat
git commit -m 'feat: add read-only SQL Server performance observatory'
git remote add origin https://github.com/AhadPorkar/sqlserver-performance-observatory.git
git push -u origin main
```

Stop on any error. Authenticate using your existing approved Git credential manager or SSH setup; never embed a token in the remote URL. If a repository already has commits, clone it and copy reviewed source into that checkout instead of force-pushing.

Open Actions and inspect **Verify** on the new commit. It builds/tests .NET on Windows and Linux and exercises actual ASP.NET plus native Chromium. The SQL Server acceptance workflow is manual and requires explicit Developer license acceptance. A successful source push is not a successful build or a SQL certification.

Suggested description: `Read-only SQL Server diagnostics with weighted Query Store comparisons, blocking chains, sanitized deadlock evidence and a guarded reproducible lab.`

Suggested topics: `csharp`, `aspnet-core`, `sql-server`, `query-store`, `extended-events`, `performance-engineering`, `observability`.

Keep the release marked preview until full build, SQL permissions, workload, recovery and security acceptance have passed. Do not add a passing badge without a passing run.
