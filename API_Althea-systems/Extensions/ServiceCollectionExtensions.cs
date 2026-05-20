using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using FluentValidation;
using FluentValidation.AspNetCore;
using StackExchange.Redis;
using API_Althea_systems.Data;
using API_Althea_systems.Repositories;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services;
using API_Althea_systems.Services.Email;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AltheaDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("PostgreSQL")));

        return services;
    }

    public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            throw new InvalidOperationException(
                "Redis connection required for 2FA/Step-Up — set ConnectionStrings:Redis");
        }

        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnectionString));

        return services;
    }

    public static IServiceCollection AddEncryption(this IServiceCollection services, IConfiguration configuration)
    {
        // Eagerly construct the service so a missing/invalid Encryption:Key
        // fails the application startup, mirroring the JWT secret behavior.
        var encryption = new EncryptionService(configuration);
        services.AddSingleton<IEncryptionService>(encryption);
        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

        if (Encoding.UTF8.GetByteCount(secretKey) < 32)
        {
            throw new InvalidOperationException(
                "JWT SecretKey must be at least 32 bytes (HS256 requires >= 256 bits). " +
                "Set JwtSettings:SecretKey to a strong random string of 32+ characters.");
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

        return services;
    }

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter your JWT token (without 'Bearer ' prefix)"
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("bearer", document)] = []
            });
        });

        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000"];

        services.AddCors(options =>
        {
            options.AddPolicy("AltheaCors", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection AddValidation(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<Program>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IEmailConfirmationTokenRepository, EmailConfirmationTokenRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IContentRepository, ContentRepository>();
        services.AddScoped<IRecoveryCodeRepository, RecoveryCodeRepository>();

        // Services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IContentService, ContentService>();
        services.AddSingleton<ITwoFactorStateStore, RedisTwoFactorStateStore>();
        services.AddSingleton<IStepUpConsumptionStore, RedisStepUpConsumptionStore>();
        // Login-attempt throttling falls back to a no-op when Redis is not
        // configured (dev / unit tests) so login still works locally without
        // pulling Redis into every workflow.
        services.AddSingleton<ILoginAttemptStore>(sp =>
        {
            var redis = sp.GetService<IConnectionMultiplexer>();
            return redis is null
                ? new NullLoginAttemptStore()
                : new RedisLoginAttemptStore(redis);
        });
        services.AddScoped<ITwoFactorService, TwoFactorService>();

        // Special services
        services.AddSingleton<IVatCalculationService, VatCalculationService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IAnonymizationService, AnonymizationService>();
        services.AddSingleton<IPasswordHasher, PasswordHasherService>();
        services.AddScoped<IStripeService, StripeService>();
        services.AddScoped<IStripeWebhookProcessor, StripeWebhookProcessor>();
        // PDF rendering is stateless and fast (QuestPDF reuses a thread-local
        // engine), Singleton is appropriate.
        services.AddSingleton<IInvoicePdfService, InvoicePdfService>();

        return services;
    }

    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        // SMTP settings are bound from "Smtp" — blank Host means "no SMTP
        // configured" and SmtpEmailSender logs+skips at send time (see its
        // null-host branch). That keeps local dev usable without credentials.
        services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));

        // Templates load once at construction (Singleton). The options POCO
        // is eagerly built so we can pass it via constructor injection rather
        // than IOptions<>, which keeps the boot-time file scan visible in
        // EmailTemplateRenderer's stack frame on failure.
        var templateOptions = configuration.GetSection("EmailTemplates").Get<EmailTemplateOptions>()
            ?? new EmailTemplateOptions();
        services.AddSingleton(templateOptions);
        services.AddSingleton<IEmailTemplateRenderer, EmailTemplateRenderer>();

        // The SMTP sender is stateless (creates a fresh SmtpClient per send),
        // Singleton is fine and avoids per-request allocation.
        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        // Phase 2: registration-confirmation specific options + sender.
        services.Configure<EmailConfirmationOptions>(configuration.GetSection("EmailConfirmation"));
        services.AddScoped<IEmailConfirmationSender, EmailConfirmationSender>();

        // Phase 3: order-confirmation sender. Wraps IInvoicePdfService +
        // IEmailSender + IEmailTemplateRenderer; consumed by
        // InvoiceService.EnsureEmailedAsync.
        services.AddScoped<IOrderConfirmationSender, OrderConfirmationSender>();

        return services;
    }

    public static IServiceCollection AddStripe(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("Stripe");
        var secretKey = section["SecretKey"];

        // Fail fast at startup if Stripe is unconfigured — we'd rather refuse
        // to boot than silently fail at the first PaymentIntent. Placeholders
        // (sk_test_REPLACE_ME) are accepted so local dev without real keys
        // can still build the project; the SDK call itself will reject them.
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException(
                "Stripe:SecretKey is not configured. Set STRIPE_SECRET_KEY (sk_test_* in dev) " +
                "via env var or appsettings.json.");
        }

        if (!secretKey.StartsWith("sk_test_") && !secretKey.StartsWith("sk_live_"))
        {
            throw new InvalidOperationException(
                $"Stripe:SecretKey has an invalid prefix (must start with 'sk_test_' or 'sk_live_'). " +
                "Did you swap it with the publishable key?");
        }

        services.Configure<StripeOptions>(section);
        return services;
    }

    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services, IConfiguration configuration)
    {
        var healthChecks = services.AddHealthChecks();

        var pgConn = configuration.GetConnectionString("PostgreSQL");
        if (!string.IsNullOrWhiteSpace(pgConn))
        {
            healthChecks.AddNpgSql(pgConn, name: "postgresql");
        }

        var redisConn = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            healthChecks.AddRedis(redisConn, name: "redis");
        }

        return services;
    }

    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Global policy: 100 requests per minute per IP
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            // Strict policy for auth routes: 10 requests per minute per IP
            options.AddFixedWindowLimiter("auth", limiterOptions =>
            {
                limiterOptions.PermitLimit = 10;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
            });

            // Strict policy for contact/messages: 5 per minute per IP
            options.AddFixedWindowLimiter("contact", limiterOptions =>
            {
                limiterOptions.PermitLimit = 5;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
            });
        });

        return services;
    }
}
