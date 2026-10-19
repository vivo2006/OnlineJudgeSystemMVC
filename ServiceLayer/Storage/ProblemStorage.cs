using System.Globalization;

namespace ServiceLayer.Storage;

public static class ProblemStorage
{
    public static string Segment(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 100 ||
            value is "." or ".." || value.Any(c => !char.IsLetterOrDigit(c) && c != '-' && c != '_'))
            throw new ArgumentException("Use letters, numbers, hyphens or underscores for storage identifiers.");
        return value;
    }

    public static string DirectoryFor(string root, string competition, int year, string notation) =>
        Path.Combine(Path.GetFullPath(root), Segment(competition), year.ToString(CultureInfo.InvariantCulture), Segment(notation));

    public static (string[] Inputs, string[] Outputs) TestPairs(string directory, int maximum)
    {
        var inputs = Directory.GetFiles(Path.Combine(directory, "Input"))
            .Where(p => Path.GetExtension(p).Equals(".in", StringComparison.OrdinalIgnoreCase))
            .OrderBy(Path.GetFileNameWithoutExtension, StringComparer.Ordinal).ToArray();
        var outputs = Directory.GetFiles(Path.Combine(directory, "Output"))
            .Where(p => Path.GetExtension(p).Equals(".sol", StringComparison.OrdinalIgnoreCase) ||
                        Path.GetExtension(p).Equals(".out", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (inputs.Length == 0 || inputs.Length > maximum || inputs.Length != outputs.Length)
            throw new InvalidOperationException($"Provide 1–{maximum} matching input/output test pairs. Tests are never silently truncated.");
        var outputMap = outputs.ToDictionary(p => Path.GetFileNameWithoutExtension(p), StringComparer.Ordinal);
        if (inputs.Any(p => !outputMap.ContainsKey(Path.GetFileNameWithoutExtension(p))))
            throw new InvalidOperationException("Each input must have an output with the same test name.");
        return (inputs, inputs.Select(p => outputMap[Path.GetFileNameWithoutExtension(p)]).ToArray());
    }
}
