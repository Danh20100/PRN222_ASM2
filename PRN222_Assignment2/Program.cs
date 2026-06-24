using BusinessLayer.Models;
using BusinessLayer.Services;
using BusinessLayer.Strategies;
using DataAccessLayer.Context;
using DataAccessLayer.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)));

// ── Repository & Unit of Work ─────────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ── Cookie Authentication ─────────────────────────────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("TeacherOrAdmin", policy => policy.RequireRole("Admin", "Teacher"));
});

// ── API Keys Settings (strongly-typed config) ────────────────────────
// Bind section "ApiKeys" trong appsettings.json vào ApiKeysSettings POCO
var apiKeys = builder.Configuration
    .GetSection(ApiKeysSettings.SectionName)
    .Get<ApiKeysSettings>() ?? new ApiKeysSettings();

// Validate Gemini key bắt buộc
if (string.IsNullOrWhiteSpace(apiKeys.Gemini))
    throw new InvalidOperationException(
        "Gemini API key chưa được cấu hình. Vui lòng thêm vào appsettings.json: ApiKeys:Gemini");

// ── HttpClient (for embedding/AI calls) ──────────────────────────────
builder.Services.AddHttpClient("EmbeddingClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(120);
});

// ── Chunking Strategies (Strategy Pattern — all registered) ──────────
builder.Services.AddSingleton<IChunkingStrategy, FixedSizeChunkingStrategy>();
builder.Services.AddSingleton<IChunkingStrategy, ParagraphChunkingStrategy>();
builder.Services.AddSingleton<IChunkingStrategy, SentenceChunkingStrategy>();
builder.Services.AddSingleton<IChunkingStrategy, RecursiveChunkingStrategy>();

// ── Embedding Provider Factory ────────────────────────────────────────
builder.Services.AddSingleton<EmbeddingProviderFactory>(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();

    return new EmbeddingProviderFactory(
        httpClientFactory: () => httpClientFactory.CreateClient("EmbeddingClient"),
        loggerFactory: loggerFactory,
        apiKeys: apiKeys);
});

// ── Business Services ─────────────────────────────────────────────────
builder.Services.AddScoped<IFakeEmailService, FakeEmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();

builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IChatService>(sp =>
{
    return new ChatService(
        sp.GetRequiredService<IUnitOfWork>(),
        sp.GetRequiredService<EmbeddingProviderFactory>(),
        sp.GetRequiredService<ILogger<ChatService>>(),
        apiKeys: apiKeys);
});
builder.Services.AddScoped<IBenchmarkService, BenchmarkService>();

// ── Razor Pages ───────────────────────────────────────────────────────
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
    options.Conventions.AuthorizeFolder("/Documents", "TeacherOrAdmin");
    options.Conventions.AuthorizeFolder("/Benchmark", "TeacherOrAdmin");
    options.Conventions.AllowAnonymousToPage("/Auth/Login");
    options.Conventions.AllowAnonymousToPage("/Auth/Register");
});

// ── Session (for flash messages) ─────────────────────────────────────
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ── CORS ──────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

// ── Auto-apply EF Core Migrations on startup ──────────────────────────
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database migration applied successfully.");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();
