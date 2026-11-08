using System.Text.Json.Serialization;

namespace Application.Stocks;

/// <summary>How a product's stock compares with its stock definition.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StockStatus
{
    /// <summary>Above the minimum quantity.</summary>
    Normal,

    /// <summary>At or below the minimum quantity, but not negative.</summary>
    Low,

    /// <summary>Below zero, which means the records need checking.</summary>
    Negative,
}
