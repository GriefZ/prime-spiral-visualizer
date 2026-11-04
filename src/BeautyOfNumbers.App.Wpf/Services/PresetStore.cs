using System.IO;
using System.Text;
using System.Text.Json;
using BeautyOfNumbers.Core.Presets;

namespace BeautyOfNumbers.App.Wpf.Services;

internal sealed record PresetEntry(Preset Preset, string FilePath)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Preset.Name)
        ? Path.GetFileNameWithoutExtension(FilePath)
        : Preset.Name;
}

internal sealed record PresetLoadError(string FilePath, string Message);

internal sealed record PresetScanResult(
    IReadOnlyList<PresetEntry> Entries,
    IReadOnlyList<PresetLoadError> Errors);

internal sealed class PresetStore
{
    private const int MaxFileNameLength = 80;
    private const string PresetExtension = ".json";

    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    public PresetStore(string? directory = null)
    {
        DirectoryPath = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BeautyOfNumbers",
            "presets");
    }

    public string DirectoryPath { get; }

    public PresetScanResult LoadAll()
    {
        if (!Directory.Exists(DirectoryPath))
        {
            return new PresetScanResult([], []);
        }

        List<PresetEntry> entries = [];
        List<PresetLoadError> errors = [];

        List<string> paths;
        try
        {
            paths = [.. Directory.EnumerateFiles(DirectoryPath, "*" + PresetExtension)];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new PresetScanResult([], [new PresetLoadError(DirectoryPath, ex.Message)]);
        }

        foreach (string path in paths)
        {
            try
            {
                string json = File.ReadAllText(path);
                Preset preset = PresetJson.Deserialize(json);
                entries.Add(new PresetEntry(preset, path));
            }
            catch (Exception ex) when (ex is JsonException
                or InvalidDataException
                or NotSupportedException
                or IOException
                or UnauthorizedAccessException
                or InvalidOperationException
                or FormatException)
            {
                errors.Add(new PresetLoadError(path, ex.Message));
            }
        }

        entries.Sort((left, right) => string.Compare(
            left.DisplayName,
            right.DisplayName,
            StringComparison.OrdinalIgnoreCase));

        return new PresetScanResult(entries, errors);
    }

    public PresetEntry Save(Preset preset)
    {
        Directory.CreateDirectory(DirectoryPath);
        string path = Path.Combine(DirectoryPath, FileNameFor(preset.Name));
        string tempPath = path + ".tmp";
        bool saved = false;

        try
        {
            File.WriteAllText(tempPath, PresetJson.Serialize(preset), new UTF8Encoding(false));
            File.Move(tempPath, path, overwrite: true);
            saved = true;
        }
        finally
        {
            if (!saved)
            {
                DeleteIfExists(tempPath);
            }
        }

        return new PresetEntry(preset, path);
    }

    public void Delete(PresetEntry entry)
    {
        string path = Path.Combine(DirectoryPath, Path.GetFileName(entry.FilePath));
        File.Delete(path);
    }

    public static string FileNameFor(string name)
    {
        char[] invalidCharacters = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(name.Length);
        bool hasValidCharacter = false;

        foreach (char character in name.Trim())
        {
            if (Array.IndexOf(invalidCharacters, character) >= 0)
            {
                builder.Append('-');
                continue;
            }

            builder.Append(character);
            hasValidCharacter = true;
        }

        if (!hasValidCharacter)
        {
            return "preset" + PresetExtension;
        }

        string stem = builder.ToString().Trim().TrimEnd('.', ' ');
        if (stem.Length == 0)
        {
            return "preset" + PresetExtension;
        }

        if (ReservedNames.Contains(stem.Split('.')[0]))
        {
            return "preset-" + stem + PresetExtension;
        }

        if (stem.Length > MaxFileNameLength)
        {
            stem = stem[..MaxFileNameLength].TrimEnd('.', ' ');
        }

        return stem.Length == 0 ? "preset" + PresetExtension : stem + PresetExtension;
    }

    private static void DeleteIfExists(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
