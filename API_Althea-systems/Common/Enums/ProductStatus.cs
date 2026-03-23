using System.Text.Json.Serialization;

namespace API_Althea_systems.Common.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProductStatus
{
    Active,
    Draft,
    Archived
}
