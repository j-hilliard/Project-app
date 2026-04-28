using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using NSwag.Generation.Processors.Security;
using Stronghold.EnterpriseEstimating.Api.Authorization;
using Stronghold.EnterpriseEstimating.Api.Domain;
using Stronghold.EnterpriseEstimating.Api.Services;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Bootstrap;
using ZymLabs.NSwag.FluentValidation;

var builder = WebApplication.CreateBuilder(args);
var isLocal = builder.Environment.IsEnvironment("Local") || builder.Environment.IsDevelopment();
var skipDatabaseInitialization = string.Equals(
    Environment.GetEnvironmentVariable("SkipDatabaseInitialization"),
    "true",
    StringComparison.OrdinalIgnoreCase
);
var hasSqlConnectionString = !string.IsNullOrWhiteSpace(
    builder.Configuration.GetConnectionString("SqlDb")
);

if (isLocal)
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();
}

// Add appsettings.{environment}.json override
builder.Configuration
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true);

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "stronghold-estimating",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "stronghold-estimating",
            IssuerSigningKey = jwtKey,
        };
    });

builder.Services.AddMemoryCache();
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // Prevent circular reference errors from EF navigation properties (e.g. CostBookExpense.CostBook → CostBook.Expenses)
        opts.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        opts.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Application services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<EstimateNumberService>();
builder.Services.AddScoped<ToolExecutorService>();
builder.Services.AddScoped<AiService>();
builder.Services.AddScoped<RfqParserService>();
builder.Services.AddScoped<ProposalPdfService>();
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Groq HTTP client (OpenAI-compatible)
builder.Services.AddHttpClient("groq", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Ai:GroqBaseUrl"] ?? "https://api.groq.com/openai/v1");
    var apiKey = builder.Configuration["Ai:GroqApiKey"] ?? "";
    if (!string.IsNullOrEmpty(apiKey))
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
    client.Timeout = TimeSpan.FromSeconds(120);
});

// Azure AI Foundry HTTP client
builder.Services.AddHttpClient("azure-ai", client =>
{
    var endpoint = builder.Configuration["Ai:AzureFoundryEndpoint"] ?? "";
    if (!string.IsNullOrEmpty(endpoint))
        client.BaseAddress = new Uri(endpoint.TrimEnd('/') + "/");
    var apiKey = builder.Configuration["Ai:AzureFoundryKey"] ?? "";
    if (!string.IsNullOrEmpty(apiKey))
        client.DefaultRequestHeaders.Add("api-key", apiKey);
    client.Timeout = TimeSpan.FromSeconds(120);
});

// Versioning
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ReportApiVersions = true;
})
.AddMvc()
.AddApiExplorer(opt =>
{
    opt.GroupNameFormat = "'v'V";
    opt.SubstituteApiVersionInUrl = true;
});

// NSwag — Bearer token auth for Swagger UI
builder.Services.AddSingleton<FluentValidationSchemaProcessor>();
builder.Services.AddOpenApiDocument(
    (configure, serviceProvider) =>
    {
        configure.Version = "v1";
        configure.DocumentName = "v1";
        configure.ApiGroupNames = new[] { "v1" };
        configure.Title = "Stronghold Enterprise Estimating API";
        configure.SchemaSettings.SchemaProcessors.Add(serviceProvider.GetService<FluentValidationSchemaProcessor>());
        configure.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
        configure.AddSecurity("Bearer", Enumerable.Empty<string>(), new OpenApiSecurityScheme
        {
            Type = OpenApiSecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter your JWT token.",
        });
    }
);

// Database
#if DEBUG
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options
        .UseSqlServer(
            builder.Configuration.GetConnectionString("SqlDb"),
            o => o.EnableRetryOnFailure().UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
        )
        .EnableSensitiveDataLogging()
);
#else
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SqlDb"),
        o => o.EnableRetryOnFailure().UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
    )
);
#endif

builder.Services.AddHttpContextAccessor();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddScoped<ReferenceDataSeeder>();
builder.Services.AddScoped<DemoDataSeeder>();
builder.Services.AddScoped<DatabaseBootstrapper>();
builder.Services.AddScoped<SchedulingDemandService>();
builder.Services.AddScoped<AssignmentConflictService>();
builder.Services.AddScoped<CoverageCalculationService>();
builder.Services.AddScoped<SuggestedMatchService>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

var app = builder.Build();

if (isLocal)
{
    app.UseOpenApi();
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
    app.UseSwaggerUi();
}
else
{
    app.UseHsts();
}

var autoMigrateOnStartup = app.Configuration.GetValue<bool>("Database:AutoMigrateOnStartup", defaultValue: true);

if (hasSqlConnectionString && !skipDatabaseInitialization && autoMigrateOnStartup)
{
    using var scope = app.Services.CreateScope();
    var bootstrapper = scope.ServiceProvider.GetRequiredService<DatabaseBootstrapper>();
    await bootstrapper.RunAsync();
}

app.UseCors(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseHttpsRedirection();
app.UseAuthentication();

// Local/Dev only: X-Company-Override header lets the dev switcher pill change company context without re-logging in.
if (isLocal)
{
    app.Use(async (ctx, next) =>
    {
        var overrideCode = ctx.Request.Headers["X-Company-Override"].FirstOrDefault();
        if (!string.IsNullOrEmpty(overrideCode) && ctx.User.Identity?.IsAuthenticated == true)
        {
            var claims = ctx.User.Claims
                .Where(c => c.Type != "company_code")
                .Append(new System.Security.Claims.Claim("company_code", overrideCode))
                .ToList();
            var identity = new System.Security.Claims.ClaimsIdentity(
                claims, ctx.User.Identity.AuthenticationType);
            ctx.User = new System.Security.Claims.ClaimsPrincipal(identity);
        }
        await next();
    });
}

app.UseAuthorization();
app.MapControllers();
app.Run();
