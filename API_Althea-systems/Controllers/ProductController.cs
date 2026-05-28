using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API_Althea_systems.Common.Auth;
using API_Althea_systems.Models.Products;
using API_Althea_systems.Models.Shared;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Controllers;

[ApiController]
[Route("api/products")]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ProductDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 12, [FromQuery] Guid? categoryId = null)
    {
        return Ok(await _productService.GetAllAsync(page, pageSize, categoryId));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id)
    {
        return Ok(await _productService.GetByIdAsync(id));
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<ProductDto>> GetBySlug(string slug)
    {
        return Ok(await _productService.GetBySlugAsync(slug));
    }

    [HttpGet("search")]
    public async Task<ActionResult<PaginatedResponse<ProductDto>>> Search(
        [FromQuery] string q = "", [FromQuery] int page = 1, [FromQuery] int pageSize = 12)
    {
        return Ok(await _productService.SearchAsync(q, page, pageSize));
    }

    [HttpPost]
    [Authorize(Roles = "Admin"), RequireStepUp(StepUpPurpose.Admin)]
    public async Task<ActionResult<ProductDto>> Create([FromBody] ProductCreateRequest request)
    {
        var product = await _productService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin"), RequireStepUp(StepUpPurpose.Admin)]
    public async Task<ActionResult<ProductDto>> Update(Guid id, [FromBody] ProductUpdateRequest request)
    {
        return Ok(await _productService.UpdateAsync(id, request));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin"), RequireStepUp(StepUpPurpose.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _productService.DeleteAsync(id);
        return NoContent();
    }
}
