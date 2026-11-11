using Application.Abstractions.Messaging;
using Application.Common;

namespace Application.Sales.Get;

public sealed record GetSaleQuery(string? Search, SortOrder SortOrder,
    DateOnly? OccurenceFrom, DateOnly? OccurenceTo,
    decimal? TotalFrom, decimal? TotalTo,
    int Page = 1, int PageSize = 10)
    : IQuery<PagedResponse<SaleResponse>>
{
    public override string ToString()
    {
        return $"{(string.IsNullOrEmpty(Search) ? "" : $"Search: {Search}")}. " +
            $"SortOrder: {SortOrder}, " +
            $"{(OccurenceFrom.HasValue ? $"OccurenceFrom: {OccurenceFrom}, " : "")}" +
            $"{(OccurenceTo.HasValue ? $"OccurenceTo: {OccurenceTo}, " : "")}" +
            $"{(TotalFrom.HasValue ? $"TotalFrom: {TotalFrom}, " : "")}" +
            $"{(TotalTo.HasValue ? $"TotalTo: {TotalTo}, " : "")}" +
            $"Page: {Page}, PageSize: {PageSize}";
    }
}