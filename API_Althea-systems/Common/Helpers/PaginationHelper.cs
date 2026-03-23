using API_Althea_systems.Models.Shared;

namespace API_Althea_systems.Common.Helpers;

public static class PaginationHelper
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 12;
    public const int MaxPageSize = 100;

    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        return (page, pageSize);
    }

    public static int CalculateTotalPages(int totalCount, int pageSize)
        => totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

    public static int CalculateSkip(int page, int pageSize)
        => (page - 1) * pageSize;

    public static PaginatedResponse<T> CreateResponse<T>(
        IEnumerable<T> data, int page, int pageSize, int totalCount)
    {
        return new PaginatedResponse<T>(
            data, page, pageSize, totalCount, CalculateTotalPages(totalCount, pageSize));
    }

    public static PaginatedResponse<TDto> CreateResponse<TEntity, TDto>(
        IEnumerable<TEntity> data, int page, int pageSize, int totalCount, Func<TEntity, TDto> mapper)
    {
        return new PaginatedResponse<TDto>(
            data.Select(mapper), page, pageSize, totalCount, CalculateTotalPages(totalCount, pageSize));
    }
}
