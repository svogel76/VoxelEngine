using System.Text.Json.Serialization;

namespace VoxelEngine.Entity.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EntityTimeOfDayActivity
{
    Active,
    Sleep,
    Burrow
}
