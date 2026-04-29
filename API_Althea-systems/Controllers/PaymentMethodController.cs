using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API_Althea_systems.Common.Auth;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Controllers;

[ApiController]
[Route("api/users/{userId:guid}/payment-methods")]
[Authorize]
public class PaymentMethodController : ControllerBase
{
    private readonly IUserService _userService;

    public PaymentMethodController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task<ActionResult<PaymentMethodDto>> Create(Guid userId, [FromBody] PaymentMethodCreateRequest request)
    {
        HttpContext.RequireOwnershipOrAdmin(userId);
        return Created("", await _userService.AddPaymentMethodAsync(userId, request));
    }

    [HttpDelete("{paymentMethodId:guid}")]
    public async Task<IActionResult> Delete(Guid userId, Guid paymentMethodId)
    {
        HttpContext.RequireOwnershipOrAdmin(userId);
        await _userService.DeletePaymentMethodAsync(userId, paymentMethodId);
        return NoContent();
    }
}
