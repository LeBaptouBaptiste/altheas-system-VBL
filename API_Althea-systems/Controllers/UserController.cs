using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API_Althea_systems.Common.Auth;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Models.Shared;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin"), RequireStepUp(StepUpPurpose.Admin)]
    public async Task<ActionResult<PaginatedResponse<UserDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 12)
    {
        return Ok(await _userService.GetAllAsync(page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        HttpContext.RequireOwnershipOrAdmin(id);
        return Ok(await _userService.GetByIdAsync(id));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UserUpdateRequest request)
    {
        HttpContext.RequireOwnershipOrAdmin(id);
        return Ok(await _userService.UpdateAsync(id, request));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin"), RequireStepUp(StepUpPurpose.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _userService.DeleteAsync(id);
        return NoContent();
    }

    // Anonymize is the GDPR equivalent of "delete my account" — sensitive,
    // hence gated by an action step-up. NOTE: dedicated change-password and
    // change-email endpoints don't exist yet; when added (V2), they should
    // also carry [RequireStepUp(StepUpPurpose.Action)].
    [HttpPost("{id:guid}/anonymize")]
    [RequireStepUp(StepUpPurpose.Action)]
    public async Task<ActionResult<UserDto>> Anonymize(Guid id)
    {
        HttpContext.RequireOwnershipOrAdmin(id);
        return Ok(await _userService.AnonymizeAsync(id));
    }
}
