using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using API_Althea_systems.Common.Auth;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Tests.Auth;

public class RequireStepUpAttributeTests
{
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IStepUpConsumptionStore> _consumption = new();
    private readonly IServiceProvider _services;

    public RequireStepUpAttributeTests()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton(_tokenService.Object);
        sc.AddSingleton(_consumption.Object);
        _services = sc.BuildServiceProvider();
    }

    private AuthorizationFilterContext BuildContext(
        Guid? authedUserId,
        string? stepUpHeader)
    {
        var http = new DefaultHttpContext { RequestServices = _services };
        if (authedUserId.HasValue)
            http.Items["UserId"] = authedUserId.Value.ToString();
        if (stepUpHeader != null)
            http.Request.Headers["X-Step-Up-Token"] = stepUpHeader;

        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    private static void AssertStepUpRequired(AuthorizationFilterContext ctx)
    {
        ctx.Result.Should().BeOfType<ObjectResult>();
        var obj = (ObjectResult)ctx.Result!;
        obj.StatusCode.Should().Be(403);
        obj.Value.Should().BeEquivalentTo(new { reason = "step_up_required" });
    }

    [Fact]
    public async Task NoAuthenticatedUser_Returns403()
    {
        var sut = new RequireStepUpAttribute(StepUpPurpose.Action);
        var ctx = BuildContext(authedUserId: null, stepUpHeader: "anything");

        await sut.OnAuthorizationAsync(ctx);

        AssertStepUpRequired(ctx);
    }

    [Fact]
    public async Task NoStepUpHeader_Returns403()
    {
        var sut = new RequireStepUpAttribute(StepUpPurpose.Action);
        var ctx = BuildContext(authedUserId: Guid.NewGuid(), stepUpHeader: null);

        await sut.OnAuthorizationAsync(ctx);

        AssertStepUpRequired(ctx);
    }

    [Fact]
    public async Task EmptyStepUpHeader_Returns403()
    {
        var sut = new RequireStepUpAttribute(StepUpPurpose.Action);
        var ctx = BuildContext(authedUserId: Guid.NewGuid(), stepUpHeader: "   ");

        await sut.OnAuthorizationAsync(ctx);

        AssertStepUpRequired(ctx);
    }

    [Fact]
    public async Task TokenInvalid_Returns403()
    {
        _tokenService.Setup(t => t.ValidateStepUpToken("bad", StepUpPurpose.Action))
                     .Returns((StepUpTokenValidation?)null);

        var sut = new RequireStepUpAttribute(StepUpPurpose.Action);
        var ctx = BuildContext(authedUserId: Guid.NewGuid(), stepUpHeader: "bad");

        await sut.OnAuthorizationAsync(ctx);

        AssertStepUpRequired(ctx);
    }

    [Fact]
    public async Task TokenIssuedForOtherUser_Returns403()
    {
        var authed = Guid.NewGuid();
        var other = Guid.NewGuid();
        _tokenService.Setup(t => t.ValidateStepUpToken("token", StepUpPurpose.Action))
                     .Returns(new StepUpTokenValidation(other, "jti-1"));

        var sut = new RequireStepUpAttribute(StepUpPurpose.Action);
        var ctx = BuildContext(authedUserId: authed, stepUpHeader: "token");

        await sut.OnAuthorizationAsync(ctx);

        AssertStepUpRequired(ctx);
    }

    [Fact]
    public async Task ActionToken_FirstUse_LetsThrough_AndConsumesJti()
    {
        var authed = Guid.NewGuid();
        _tokenService.Setup(t => t.ValidateStepUpToken("token", StepUpPurpose.Action))
                     .Returns(new StepUpTokenValidation(authed, "jti-1"));
        _consumption.Setup(s => s.TryConsumeAsync("jti-1", It.IsAny<TimeSpan>()))
                    .ReturnsAsync(true);

        var sut = new RequireStepUpAttribute(StepUpPurpose.Action);
        var ctx = BuildContext(authedUserId: authed, stepUpHeader: "token");

        await sut.OnAuthorizationAsync(ctx);

        ctx.Result.Should().BeNull(); // null result = filter let the request proceed
        _consumption.Verify(s => s.TryConsumeAsync("jti-1", It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task ActionToken_SecondUse_Returns403()
    {
        var authed = Guid.NewGuid();
        _tokenService.Setup(t => t.ValidateStepUpToken("token", StepUpPurpose.Action))
                     .Returns(new StepUpTokenValidation(authed, "jti-1"));
        _consumption.Setup(s => s.TryConsumeAsync("jti-1", It.IsAny<TimeSpan>()))
                    .ReturnsAsync(false); // already consumed

        var sut = new RequireStepUpAttribute(StepUpPurpose.Action);
        var ctx = BuildContext(authedUserId: authed, stepUpHeader: "token");

        await sut.OnAuthorizationAsync(ctx);

        AssertStepUpRequired(ctx);
    }

    [Fact]
    public async Task AdminToken_LetsThrough_WithoutConsumingJti()
    {
        // Admin tokens are reusable — consumption store must NOT be called.
        var authed = Guid.NewGuid();
        _tokenService.Setup(t => t.ValidateStepUpToken("admin-token", StepUpPurpose.Admin))
                     .Returns(new StepUpTokenValidation(authed, "jti-admin"));

        var sut = new RequireStepUpAttribute(StepUpPurpose.Admin);
        var ctx = BuildContext(authedUserId: authed, stepUpHeader: "admin-token");

        await sut.OnAuthorizationAsync(ctx);

        ctx.Result.Should().BeNull();
        _consumption.Verify(s => s.TryConsumeAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
    }
}
