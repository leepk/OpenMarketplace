using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using OpenMarketplace.Api.Extensions;
using OpenMarketplace.Api.Middleware;
using OpenMarketplace.Application;
using OpenMarketplace.Infrastructure;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/openmarketplace-api-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        shared: true)
    .CreateLogger();

try
{
    Log.Information("Starting OpenMarketplace API");

    var builder = WebApplication.CreateBuilder(args);

    // Respect ASPNETCORE_URLS/launchSettings/Docker port. Default local API port is 5001.
    var configuredUrls = builder.Configuration["ASPNETCORE_URLS"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    if (string.IsNullOrWhiteSpace(configuredUrls))
    {
        builder.WebHost.UseUrls("http://0.0.0.0:5001");
    }

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "OpenMarketplace.Api"));

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                   ForwardedHeaders.XForwardedProto |
                                   ForwardedHeaders.XForwardedHost;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    var jwtSecret = builder.Configuration["Jwt:Secret"] ?? Environment.GetEnvironmentVariable("OPENMARKETPLACE_JWT_SECRET");
    if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
    {
        jwtSecret = "OpenMarketplace_Local_Development_Jwt_Secret_Change_Me_At_Least_64_Chars";
    }

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "OpenMarketplace.Api",
                ValidAudience = builder.Configuration["Jwt:Audience"] ?? "OpenMarketplace.Web",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.FromMinutes(2)
            };
        });
    builder.Services.AddAuthorization();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "OpenMarketplace API",
            Version = "v1"
        });
        options.CustomSchemaIds(type => (type.FullName ?? type.Name).Replace("+", "."));
        options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter: Bearer {token}"
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddHealthChecks();
    builder.Services.AddCors(options => options.AddPolicy("Default", policy => policy
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowAnyOrigin()));

    var app = builder.Build();

    app.UseForwardedHeaders();

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    // Swagger starts immediately. Database migration/seed runs automatically in the background below.
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "swagger/{documentName}/swagger.json";
    });
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "OpenMarketplace API v1");
        options.DocumentTitle = "OpenMarketplace API Docs";
        options.DisplayRequestDuration();
    });

    app.MapGet("/", () => Results.Redirect("/swagger/index.html", permanent: false));
    app.MapGet("/swagger-json", () => Results.Redirect("/swagger/v1/swagger.json", permanent: false));

    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseCors("Default");
    app.UseStaticFiles();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health/live");
    app.MapHealthChecks("/health/ready");
    app.MapControllers();

    // Keep Swagger/API online even if the database migration/seed is retrying or temporarily failing.
    // Database initialization now runs after the web host starts, so /swagger can still be opened
    // while startup repairs seed data or waits for PostgreSQL.
    app.StartDatabaseInitialization();

    app.Logger.LogInformation("OpenMarketplace API listening. Swagger: http://localhost:5001/swagger/index.html");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "OpenMarketplace API terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
