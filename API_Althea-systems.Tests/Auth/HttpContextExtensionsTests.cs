using FluentAssertions;
using Microsoft.AspNetCore.Http;
using API_Althea_systems.Common.Auth;
using API_Althea_systems.Common.Exceptions;

namespace API_Althea_systems.Tests.Auth;

public class HttpContextExtensionsTests
{
    private static HttpContext BuildContext(Guid? userId, string? role = null)
    {
        var http = new DefaultHttpContext();
        if (userId.HasValue) http.Items["UserId"] = userId.Value.ToString();
        if (role != null) http.Items["UserRole"] = role;
        return http;
    }

    [Fact]
    public void GetCurrentUserId_NoUser_ReturnsNull()
    {
        var http = BuildContext(userId: null);

        http.GetCurrentUserId().Should().BeNull();
    }

    [Fact]
    public void GetCurrentUserId_ValidGuid_Returns()
    {
        var id = Guid.NewGuid();
        var http = BuildContext(userId: id);

        http.GetCurrentUserId().Should().Be(id);
    }

    [Fact]
    public void IsCurrentUserAdmin_True_WhenRoleAdmin()
    {
        var http = BuildContext(userId: Guid.NewGuid(), role: "Admin");

        http.IsCurrentUserAdmin().Should().BeTrue();
    }

    [Fact]
    public void IsCurrentUserAdmin_False_WhenRoleCustomer()
    {
        var http = BuildContext(userId: Guid.NewGuid(), role: "Customer");

        http.IsCurrentUserAdmin().Should().BeFalse();
    }

    [Fact]
    public void RequireOwnershipOrAdmin_Owner_Passes()
    {
        var id = Guid.NewGuid();
        var http = BuildContext(userId: id, role: "Customer");

        var act = () => http.RequireOwnershipOrAdmin(id);

        act.Should().NotThrow();
    }

    [Fact]
    public void RequireOwnershipOrAdmin_Admin_BypassesOwnership()
    {
        var http = BuildContext(userId: Guid.NewGuid(), role: "Admin");

        var act = () => http.RequireOwnershipOrAdmin(Guid.NewGuid());

        act.Should().NotThrow();
    }

    [Fact]
    public void RequireOwnershipOrAdmin_OtherUser_ThrowsForbidden()
    {
        var http = BuildContext(userId: Guid.NewGuid(), role: "Customer");

        var act = () => http.RequireOwnershipOrAdmin(Guid.NewGuid());

        act.Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void RequireOwnershipOrAdmin_NoUser_ThrowsForbidden()
    {
        var http = BuildContext(userId: null);

        var act = () => http.RequireOwnershipOrAdmin(Guid.NewGuid());

        act.Should().Throw<ForbiddenException>();
    }
}
