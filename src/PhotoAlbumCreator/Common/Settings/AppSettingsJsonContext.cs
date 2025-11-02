using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhotoAlbumCreator.Common.Settings;

[JsonSourceGenerationOptions(
    ReadCommentHandling = JsonCommentHandling.Skip,
    WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class AppSettingsJsonContext : JsonSerializerContext
{
    public static AppSettingsJsonContext CreateJsonContext()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DictionaryKeyPolicy = null,

            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,

            WriteIndented = true,

            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
        };

        return new AppSettingsJsonContext(options);
    }
}
