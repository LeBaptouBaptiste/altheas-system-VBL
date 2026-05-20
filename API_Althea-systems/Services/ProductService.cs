using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Products;
using API_Althea_systems.Models.Shared;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto> GetByIdAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Product", id);
        return MapToDto(product);
    }

    public async Task<ProductDto> GetBySlugAsync(string slug)
    {
        var product = await _productRepository.GetBySlugAsync(slug)
            ?? throw new NotFoundException($"Product with slug '{slug}' was not found.");
        return MapToDto(product);
    }

    public async Task<PaginatedResponse<ProductDto>> GetAllAsync(int page, int pageSize, Guid? categoryId = null)
    {
        var products = await _productRepository.GetAllAsync(page, pageSize, categoryId);
        var total = await _productRepository.CountAsync(categoryId);
        return new PaginatedResponse<ProductDto>(
            products.Select(MapToDto), page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<PaginatedResponse<ProductDto>> SearchAsync(string query, int page, int pageSize)
    {
        var products = await _productRepository.SearchAsync(query, page, pageSize);
        var total = await _productRepository.SearchCountAsync(query);
        return new PaginatedResponse<ProductDto>(
            products.Select(MapToDto), page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<ProductDto> CreateAsync(ProductCreateRequest request)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Slug = request.Slug,
            NameFr = request.NameFr,
            NameEn = request.NameEn,
            NameMs = request.NameMs,
            NameAr = request.NameAr,
            DescriptionFr = request.DescriptionFr,
            DescriptionEn = request.DescriptionEn,
            DescriptionMs = request.DescriptionMs,
            DescriptionAr = request.DescriptionAr,
            LongDescriptionFr = request.LongDescriptionFr,
            LongDescriptionEn = request.LongDescriptionEn,
            LongDescriptionMs = request.LongDescriptionMs,
            LongDescriptionAr = request.LongDescriptionAr,
            PriceHT = request.PriceHT,
            VatRate = request.VatRate,
            StockQty = request.StockQty,
            StockStatus = request.StockStatus,
            IsNew = request.IsNew,
            PriorityRank = request.PriorityRank,
            Images = request.Images,
            Status = request.Status,
            ProductCategories = request.CategoryIds.Select(cId => new ProductCategory { CategoryId = cId }).ToList(),
            Specs = request.Specs.Select(s => new ProductSpec
            {
                Id = Guid.NewGuid(),
                Label = s.Label,
                Value = s.Value,
                LabelEn = s.LabelEn,
                LabelMs = s.LabelMs,
                LabelAr = s.LabelAr,
                ValueEn = s.ValueEn,
                ValueMs = s.ValueMs,
                ValueAr = s.ValueAr,
            }).ToList()
        };

        await _productRepository.CreateAsync(product);
        return await GetByIdAsync(product.Id);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, ProductUpdateRequest request)
    {
        var product = await _productRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Product", id);

        if (request.Slug != null) product.Slug = request.Slug;
        if (request.NameFr != null) product.NameFr = request.NameFr;
        if (request.NameEn != null) product.NameEn = request.NameEn;
        if (request.NameMs != null) product.NameMs = request.NameMs;
        if (request.NameAr != null) product.NameAr = request.NameAr;
        if (request.DescriptionFr != null) product.DescriptionFr = request.DescriptionFr;
        if (request.DescriptionEn != null) product.DescriptionEn = request.DescriptionEn;
        if (request.DescriptionMs != null) product.DescriptionMs = request.DescriptionMs;
        if (request.DescriptionAr != null) product.DescriptionAr = request.DescriptionAr;
        if (request.LongDescriptionFr != null) product.LongDescriptionFr = request.LongDescriptionFr;
        if (request.LongDescriptionEn != null) product.LongDescriptionEn = request.LongDescriptionEn;
        if (request.LongDescriptionMs != null) product.LongDescriptionMs = request.LongDescriptionMs;
        if (request.LongDescriptionAr != null) product.LongDescriptionAr = request.LongDescriptionAr;
        if (request.PriceHT.HasValue) product.PriceHT = request.PriceHT.Value;
        if (request.VatRate.HasValue) product.VatRate = request.VatRate.Value;
        if (request.StockQty.HasValue) product.StockQty = request.StockQty.Value;
        if (request.StockStatus.HasValue) product.StockStatus = request.StockStatus.Value;
        if (request.IsNew.HasValue) product.IsNew = request.IsNew.Value;
        if (request.PriorityRank.HasValue) product.PriorityRank = request.PriorityRank.Value;
        if (request.Images != null) product.Images = request.Images;
        if (request.Status.HasValue) product.Status = request.Status.Value;

        await _productRepository.UpdateAsync(product);
        return await GetByIdAsync(product.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Product", id);
        await _productRepository.DeleteAsync(product);
    }

    private static ProductDto MapToDto(Product p) => new(
        p.Id, p.Slug,
        p.NameFr, p.NameEn, p.NameMs, p.NameAr,
        p.DescriptionFr, p.DescriptionEn, p.DescriptionMs, p.DescriptionAr,
        p.LongDescriptionFr, p.LongDescriptionEn, p.LongDescriptionMs, p.LongDescriptionAr,
        p.PriceHT, p.VatRate,
        p.StockQty, p.StockStatus, p.IsNew, p.PriorityRank, p.Images, p.Status,
        p.CreatedAt, p.UpdatedAt,
        p.ProductCategories.Select(pc => new CategorySummaryDto(pc.Category.Id, pc.Category.Slug, pc.Category.NameFr, pc.Category.NameEn)),
        p.Specs.Select(s => new ProductSpecDto(
            s.Label, s.Value,
            s.LabelEn, s.LabelMs, s.LabelAr,
            s.ValueEn, s.ValueMs, s.ValueAr))
    );
}
