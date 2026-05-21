namespace API_Althea_systems.Services.IServices;

/// <summary>
/// Loads HTML templates from disk and substitutes <c>{{placeholder}}</c> tokens.
/// Kept deliberately dumb — no conditionals, no loops, no partials. Anything
/// fancier (Razor, Scriban) is overkill for the half-dozen transactional
/// emails this codebase needs. If we ever outgrow it, the interface stays
/// stable and the implementation can be swapped.
/// </summary>
public interface IEmailTemplateRenderer
{
    /// <summary>
    /// Renders the template registered under <paramref name="templateName"/>
    /// (filename without the .html extension). Throws if the template doesn't
    /// exist or if it references a placeholder not provided in
    /// <paramref name="placeholders"/> — we'd rather fail loudly than ship
    /// a "{{customerName}}" literal to a customer.
    ///
    /// When <paramref name="locale"/> is provided, the renderer looks up
    /// <c>{templateName}.{locale}.html</c> first (e.g. <c>welcome.en.html</c>)
    /// and falls back to <c>{templateName}.html</c> (the default French
    /// template) when no localised version exists. This keeps the catalog
    /// additive: untranslated emails ship in French until someone drops in
    /// a localised file.
    /// </summary>
    string Render(
        string templateName,
        IReadOnlyDictionary<string, string> placeholders,
        string? locale = null);
}
