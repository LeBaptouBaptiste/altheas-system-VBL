using System.Text.Json.Serialization;

namespace API_Althea_systems.Common.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentStatus
{
    Pending,
    Validated,
    Failed,
    Refunded
}
