using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeTaskManager.Api.Tests;

/// <summary>Shared JSON options for integration tests, mirroring Program.cs's enum-as-string converter so response bodies deserialize consistently.</summary>
internal static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
