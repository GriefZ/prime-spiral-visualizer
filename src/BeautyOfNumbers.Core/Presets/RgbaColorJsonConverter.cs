using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using BeautyOfNumbers.Core.Colors;

namespace BeautyOfNumbers.Core.Presets;

internal sealed class RgbaColorJsonConverter : JsonConverter<RgbaColor>
{
    public override RgbaColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string text = reader.GetString() ?? throw new JsonException("Color value must be a hex string.");
        ReadOnlySpan<char> hex = text.AsSpan();
        if (hex.Length > 0 && hex[0] == '#')
        {
            hex = hex[1..];
        }

        byte alpha = 255;
        if (hex.Length == 8)
        {
            alpha = ParseByte(hex[..2], text);
            hex = hex[2..];
        }

        if (hex.Length != 6)
        {
            throw new JsonException($"Color '{text}' must use '#RRGGBB' or '#AARRGGBB' format.");
        }

        return new RgbaColor(
            ParseByte(hex[..2], text),
            ParseByte(hex.Slice(2, 2), text),
            ParseByte(hex.Slice(4, 2), text),
            alpha);
    }

    public override void Write(Utf8JsonWriter writer, RgbaColor value, JsonSerializerOptions options)
    {
        string hex = value.A == 255
            ? $"#{value.R:X2}{value.G:X2}{value.B:X2}"
            : $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}";
        writer.WriteStringValue(hex);
    }

    private static byte ParseByte(ReadOnlySpan<char> hex, string source) =>
        byte.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte value)
            ? value
            : throw new JsonException($"Color '{source}' contains invalid hexadecimal digits.");
}
