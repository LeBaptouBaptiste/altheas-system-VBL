using FluentAssertions;
using Moq;
using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Products;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services;

namespace API_Althea_systems.Tests.Services;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _categoryRepo = new();
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        _sut = new CategoryService(_categoryRepo.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingCategory_ReturnsDto()
    {
        var cat = CreateTestCategory();
        _categoryRepo.Setup(r => r.GetByIdAsync(cat.Id)).ReturnsAsync(cat);
        _categoryRepo.Setup(r => r.GetProductCountAsync(cat.Id)).ReturnsAsync(5);

        var result = await _sut.GetByIdAsync(cat.Id);

        result.Slug.Should().Be("test-cat");
        result.ProductCount.Should().Be(5);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistent_ThrowsNotFound()
    {
        _categoryRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Category?)null);

        var act = () => _sut.GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllCategories()
    {
        var categories = new List<Category> { CreateTestCategory() };
        _categoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);
        _categoryRepo.Setup(r => r.GetProductCountAsync(It.IsAny<Guid>())).ReturnsAsync(3);

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedCategory()
    {
        var request = new CategoryCreateRequest("new-cat", "Nouvelle", "New", null, null, "Desc FR", "Desc EN", null, null, "img", null, 1, true);
        _categoryRepo.Setup(r => r.CreateAsync(It.IsAny<Category>())).ReturnsAsync((Category c) => c);
        _categoryRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(CreateTestCategory());
        _categoryRepo.Setup(r => r.GetProductCountAsync(It.IsAny<Guid>())).ReturnsAsync(0);

        var result = await _sut.CreateAsync(request);

        result.Should().NotBeNull();
        _categoryRepo.Verify(r => r.CreateAsync(It.IsAny<Category>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistent_ThrowsNotFound()
    {
        _categoryRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Category?)null);

        var act = () => _sut.DeleteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static Category CreateTestCategory() => new()
    {
        Id = Guid.NewGuid(),
        Slug = "test-cat",
        NameFr = "Catégorie Test",
        NameEn = "Test Category",
        DescriptionFr = "Desc",
        DescriptionEn = "Desc",
        Image = "img",
        DisplayOrder = 1,
        Active = true,
        Children = []
    };
}
