using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Controllers;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Tests.Controllers;

/// <summary>
/// Verifies the ownership / admin gate added on top of the user-scoped
/// endpoints. We don't need a full MVC pipeline — invoking the action
/// directly against a synthetic <see cref="HttpContext"/> exercises the
/// extension <c>RequireOwnershipOrAdmin</c> path that sits before the
/// service call.
/// </summary>
public class UserControllerTests
{
    private readonly Mock<IUserService> _userService = new();

    private static UserController BuildSut(IUserService svc, Guid? authedUserId, string role = "Customer")
    {
        var controller = new UserController(svc);
        var http = new DefaultHttpContext();
        if (authedUserId.HasValue)
            http.Items["UserId"] = authedUserId.Value.ToString();
        http.Items["UserRole"] = role;
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        return controller;
    }

    [Fact]
    public async Task GetById_OtherUser_ThrowsForbidden()
    {
        var caller = Guid.NewGuid();
        var target = Guid.NewGuid();
        var sut = BuildSut(_userService.Object, caller, role: "Customer");

        var act = () => sut.GetById(target);

        await act.Should().ThrowAsync<ForbiddenException>();
        _userService.Verify(s => s.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetById_OwnId_CallsService()
    {
        var id = Guid.NewGuid();
        var dto = new UserDto(id, "n", "e@e.com", UserRole.Customer, UserStatus.Active,
            false, true, false, null, DateTime.UtcNow, [], []);
        _userService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(dto);
        var sut = BuildSut(_userService.Object, id, role: "Customer");

        var result = await sut.GetById(id);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_AdminAnyId_CallsService()
    {
        var target = Guid.NewGuid();
        var dto = new UserDto(target, "n", "e@e.com", UserRole.Customer, UserStatus.Active,
            false, true, false, null, DateTime.UtcNow, [], []);
        _userService.Setup(s => s.GetByIdAsync(target)).ReturnsAsync(dto);
        var sut = BuildSut(_userService.Object, Guid.NewGuid(), role: "Admin");

        var result = await sut.GetById(target);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_OtherUser_ThrowsForbidden()
    {
        var caller = Guid.NewGuid();
        var target = Guid.NewGuid();
        var sut = BuildSut(_userService.Object, caller, role: "Customer");
        var req = new UserUpdateRequest("Name", "e@e.com", null);

        var act = () => sut.Update(target, req);

        await act.Should().ThrowAsync<ForbiddenException>();
        _userService.Verify(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UserUpdateRequest>()), Times.Never);
    }

    [Fact]
    public async Task Anonymize_OtherUser_ThrowsForbidden()
    {
        var caller = Guid.NewGuid();
        var target = Guid.NewGuid();
        var sut = BuildSut(_userService.Object, caller, role: "Customer");

        var act = () => sut.Anonymize(target);

        await act.Should().ThrowAsync<ForbiddenException>();
        _userService.Verify(s => s.AnonymizeAsync(It.IsAny<Guid>()), Times.Never);
    }
}
