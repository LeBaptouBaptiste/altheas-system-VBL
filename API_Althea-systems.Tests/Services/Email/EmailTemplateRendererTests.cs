using API_Althea_systems.Services.Email;
using FluentAssertions;

namespace API_Althea_systems.Tests.Services.Email;

/// <summary>
/// The renderer's job is dead simple — substitute {{placeholder}} tokens —
/// but the failure modes matter: silent passthrough of an unknown placeholder
/// would mean "{{firstName}}" lands in a real customer's inbox. We test the
/// guardrails as much as the happy path.
/// </summary>
public class EmailTemplateRendererTests
{
    private static EmailTemplateRenderer Build(Dictionary<string, string> templates)
        => new(templates);

    [Fact]
    public void Render_SubstitutesSinglePlaceholder()
    {
        var sut = Build(new Dictionary<string, string>
        {
            ["welcome"] = "<p>Bonjour {{firstName}}</p>"
        });

        var html = sut.Render("welcome",
            new Dictionary<string, string> { ["firstName"] = "Baptiste" });

        html.Should().Be("<p>Bonjour Baptiste</p>");
    }

    [Fact]
    public void Render_SubstitutesAllOccurrencesOfTheSamePlaceholder()
    {
        // Same key referenced twice (e.g. greeting + footer) → both must
        // be replaced. Regex.Replace covers this by default but we lock it
        // in to prevent a regression to "first match only".
        var sut = Build(new Dictionary<string, string>
        {
            ["confirm"] = "Hello {{name}}, see you {{name}}."
        });

        var html = sut.Render("confirm",
            new Dictionary<string, string> { ["name"] = "X" });

        html.Should().Be("Hello X, see you X.");
    }

    [Fact]
    public void Render_LeavesNonPlaceholderBracesUntouched()
    {
        // A single brace or a {non-word} chunk is NOT a placeholder. We must
        // not eat user content that happens to contain curly braces (CSS,
        // JSON snippets in transactional mails, etc.).
        var sut = Build(new Dictionary<string, string>
        {
            ["css"] = "body { color: red; } and {{name}} stays"
        });

        var html = sut.Render("css",
            new Dictionary<string, string> { ["name"] = "ok" });

        html.Should().Be("body { color: red; } and ok stays");
    }

    [Fact]
    public void Render_MissingTemplate_Throws()
    {
        var sut = Build(new Dictionary<string, string>
        {
            ["welcome"] = "<p>Hi</p>"
        });

        var act = () => sut.Render("does-not-exist",
            new Dictionary<string, string>());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*does-not-exist*not loaded*");
    }

    [Fact]
    public void Render_MissingPlaceholder_Throws()
    {
        // We deliberately FAIL CLOSED on missing placeholders — a silent
        // passthrough would ship literal "{{firstName}}" to the customer.
        var sut = Build(new Dictionary<string, string>
        {
            ["welcome"] = "<p>Bonjour {{firstName}}</p>"
        });

        var act = () => sut.Render("welcome", new Dictionary<string, string>());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*firstName*");
    }

    [Fact]
    public void Render_EmptyPlaceholderValue_IsSubstituted()
    {
        // Empty string is a valid value (not null) — render to "".
        var sut = Build(new Dictionary<string, string>
        {
            ["welcome"] = "Hi {{company}}!"
        });

        var html = sut.Render("welcome",
            new Dictionary<string, string> { ["company"] = "" });

        html.Should().Be("Hi !");
    }

    [Fact]
    public void Render_TemplateNameLookupIsCaseInsensitive()
    {
        // Convenience — keys stored case-insensitively means callers don't
        // have to remember the on-disk filename casing.
        var sut = Build(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Welcome"] = "<p>Hi</p>"
        });

        sut.Render("welcome", new Dictionary<string, string>())
            .Should().Be("<p>Hi</p>");
    }
}
