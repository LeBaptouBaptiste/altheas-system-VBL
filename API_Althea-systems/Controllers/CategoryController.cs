using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API_Althea_systems.Models.Products;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll()
    {
        return Ok(await _categoryService.GetAllAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> GetById(Guid id)
    {
        return Ok(await _categoryService.GetByIdAsync(id));
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<CategoryDto>> GetBySlug(string slug)
    {
        return Ok(await _categoryService.GetBySlugAsync(slug));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CategoryCreateRequest request)
    {
        var category = await _categoryService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoryDto>> Update(Guid id, [FromBody] CategoryUpdateRequest request)
    {
        return Ok(await _categoryService.UpdateAsync(id, request));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _categoryService.DeleteAsync(id);
        return NoContent();
    }
}
