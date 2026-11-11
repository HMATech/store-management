using Microsoft.EntityFrameworkCore;

namespace Application.Common;

public record PagedResponse<TResponse>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<TResponse> Responses { get; set; } = new()!;

    public bool HasNextPage => Page * PageSize < TotalCount;
    public bool HasPreviousPage => Page > 1;

    protected PagedResponse(int page, int pageSize, int totalCount, List<TResponse> responses)
    {
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
        Responses = responses;
    }

    public static PagedResponse<TResponse> Create(int page, int pageSize, int totalCount, List<TResponse> responses)
        => new(page, pageSize, totalCount, responses);

    public async static Task<PagedResponse<TResponse>> CreateAsync(IQueryable<TResponse> query, int page, int pageSize)
    {
        int totalCount = await query.CountAsync();
        var responses = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new(page, pageSize, totalCount, responses);
    }
}