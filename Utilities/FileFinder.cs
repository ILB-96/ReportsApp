using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Reports.Utilities;

public static class FileFinder
{
    public static string GetFullPath(string[] keywords)
    {
        Console.WriteLine("FileFinder.GetFullPath started.");

        if (keywords is null || keywords.Length == 0)
        {
            Console.WriteLine("No keywords were provided.");
            return string.Empty;
        }

        var downloadsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");

        Console.WriteLine($"Downloads path: {downloadsPath}");

        if (!Directory.Exists(downloadsPath))
        {
            Console.WriteLine("Downloads directory does not exist.");
            return string.Empty;
        }

        var normalizedKeywords = keywords
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Console.WriteLine($"Normalized keywords: {string.Join(", ", normalizedKeywords)}");

        if (normalizedKeywords.Length == 0)
        {
            Console.WriteLine("No valid keywords remained after normalization.");
            return string.Empty;
        }

        try
        {
            var matchingFiles = Directory
                .EnumerateFiles(downloadsPath, "*", SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .Where(file => MatchesAnyKeyword(file.Name, normalizedKeywords))
                .ToList();

            Console.WriteLine($"Matching files found: {matchingFiles.Count}");

            foreach (var file in matchingFiles)
            {
                Console.WriteLine($"Match: {file.Name} | LastWriteTimeUtc: {file.LastWriteTimeUtc:O}");
            }

            var newestMatch = matchingFiles
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .FirstOrDefault();

            if (newestMatch is null)
            {
                Console.WriteLine("No matching file was found.");
                return string.Empty;
            }

            Console.WriteLine($"Newest match selected: {newestMatch.FullName}");
            return newestMatch.FullName;
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"UnauthorizedAccessException: {ex.Message}");
            return string.Empty;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"IOException: {ex.Message}");
            return string.Empty;
        }
    }

    private static bool MatchesAnyKeyword(string fileName, string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (MatchesKeyword(fileName, keyword))
                return true;
        }

        return false;
    }

    private static bool MatchesKeyword(string fileName, string keyword)
    {
        var parts = keyword
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parts.All(part =>
            fileName.Contains(part, StringComparison.OrdinalIgnoreCase));
    }
}