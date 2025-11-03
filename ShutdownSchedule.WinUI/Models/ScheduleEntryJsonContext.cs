using System.Text.Json.Serialization;

namespace ShutdownSchedule.WinUI.Models
{
    [JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(ScheduleEntry))]
    internal partial class ScheduleEntryJsonContext : JsonSerializerContext
    {
    }
}
