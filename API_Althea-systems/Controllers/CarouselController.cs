using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API_Althea_systems.Common.Auth;
using API_Althea_systems.Models.Content;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Controllers;

[ApiController]
[Route("api/carousel")]
public class CarouselController : ControllerBase
{
    private readonly IContentService _contentService;

    public CarouselController(IContentService contentService)
    {
        _contentService = contentService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<HeroSlideDto>>> GetAll([FromQuery] bool activeOnly = true)
    {
        return Ok(await _contentService.GetAllHeroSlidesAsync(activeOnly));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HeroSlideDto>> GetById(Guid id)
    {
        return Ok(await _contentService.GetHeroSlideByIdAsync(id));
    }

    [HttpPost]
    [Authorize(Roles = "Admin"), RequireStepUp(StepUpPurpose.Admin)]
    public async Task<ActionResult<HeroSlideDto>> Create([FromBody] HeroSlideCreateRequest request)
    {
        var slide = await _contentService.CreateHeroSlideAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = slide.Id }, slide);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin"), RequireStepUp(StepUpPurpose.Admin)]
    public async Task<ActionResult<HeroSlideDto>> Update(Guid id, [FromBody] HeroSlideUpdateRequest request)
    {
        return Ok(await _contentService.UpdateHeroSlideAsync(id, request));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin"), RequireStepUp(StepUpPurpose.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _contentService.DeleteHeroSlideAsync(id);
        return NoContent();
    }
}
