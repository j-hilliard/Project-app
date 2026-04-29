using FluentValidation;
using Stronghold.EnterpriseEstimating.Api.Extensions;
using Stronghold.EnterpriseEstimating.Api.Middleware;
using Stronghold.EnterpriseEstimating.Data.Bootstrap;

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

builder.Configuration
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true);

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddMemoryCache();
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        opts.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddApplicationServices();
builder.Services.AddAiHttpClients(builder.Configuration);
builder.Services.AddApiVersioningAndSwagger();
builder.Services.AddDatabase(builder.Configuration, builder.Environment);
builder.Services.AddHttpContextAccessor();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

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

if (isLocal)
    app.UseCompanyOverride();

app.UseAuthorization();
app.MapControllers();
app.Run();
