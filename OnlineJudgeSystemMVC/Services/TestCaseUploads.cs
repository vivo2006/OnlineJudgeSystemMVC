using ServiceLayer.Storage;

namespace OnlineJudgeSystemMVC.Services;

public static class TestCaseUploads
{
    public static void Validate(IFormFile[] files, int maximum)
    {
        if (files.Length == 0 || files.Length > maximum * 2 || files.Sum(f => f.Length) > 20 * 1024 * 1024)
            throw new ArgumentException("Upload matching test pairs up to the configured limit (20 MB total).");
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var inputs = new HashSet<string>(StringComparer.Ordinal);
        var outputs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            var name = file.FileName;
            if (name != Path.GetFileName(name) || name.Contains('\\') || name.Contains('/') ||
                file.Length > 1024 * 1024 || !names.Add(name))
                throw new ArgumentException("Test files must have unique simple filenames and be at most 1 MB.");
            var extension = Path.GetExtension(name).ToLowerInvariant();
            var stem = Path.GetFileNameWithoutExtension(name);
            if (string.IsNullOrWhiteSpace(stem) || name.Any(c => char.IsControl(c)) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("Invalid test filename.");
            if (extension == ".in") inputs.Add(stem);
            else if (extension is ".out" or ".sol")
            { if (!outputs.Add(stem)) throw new ArgumentException("Duplicate expected output."); }
            else throw new ArgumentException("Only .in, .out and .sol test files are supported.");
        }
        if (!inputs.SetEquals(outputs)) throw new ArgumentException("Every input needs a matching output filename.");
    }

    public static async Task SaveAsync(string directory, IFormFile[] files, int maximum)
    {
        Validate(files, maximum);
        var parent = Path.GetDirectoryName(Path.GetFullPath(directory))!;
        var staging = Path.Combine(parent, ".upload-" + Guid.NewGuid().ToString("N"));
        var backup = Path.Combine(parent, ".backup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(staging, "Input"));
        Directory.CreateDirectory(Path.Combine(staging, "Output"));
        try
        {
            foreach (var file in files)
            {
                var folder = Path.GetExtension(file.FileName).Equals(".in", StringComparison.OrdinalIgnoreCase) ? "Input" : "Output";
                await using var stream = new FileStream(Path.Combine(staging, folder, file.FileName), FileMode.CreateNew);
                await file.CopyToAsync(stream);
            }
            ProblemStorage.TestPairs(staging, maximum);
            if (Directory.Exists(directory)) Directory.Move(directory, backup);
            try { Directory.Move(staging, directory); }
            catch
            {
                if (Directory.Exists(backup) && !Directory.Exists(directory)) Directory.Move(backup, directory);
                throw;
            }
        }
        finally
        {
            // Both paths are generated immediate children of the validated problem directory's parent.
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
            if (Directory.Exists(backup) && Directory.Exists(directory)) Directory.Delete(backup, true);
        }
    }
}
