using System.Diagnostics;

using libNOM.io.Settings;

namespace libNOM.io;


/// <summary>
/// Abstract base for all platforms which just hook into the methods they need.
/// </summary>
// This partial class contains property related code.
public abstract partial class Platform : IPlatform, IEquatable<Platform>
{
    #region Container

    protected Container? AccountContainer { get; set; } // can be null if LoadingStrategyEnum.Empty

    protected List<Container> SaveContainerCollection { get; } = [];

    #endregion

    #region Flags

    // public //

    public abstract bool CanCreate { get; }

    public abstract bool CanRead { get; }

    public abstract bool CanUpdate { get; }

    public abstract bool CanDelete { get; }

    public virtual bool Exists => Location?.Exists ?? false; // { get; }

    public virtual bool HasAccountData => AccountContainer?.Exists == true && AccountContainer!.IsCompatible; // { get; }

    public abstract bool HasModding { get; }

    public bool IsLoaded => SaveContainerCollection.Count > 0; // { get; }

    public bool IsRunning // { get; }
    {
        get
        {
            if (IsConsolePlatform || string.IsNullOrEmpty(PlatformProcessPath))
                return false;

            try
            {
                // First we get the file name of the process as it is different on Windows and macOS.
                var processName = Path.GetFileNameWithoutExtension(PlatformProcessPath);
                // Then we still need to check the MainModule to get the correct process as Steam (Windows) and Microsoft have the same name.
                var process = Process.GetProcessesByName(processName).FirstOrDefault(i => i.MainModule?.FileName?.EndsWith(PlatformProcessPath, StringComparison.Ordinal) == true);
                return process is not null && !process.HasExited;
            }
            // Throws Win32Exception if the implementing program only targets x86 as the game is a x64 process.
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                return false;
            }
        }
    }

    public virtual bool IsValid => PlatformAnchorFilePattern.ContainsIndex(AnchorFileIndex); // { get; }

    public abstract RestartRequirementEnum RestartToApply { get; }

    // protected //

    protected abstract bool IsConsolePlatform { get; }

    #endregion

    #region Platform Configuration

    // public //

    public DirectoryInfo Location { get; protected set; }

    public PlatformSettings Settings { get; protected set; }

    // protected //

    protected int AnchorFileIndex { get; set; } = -1;

    #endregion

    #region Platform Indicator

    // public //

    public abstract PlatformEnum PlatformEnum { get; }

    public UserIdentification PlatformUserIdentification { get; } = new();

    // protected //

    protected abstract string[] PlatformAnchorFilePattern { get; }

    protected abstract string? PlatformArchitecture { get; }

    protected abstract string? PlatformProcessPath { get; }

    protected abstract string PlatformToken { get; }

    #endregion
}
