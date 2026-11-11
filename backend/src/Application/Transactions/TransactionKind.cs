using System.Text.Json.Serialization;

namespace Application.Transactions;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionKind
{
    Sale,
    Purchase,
}
