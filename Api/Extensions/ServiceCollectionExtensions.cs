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

namespace Stronghold.EnterpriseEstimating.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var jwtSecret = config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Jwt:Issuer"] ?? "stronghold-estimating",
                    ValidAudience = config["Jwt:Audience"] ?? "stronghold-estimating",
                    IssuerSigningKey = jwtKey,
                };
            });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<EstimateNumberService>();
        services.AddScoped<ToolExecutorService>();
        services.AddScoped<AiService>();
        services.AddScoped<RfqParserService>();
        services.AddScoped<ProposalPdfService>();
        services.AddScoped<SchedulingDemandService>();
        services.AddScoped<AssignmentConflictService>();
        services.AddScoped<CoverageCalculationService>();
        services.AddScoped<SuggestedMatchService>();
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
        return services;
    }

    public static IServiceCollection AddAiHttpClients(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient("groq", client =>
        {
            client.BaseAddress = new Uri(config["Ai:GroqBaseUrl"] ?? "https://api.groq.com/openai/v1");
            var apiKey = config["Ai:GroqApiKey"] ?? "";
            if (!string.IsNullOrEmpty(apiKey))
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            client.Timeout = TimeSpan.FromSeconds(120);
        });

        services.AddHttpClient("azure-ai", client =>
        {
            var endpoint = config["Ai:AzureFoundryEndpoint"] ?? "";
            if (!string.IsNullOrEmpty(endpoint))
                client.BaseAddress = new Uri(endpoint.TrimEnd('/') + "/");
            var apiKey = config["Ai:AzureFoundryKey"] ?? "";
            if (!string.IsNullOrEmpty(apiKey))
                client.DefaultRequestHeaders.Add("api-key", apiKey);
            client.Timeout = TimeSpan.FromSeconds(120);
        });

        return services;
    }

    public static IServiceCollection AddApiVersioningAndSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddApiVersioning(opt =>
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

        services.AddSingleton<FluentValidationSchemaProcessor>();
        services.AddOpenApiDocument(
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

        return services;
    }

    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env)
    {
#if DEBUG
        services.AddDbContextFactory<AppDbContext>(options =>
            options
                .UseSqlServer(
                    config.GetConnectionString("SqlDb"),
                    o => o.EnableRetryOnFailure().UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                )
                .EnableSensitiveDataLogging()
        );
#else
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlServer(
                config.GetConnectionString("SqlDb"),
                o => o.EnableRetryOnFailure().UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
            )
        );
#endif
        services.AddScoped<ReferenceDataSeeder>();
        services.AddScoped<DemoDataSeeder>();
        services.AddScoped<DatabaseBootstrapper>();
        return services;
    }
}
