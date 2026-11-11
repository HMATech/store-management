using Application.Abstractions.Messaging;
using Application.Common;

namespace Application.Transactions.Get;

/// <summary>
/// Sales and purchases in one list. <paramref name="From"/> and <paramref name="To"/> are calendar days in
/// <paramref name="TimeZone"/>, both included, and either can be left out.
/// </summary>
/// <param name="Kind">Keeps only sales or only purchases (a <see cref="TransactionKind"/> name, ignoring case).</param>
public sealed record GetTransactionQuery(SortOrder SortOrder, string? Kind, DateOnly? From, DateOnly? To,
    string TimeZone, int Page = 1, int PageSize = 10)
    : IQuery<PagedResponse<TransactionSummaryResponse>>
{
    public override string ToString()
    {
        return $"SortOrder: {SortOrder}, " +
            $"{(string.IsNullOrEmpty(Kind) ? "" : $"Kind: {Kind}, ")}" +
            $"{(From.HasValue ? $"From: {From}, " : "")}" +
            $"{(To.HasValue ? $"To: {To}, " : "")}" +
            $"TimeZone: {TimeZone}, Page: {Page}, PageSize: {PageSize}";
    }
}
