# Third-party components and source references

Original repository code uses the MIT license. The runtime SDK, SQL Server product/container and dependencies retain their own licenses. This repository does not redistribute a SQL Server installation, .NET SDK, fonts, browser binaries or NuGet packages.

| Component | Role | Source / license information |
|---|---|---|
| .NET / ASP.NET Core | Runtime and web framework. | https://github.com/dotnet/aspnetcore |
| Microsoft.Data.SqlClient 6.1.7 | SQL Server driver; chosen LTS line, not latest STS. | https://github.com/dotnet/SqlClient |
| xUnit and VS adapter | Authored .NET tests. | https://github.com/xunit/xunit |
| Microsoft.NET.Test.Sdk | Test discovery/execution. | https://github.com/microsoft/vstest |
| Playwright | Browser test tooling only. | https://github.com/microsoft/playwright-python |
| SQL Server Developer container | Optional isolated lab, separate EULA. | https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker?view=sql-server-ver17 |

Pinned package versions are in the project files and tests/requirements.txt. The UI uses no third-party JavaScript runtime packages and no CDN. The original SVG mark and CSS contain no third-party logos or redistributed fonts. Read package license terms before redistribution. The project is not affiliated with or endorsed by Microsoft.
