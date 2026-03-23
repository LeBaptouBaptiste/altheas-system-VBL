using System.Text.Json.Serialization;

namespace API_Althea_systems.Common.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ShippingMethod
{
    /// <summary>5-7 days, €15</summary>
    Standard,

    /// <summary>2-3 days, €35</summary>
    Express,

    /// <summary>Next day, €75</summary>
    Overnight
}
