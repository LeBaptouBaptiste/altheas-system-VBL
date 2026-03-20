using API_Althea_systems.Extensions;

namespace API_Althea_systems;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ── Services ──────────────────────────────────────────
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddSwaggerDocumentation();
        builder.Services.AddDatabase(builder.Configuration);
        builder.Services.AddRedisCache(builder.Configuration);
        builder.Services.AddJwtAuthentication(builder.Configuration);
        builder.Services.AddCorsPolicy(builder.Configuration);
        builder.Services.AddValidation();
        builder.Services.AddApplicationServices();

        var app = builder.Build();

        // ── Pipeline ──────────────────────────────────────────

        // Middleware custom (ErrorHandler, DebugHandler) will be added in Phase 3

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

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
