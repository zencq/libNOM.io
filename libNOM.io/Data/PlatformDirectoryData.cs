using System.Text.RegularExpressions;

namespace libNOM.io.Data;


/// <summary>
/// Holds information about the default path and file patterns for a <see cref="Platform"/>.
/// </summary>
internal abstract record class PlatformDirectoryData
{
    internal virtual string DirectoryPath { get; } = string.Empty;

    internal virtual string DirectoryPathPattern { get; } = string.Empty;

    internal abstract string[] AnchorFileGlob { get; }

    internal abstract Regex[] AnchorFileRegex { get; }
}
