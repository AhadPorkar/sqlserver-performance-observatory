using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Observatory.Core;
using Observatory.SqlServer;
using Observatory.Web;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["urls"] ?? "http://localhost:5080");
var options = builder.Configuration.GetSection("Observatory").Get<ObservatoryOptions>() ?? new();
options.Validate();
var accessKey = builder.Configuration["SPO_ACCESS_KEY"] ?? Environment.GetEnvironmentVariable("SPO_ACCESS_KEY");
if (string.IsNullOrWhiteSpace(accessKey) || accessKey.Length < 32)
    throw new InvalidOperationException("Set SPO_ACCESS_KEY to a randomly generated secret of at least 32 characters. See scripts/run-demo.");
var accessHash = SHA256.HashData(Encoding.UTF8.GetBytes(accessKey));
var origin = new Uri(options.PublicOrigin).GetLeftPart(UriPartial.Authority);
builder.Services.AddSingleton(options);
builder.Services.AddSingleton(options.Collection);
builder.Services.AddSingleton<SqlCollector>();
builder.Services.AddSingleton<SnapshotCatalog>();
builder.Services.AddSingleton(_ => new SnapshotStore(options.StateDirectory, options.HistoryLimit));
builder.Services.AddHostedService<CollectionWorker>();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new Int64StringConverter()));
builder.Services.AddDataProtection().SetApplicationName("SqlServerPerformanceObservatory")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(options.StateDirectory, ".keys")));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.Cookie.Name = "spo.session"; o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.SecurePolicy = origin.StartsWith("https://", StringComparison.Ordinal) ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
    o.ExpireTimeSpan = TimeSpan.FromHours(8); o.SlidingExpiration = false;
    o.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
    o.Events.OnValidatePrincipal = context =>
    {
        var expected = Convert.ToHexString(accessHash);
        if (context.Principal?.FindFirstValue("key-version") != expected) context.RejectPrincipal();
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = "X-CSRF-TOKEN"; o.Cookie.Name = "spo.csrf"; o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = origin.StartsWith("https://", StringComparison.Ordinal) ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
});
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("login", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.ForwardLimit = 1;
    // Never trust all proxies. Defaults are explicitly cleared.
    o.KnownProxies.Clear(); o.KnownIPNetworks.Clear();
    foreach (var proxy in options.TrustedProxies) o.KnownProxies.Add(IPAddress.Parse(proxy));
});
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 8192);
var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    context.Response.StatusCode = 500;
    await context.Response.WriteAsJsonAsync(new { code = "INTERNAL_ERROR", traceId = context.TraceIdentifier });
}));
if (options.TrustedProxies.Length > 0) app.UseForwardedHeaders();
app.Use(async (context, next) =>
{
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; object-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    if (context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/auth"))
        context.Response.Headers["Cache-Control"] = "no-store";
    if (origin.StartsWith("https://", StringComparison.Ordinal) && !context.Request.IsHttps)
    { context.Response.StatusCode = 400; await context.Response.WriteAsJsonAsync(new { code = "HTTPS_REQUIRED" }); return; }
    var suppliedOrigin = context.Request.Headers.Origin.ToString();
    if (context.Request.Method is not ("GET" or "HEAD") && suppliedOrigin != origin)
    { context.Response.StatusCode = 403; await context.Response.WriteAsJsonAsync(new { code = "ORIGIN_REJECTED" }); return; }
    // Do not accept arbitrary Host headers (e.g. DNS rebinding to a local app).
    if (!string.Equals(context.Request.Host.Value, new Uri(origin).Authority, StringComparison.OrdinalIgnoreCase))
    { context.Response.StatusCode = 400; return; }
    await next();
});
app.UseDefaultFiles(); app.UseStaticFiles();
app.UseRouting(); app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization();
app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));
app.MapGet("/auth/csrf", (IAntiforgery antiforgery, HttpContext context) =>
    Results.Ok(new { token = antiforgery.GetAndStoreTokens(context).RequestToken }));
app.MapPost("/auth/login", async (LoginRequest login, IAntiforgery antiforgery, HttpContext context) =>
{
    try { await antiforgery.ValidateRequestAsync(context); }
    catch (AntiforgeryValidationException) { return Results.BadRequest(new { code = "CSRF_REJECTED" }); }
    if (login.AccessKey is null || login.AccessKey.Length > 1024 || !CryptographicOperations.FixedTimeEquals(accessHash, SHA256.HashData(Encoding.UTF8.GetBytes(login.AccessKey))))
        return Results.Json(new { code = "INVALID_ACCESS_KEY" }, statusCode: 401);
    var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "analyst"), new Claim(ClaimTypes.Name, "Analyst"), new Claim("key-version", Convert.ToHexString(accessHash)) };
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
    return Results.Ok(new { ok = true });
}).RequireRateLimiting("login");
app.MapPost("/auth/logout", async (IAntiforgery antiforgery, HttpContext context) =>
{
    try { await antiforgery.ValidateRequestAsync(context); }
    catch (AntiforgeryValidationException) { return Results.BadRequest(new { code = "CSRF_REJECTED" }); }
    await context.SignOutAsync(); return Results.Ok(new { ok = true });
}).RequireAuthorization();
var api = app.MapGroup("/api").RequireAuthorization();
api.MapGet("/targets", (SnapshotCatalog catalog) => Results.Ok(new { mode = options.Mode,
    targets = catalog.All().Select(x => new { x.TargetId, x.DisplayName, x.Mode, x.CapturedUtc }) }));
api.MapGet("/targets/{id}/snapshot", (string id, SnapshotCatalog catalog) =>
    catalog.Get(id) is { } s ? Results.Ok(s) : Results.NotFound(new { code = "TARGET_NOT_READY" }));
api.MapGet("/targets/{id}/analysis", (string id, int? baselineMinutes, int? recentMinutes, SnapshotCatalog catalog) =>
{
    if (catalog.Get(id) is not { } s) return Results.NotFound(new { code = "TARGET_NOT_READY" });
    var baseline = baselineMinutes ?? 60; var recent = recentMinutes ?? 60;
    if (baseline is < 1 or > 1440 || recent is < 1 or > 1440 || baseline + recent > options.Collection.LookbackHours * 60)
        return Results.BadRequest(new { code = "INVALID_COMPARISON_WINDOW" });
    return Results.Ok(AnalysisEngine.Analyze(s, new(baseline, recent)));
});
api.MapGet("/targets/{id}/blocking", (string id, SnapshotCatalog catalog) =>
    catalog.Get(id) is { } s ? Results.Ok(new { status = s.BlockingStatus, chains = BlockingAnalyzer.Build(s.Blocking) }) : Results.NotFound());
api.MapGet("/targets/{id}/evidence", (string id, SnapshotCatalog catalog) =>
{
    if (catalog.Get(id) is not { } s) return Results.NotFound();
    // This is an unsigned diagnostic export, not a compliance attestation.
    var evidence = new { schemaVersion = 1, generatedUtc = DateTimeOffset.UtcNow, kind = "diagnostic-evidence-unsigned",
        snapshot = s, analysis = AnalysisEngine.Analyze(s, new()), chains = BlockingAnalyzer.Build(s.Blocking) };
    var bytes = JsonSerializer.SerializeToUtf8Bytes(evidence, JsonDefaults.Create());
    return Results.File(bytes, "application/json", $"observatory-{s.TargetId}-{s.Id}.json");
});
app.Run();
public sealed record LoginRequest(string? AccessKey);
public partial class Program { }
