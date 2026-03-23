using FluentAssertions;
using Moq;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Products;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services;

namespace API_Althea_systems.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _sut = new ProductService(_productRepo.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingProduct_ReturnsDto()
    {
        var product = CreateTestProduct();
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var result = await _sut.GetByIdAsync(product.Id);

        result.Slug.Should().Be("test-product");
        result.PriceHT.Should().Be(100m);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistent_ThrowsNotFound()
    {
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product?)null);

        var act = () => _sut.GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetBySlugAsync_ExistingSlug_ReturnsDto()
    {
        var product = CreateTestProduct();
        _productRepo.Setup(r => r.GetBySlugAsync("test-product")).ReturnsAsync(product);

        var result = await _sut.GetBySlugAsync("test-product");

        result.NameFr.Should().Be("Produit Test");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedResponse()
    {
        var products = new List<Product> { CreateTestProduct() };
        _productRepo.Setup(r => r.GetAllAsync(1, 12, null)).ReturnsAsync(products);
        _productRepo.Setup(r => r.CountAsync(null)).ReturnsAsync(1);

        var result = await _sut.GetAllAsync(1, 12);

        result.Data.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_ExistingProduct_CallsRepository()
    {
        var product = CreateTestProduct();
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        await _sut.DeleteAsync(product.Id);

        _productRepo.Verify(r => r.DeleteAsync(product), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistent_ThrowsNotFound()
    {
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product?)null);

        var act = () => _sut.DeleteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static Product CreateTestProduct() => new()
    {
        Id = Guid.NewGuid(),
        Slug = "test-product",
        NameFr = "Produit Test",
        NameEn = "Test Product",
        DescriptionFr = "Description",
        DescriptionEn = "Description",
        LongDescriptionFr = "Long",
        LongDescriptionEn = "Long",
        PriceHT = 100m,
        VatRate = VatRate.Standard,
        StockQty = 10,
        StockStatus = StockStatus.InStock,
        Status = ProductStatus.Active,
        Images = ["img-1"],
        ProductCategories = new List<ProductCategory>
        {
            new() { Category = new Category { Id = Guid.NewGuid(), Slug = "cat", NameFr = "Cat", NameEn = "Cat", DescriptionFr = "", DescriptionEn = "", Image = "" } }
        },
        Specs = new List<ProductSpec>
        {
            new() { Id = Guid.NewGuid(), Label = "Weight", Value = "5kg" }
        }
    };
}
