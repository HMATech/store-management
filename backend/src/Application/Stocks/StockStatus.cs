using System.Text.Json.Serialization;

namespace Application.Stocks;

/// <summary>How a product's stock compares with its stock definition.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StockStatus
{
    /// <summary>Above the minimum quantity.</summary>
    Normal,

    /// <summary>Above zero, up to and including the minimum quantity: running out.</summary>
    Low,

    /// <summary>Exactly zero: nothing left to sell.</summary>
    Empty,

    /// <summary>Below zero, which means the records need checking.</summary>
    Negative,
}
