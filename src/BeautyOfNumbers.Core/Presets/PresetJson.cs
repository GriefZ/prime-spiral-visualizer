using System.Text.Json;
using System.Text.Json.Serialization;
using BeautyOfNumbers.Core.Layouts;
using BeautyOfNumbers.Core.Requests;
using BeautyOfNumbers.Core.Style;

namespace BeautyOfNumbers.Core.Presets;

public static class PresetJson
{
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static string Serialize(Preset preset) => JsonSerializer.Serialize(preset, Options);

    public static Preset Deserialize(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("schemaVersion", out JsonElement versionElement))
        {
            throw new InvalidDataException("Preset JSON is missing the required 'schemaVersion' property.");
        }

        int schemaVersion = versionElement.GetInt32();
        if (schemaVersion != CurrentSchemaVersion)
        {
            throw new NotSupportedException(
                $"Preset schema version {schemaVersion} is not supported; this build understands version {CurrentSchemaVersion}.");
        }

        Preset preset = JsonSerializer.Deserialize<Preset>(json, Options)
            ?? throw new InvalidDataException("Preset JSON is empty.");

        return preset with
        {
            Name = preset.Name ?? string.Empty,
            LayoutId = preset.LayoutId ?? ArchimedeanSpiral.LayoutId,
            Layout = preset.Layout ?? new LayoutOptions(),
            Style = preset.Style ?? RenderStyle.Default,
            Output = preset.Output ?? OutputOptions.Default,
        };
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };
        options.Converters.Add(new RgbaColorJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
