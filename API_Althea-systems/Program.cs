using API_Althea_systems.Data;
using API_Althea_systems.Data.Seed;
using API_Althea_systems.Extensions;
using API_Althea_systems.Middleware;
using API_Althea_systems.Services.IServices;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

namespace API_Althea_systems;

public class Program
{
    public static async Task Main(string[] args)
    {
        // QuestPDF license must be set BEFORE the first Document.Generate call.
        // Community License: free for organisations under $1M USD revenue
        // (https://www.questpdf.com/license/). Switch to Professional or
        // Enterprise when we cross that threshold.
        QuestPDF.Settings.License = LicenseType.Community;

        var builder = WebApplication.CreateBuilder(args);

        // ── Services ──────────────────────────────────────────
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            });
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });
        builder.Services.AddOpenApi();
        builder.Services.AddSwaggerDocumentation();
        builder.Services.AddDatabase(builder.Configuration);
        builder.Services.AddRedisCache(builder.Configuration);
        builder.Services.AddEncryption(builder.Configuration);
        builder.Services.AddJwtAuthentication(builder.Configuration);
        builder.Services.AddCorsPolicy(builder.Configuration);
        builder.Services.AddValidation();
        builder.Services.AddRateLimiting();
        builder.Services.AddHealthCheckServices(builder.Configuration);
        builder.Services.AddStripe(builder.Configuration);
        builder.Services.AddApplicationServices();

        var app = builder.Build();

        // ── Pipeline ──────────────────────────────────────────

        // Custom middleware (order matters: Error wraps everything, Debug logs, JWT extracts user)
        app.UseMiddleware<ErrorHandlerMiddleware>();
        if (app.Environment.IsDevelopment())
        {
            app.UseMiddleware<DebugHandlerMiddleware>();
        }
        app.UseMiddleware<JwtMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Althea Systems API v1");
                options.RoutePrefix = "swagger";
            });
        }

        app.UseCors("AltheaCors");
        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();

        // Health check endpoint (no auth — for container/orchestrator probes)
        app.MapHealthChecks("/health");

        app.MapControllers();

        // Apply pending migrations on startup (all environments)
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AltheaDbContext>();
            await db.Database.MigrateAsync();

            // Seed if database is empty (safe: checks Users.Any() before inserting)
            await DataSeeder.SeedAsync(db);

            // Backfill invoices for any paid order missing one. Catches legacy
            // orders from before auto-issuance landed and orders whose webhook
            // path failed. Runs every boot — guarded internally for cost.
            var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
            var backfillLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            await InvoiceBackfill.RunAsync(db, invoiceService, backfillLogger);
        }

        await app.RunAsync();
    }
}
