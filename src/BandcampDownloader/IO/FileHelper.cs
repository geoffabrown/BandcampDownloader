using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BandcampDownloader.IO;

internal static class FileHelper
{
    public static string ToAllowedFileName(this string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        fileName = fileName.ReplaceInvalidPathCharacters('_');

        // Remove trailing dot(s)
        fileName = Regex.Replace(fileName, @"\.+$", "");

        // Replace whitespace(s) by ' '
        fileName = Regex.Replace(fileName, @"\s+", " ");

        // Remove trailing whitespace(s) /!\ Must be last
        fileName = Regex.Replace(fileName, @"\s+$", "");

        return fileName;
    }

    /// <summary>
    /// Waits for a file to be available for writing by attempting to open it with write access.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="maxWaitTimeMs">Maximum time to wait in milliseconds. Default is 2000ms.</param>
    /// <param name="retryDelayMs">Delay between retry attempts in milliseconds. Default is 50ms.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>True if the file is available for writing; false if the maximum wait time was exceeded.</returns>
    public static async Task<bool> WaitForFileAvailableAsync(string filePath, int maxWaitTimeMs = 2000, int retryDelayMs = 50, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        var startTime = DateTime.UtcNow;
        var maxWaitTime = TimeSpan.FromMilliseconds(maxWaitTimeMs);

        while (DateTime.UtcNow - startTime < maxWaitTime)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Try to open the file with write access to check if it's available
                await using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Write, FileShare.None);
                return true;
            }
            catch (IOException)
            {
                // File is still locked, wait and retry
                await Task.Delay(retryDelayMs, cancellationToken).ConfigureAwait(false);
            }
        }

        return false;
    }

    private static string ReplaceInvalidPathCharacters(this string path, char replaceBy)
    {
        foreach (var invalidCharacter in Path.GetInvalidPathChars())
        {
            path = path.Replace(invalidCharacter, replaceBy);
        }

        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            path = path.Replace(invalidCharacter, replaceBy);
        }

        return path;
    }
}
