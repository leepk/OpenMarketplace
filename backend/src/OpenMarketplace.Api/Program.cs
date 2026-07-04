using Microsoft.AspNetCore.HttpOverrides;
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

    // HTTP only. Docker/reverse proxy can handle HTTPS outside the API.
    builder.WebHost.UseUrls("http://0.0.0.0:5100");

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

    app.MapHealthChecks("/health/live");
    app.MapHealthChecks("/health/ready");
    app.MapControllers();

    // No manual dotnet ef command is required. This automatically applies pending migrations and seed data
    // before the API accepts requests, so controllers never query missing tables such as user_profiles.
    await app.InitializeDatabaseWithRetryAsync();

    app.Logger.LogInformation("OpenMarketplace API listening on HTTP port 5100. Swagger: http://localhost:5100/swagger/index.html");
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
