using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API_Althea_systems.Common.Auth;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Controllers;

[ApiController]
[Route("api/users/{userId:guid}/addresses")]
[Authorize]
public class AddressController : ControllerBase
{
    private readonly IUserService _userService;

    public AddressController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task<ActionResult<AddressDto>> Create(Guid userId, [FromBody] AddressCreateRequest request)
    {
        HttpContext.RequireOwnershipOrAdmin(userId);
        return Created("", await _userService.AddAddressAsync(userId, request));
    }

    [HttpPut("{addressId:guid}")]
    public async Task<ActionResult<AddressDto>> Update(Guid userId, Guid addressId, [FromBody] AddressCreateRequest request)
    {
        HttpContext.RequireOwnershipOrAdmin(userId);
        return Ok(await _userService.UpdateAddressAsync(userId, addressId, request));
    }

    [HttpDelete("{addressId:guid}")]
    public async Task<IActionResult> Delete(Guid userId, Guid addressId)
    {
        HttpContext.RequireOwnershipOrAdmin(userId);
        await _userService.DeleteAddressAsync(userId, addressId);
        return NoContent();
    }
}
