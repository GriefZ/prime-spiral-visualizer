using BeautyOfNumbers.Core.Layouts;
using BeautyOfNumbers.Core.Presets;
using BeautyOfNumbers.Core.Requests;
using BeautyOfNumbers.Core.Style;
using Xunit;

namespace BeautyOfNumbers.Core.Tests;

public sealed class PresetJsonTests
{
    [Fact]
    public void RoundTrip_PresetToJsonToPreset_PreservesValues()
    {
        Preset original = CreatePreset();

        Preset restored = PresetJson.Deserialize(PresetJson.Serialize(original));

        Assert.Equal(original.SchemaVersion, restored.SchemaVersion);
        Assert.Equal(original.Name, restored.Name);
        Assert.Equal(original.Range, restored.Range);
        Assert.Equal(original.ShowOnlyPrimes, restored.ShowOnlyPrimes);
        Assert.Equal(original.LayoutId, restored.LayoutId);
        Assert.Equal(original.Layout, restored.Layout);
        Assert.Equal(original.Style.Background, restored.Style.Background);
        Assert.Equal(original.Style.Palette, restored.Style.Palette);
        Assert.Equal(original.Style.PointSize, restored.Style.PointSize);
        Assert.Equal(original.Style.Antialias, restored.Style.Antialias);
        Assert.Equal(original.Output, restored.Output);
    }

    [Fact]
    public void Serialize_AfterRoundTrip_ProducesIdenticalJson()
    {
        string json = PresetJson.Serialize(CreatePreset());

        string secondJson = PresetJson.Serialize(PresetJson.Deserialize(json));

        Assert.Equal(json, secondJson);
    }

    [Fact]
    public void Deserialize_WhenSchemaVersionIsUnknown_ThrowsNotSupportedException()
    {
        const string json = """
            {
              "schemaVersion": 99,
              "range": { "start": 1, "count": 10 }
            }
            """;

        Assert.Throws<NotSupportedException>(() => PresetJson.Deserialize(json));
    }

    [Fact]
    public void Deserialize_WhenSchemaVersionIsMissing_ThrowsInvalidDataException()
    {
        const string json = """
            {
              "range": { "start": 1, "count": 10 }
            }
            """;

        Assert.Throws<InvalidDataException>(() => PresetJson.Deserialize(json));
    }

    [Fact]
    public void Deserialize_WhenOptionalFieldsAreMissing_UsesDefaults()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "range": { "start": 1, "count": 10 }
            }
            """;

        Preset preset = PresetJson.Deserialize(json);

        Assert.Equal(string.Empty, preset.Name);
        Assert.Equal(ArchimedeanSpiral.LayoutId, preset.LayoutId);
        Assert.Equal(new LayoutOptions(), preset.Layout);
        Assert.Equal(RenderStyle.Default.Background, preset.Style.Background);
        Assert.Equal(RenderStyle.Default.PointSize, preset.Style.PointSize);
        Assert.Equal(RenderStyle.Default.Antialias, preset.Style.Antialias);
        Assert.Equal(RenderStyle.Default.Palette, preset.Style.Palette);
        Assert.Equal(OutputOptions.Default, preset.Output);
        Assert.False(preset.ShowOnlyPrimes);
    }

    private static Preset CreatePreset() => new(
        PresetJson.CurrentSchemaVersion,
        "Test preset",
        new NumberRange(1, 1000),
        true,
        ArchimedeanSpiral.LayoutId,
        new LayoutOptions(0.5, true, 2.0),
        RenderStyle.Default,
        new OutputOptions(800, 600, 144));
}
