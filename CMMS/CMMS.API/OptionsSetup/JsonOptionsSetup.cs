using System.Text.Json.Serialization;
using System.Text.Json;

namespace CMMS.API.OptionsSetup
{
    public static class JsonOptionsSetup
    {
        public static JsonSerializerOptions DefaultOptions { get; } = new JsonSerializerOptions
        {
            MaxDepth = 64,
            WriteIndented = true
        };
    }
}
