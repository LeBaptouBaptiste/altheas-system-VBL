using System.Text.RegularExpressions;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services.Email;

/// <summary>
/// Loads <c>*.html</c> files from a configured directory at construction time
/// (registered as Singleton) and caches them in memory. Substitutes
/// <c>{{placeholder}}</c> tokens at render time.
///
/// Templates ship as files (not embedded resources) so designers can edit
/// them without recompiling. The csproj has a Content/CopyToOutputDirectory
/// rule so they end up next to the published DLLs.
/// </summary>
public class EmailTemplateRenderer : IEmailTemplateRenderer
{
    // {{key}} where key is one or more word characters. Anchored on the inner
    // group to keep replacement cheap. Match across newlines (templates span
    // multiple lines).
    private static readonly Regex PlaceholderRegex =
        new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

    private readonly IReadOnlyDictionary<string, string> _templates;
    private readonly ILogger<EmailTemplateRenderer> _logger;

    public EmailTemplateRenderer(
        EmailTemplateOptions options,
        IWebHostEnvironment env,
        ILogger<EmailTemplateRenderer> logger)
    {
        _logger = logger;

        var dir = Path.IsPathRooted(options.Directory)
            ? options.Directory
            : Path.Combine(env.ContentRootPath, options.Directory);

        _templates = LoadFromDisk(dir, logger);
    }

    // Test-friendly: callers can pass an already-built dictionary and skip
    // disk I/O. Keeps unit tests fast and hermetic. Public for the same
    // reason as SmtpEmailSender's transport-factory ctor — no InternalsVisibleTo
    // gymnastics — but DI never picks this overload (no IReadOnlyDictionary
    // registered).
    public EmailTemplateRenderer(IReadOnlyDictionary<string, string> templates)
    {
        _templates = templates;
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<EmailTemplateRenderer>.Instance;
    }

    public string Render(string templateName, IReadOnlyDictionary<string, string> placeholders)
    {
        if (!_templates.TryGetValue(templateName, out var raw))
        {
            // Fail loud at the call site — easier to debug than a 500 in
            // production with a vague "template not found" deep in the stack.
            throw new InvalidOperationException(
                $"Email template '{templateName}' is not loaded. " +
                $"Known templates: [{string.Join(", ", _templates.Keys)}]");
        }

        return PlaceholderRegex.Replace(raw, match =>
        {
            var key = match.Groups[1].Value;
            if (!placeholders.TryGetValue(key, out var value))
            {
                // Missing placeholder = bug in the calling code. Fail rather
                // than ship "Bonjour {{firstName}}" to a customer.
                throw new InvalidOperationException(
                    $"Template '{templateName}' references placeholder '{{{{{key}}}}}' " +
                    $"but the caller did not provide a value for it.");
            }
            return value;
        });
    }

    private static IReadOnlyDictionary<string, string> LoadFromDisk(
        string directory,
        ILogger logger)
    {
        if (!Directory.Exists(directory))
        {
            // Boot-time soft failure: log and return empty. Render() throws
            // later for every requested template, which is the right place
            // to surface the issue (caller knows what they wanted).
            logger.LogWarning(
                "Email templates directory does not exist: {Directory}. No templates loaded.",
                directory);
            return new Dictionary<string, string>();
        }

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in Directory.EnumerateFiles(directory, "*.html", SearchOption.TopDirectoryOnly))
        {
            // Template name is the filename without extension. We keep the
            // naming convention deliberately simple — one file per email,
            // language suffix optional ("welcome.fr.html") and treated as
            // part of the key for now (i18n is a future phase).
            var name = Path.GetFileNameWithoutExtension(path);
            map[name] = File.ReadAllText(path);
        }

        logger.LogInformation(
            "Loaded {Count} email template(s) from {Directory}: [{Names}]",
            map.Count, directory, string.Join(", ", map.Keys));

        return map;
    }
}

/// <summary>
/// Bound to the "EmailTemplates" config section. Singleton because it's read
/// once at construction of <see cref="EmailTemplateRenderer"/>.
/// </summary>
public class EmailTemplateOptions
{
    public string Directory { get; set; } = "Templates/Emails";
}
