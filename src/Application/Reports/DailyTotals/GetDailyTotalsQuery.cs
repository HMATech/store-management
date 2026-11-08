using Application.Abstractions.Messaging;

namespace Application.Reports.DailyTotals;

/// <summary>
/// Totals of sales and purchases for each day from <paramref name="From"/> to <paramref name="To"/>, both included.
/// Days are calendar days in <paramref name="TimeZone"/> (an IANA id such as <c>Asia/Jakarta</c>), because
/// occurrence times are stored in UTC and a store's day doesn't start at UTC midnight.
/// </summary>
public sealed record GetDailyTotalsQuery(DateOnly From, DateOnly To, string TimeZone)
    : IQuery<DailyTotalsResponse>
{
    public override string ToString()
    {
        return $"From: {From}, To: {To}, TimeZone: {TimeZone}";
    }
}
