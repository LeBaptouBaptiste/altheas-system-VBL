using Microsoft.AspNetCore.Mvc;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Models.Shared;

namespace API_Althea_systems.Controllers;

[ApiController]
[Route("api/shipping")]
public class ShippingController : ControllerBase
{
    [HttpGet("methods")]
    public ActionResult<IEnumerable<ShippingMethodDto>> GetMethods()
    {
        var methods = new List<ShippingMethodDto>
        {
            new(ShippingMethod.Standard, "Livraison standard", "Standard delivery", 15m, "5-7 jours ouvrés", "5-7 business days"),
            new(ShippingMethod.Express, "Livraison express", "Express delivery", 35m, "2-3 jours ouvrés", "2-3 business days"),
            new(ShippingMethod.Overnight, "Livraison 24h", "Next-day delivery", 75m, "Lendemain avant 12h", "Next day before noon")
        };

        return Ok(methods);
    }
}
