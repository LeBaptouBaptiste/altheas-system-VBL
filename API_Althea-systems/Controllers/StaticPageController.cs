using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API_Althea_systems.Models.Content;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Controllers;

[ApiController]
[Route("api/pages")]
public class StaticPageController : ControllerBase
{
    private readonly IContentService _contentService;

    public StaticPageController(IContentService contentService)
    {
        _contentService = contentService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StaticPageDto>>> GetAll()
    {
        return Ok(await _contentService.GetAllStaticPagesAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StaticPageDto>> GetById(Guid id)
    {
        return Ok(await _contentService.GetStaticPageByIdAsync(id));
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<StaticPageDto>> GetBySlug(string slug)
    {
        return Ok(await _contentService.GetStaticPageBySlugAsync(slug));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StaticPageDto>> Create([FromBody] StaticPageCreateRequest request)
    {
        var page = await _contentService.CreateStaticPageAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = page.Id }, page);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StaticPageDto>> Update(Guid id, [FromBody] StaticPageUpdateRequest request)
    {
        return Ok(await _contentService.UpdateStaticPageAsync(id, request));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _contentService.DeleteStaticPageAsync(id);
        return NoContent();
    }
}
