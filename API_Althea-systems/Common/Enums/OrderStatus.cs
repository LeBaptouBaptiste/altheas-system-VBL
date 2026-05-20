using System.Text.Json.Serialization;

namespace API_Althea_systems.Common.Enums;

// Order MUST match front/src/lib/enums.ts. The .NET integer index is what
// crosses the wire when the client sends `{"status": N}` — adding or removing
// a value silently breaks every PUT /orders/{id}/status. Stored as a string
// in PostgreSQL (OrderConfiguration.HasConversion<string>()), so adding new
// names doesn't need a migration.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    Pending,    // 0
    Confirmed,  // 1
    Processing, // 2
    Shipped,    // 3
    Delivered,  // 4
    Cancelled,  // 5
    Returned    // 6
}
