using System.Diagnostics;

using libNOM.io.Settings;

using Newtonsoft.Json.Linq;

namespace libNOM.io;


/// <summary>
/// Abstract base for all platforms which just hook into the methods they need.
/// </summary>
// This partial class contains some general code.
public abstract partial class Platform : IPlatform, IEquatable<Platform>
{
    #region Constant

    protected virtual int MAX_SAVE_SLOTS { get; } = Constants.MAX_SAVE_SLOTS; // overridable for compatibility with old PlayStation format
    protected virtual int MAX_SAVE_PER_SLOT { get; } = Constants.MAX_SAVE_PER_SLOT; // overridable in case it will be necessary in the future
    internal int MAX_SAVE_TOTAL => MAX_SAVE_SLOTS * MAX_SAVE_PER_SLOT; // { get; } // compute here in case one of the values has been overridden

    protected virtual int META_LENGTH_KNOWN_VANILLA { get; } = -1; // all metadata at the beginning of a file before the first extension in Waypoint
    protected int META_LENGTH_KNOWN_NAME => META_LENGTH_KNOWN_VANILLA + (1 * Constants.SAVE_RENAMING_LENGTH_MANIFEST); // { get; } // ? + 128
    protected int META_LENGTH_KNOWN_SUMMARY => META_LENGTH_KNOWN_VANILLA + (2 * Constants.SAVE_RENAMING_LENGTH_MANIFEST); // { get; } // ? + 256

    internal abstract int META_LENGTH_TOTAL_VANILLA { get; }
    internal abstract int META_LENGTH_TOTAL_WAYPOINT { get; }
    internal abstract int META_LENGTH_TOTAL_WORLDS { get; }

    #endregion

    // Property

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

    public bool IsLoaded => SaveContainerCollection.Count != 0; // { get; }

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

    // Accessor

    #region Getter

    // public //

    public IContainer? GetAccountContainer() => AccountContainer;

    public IContainer? GetSaveContainer(int collectionIndex) => SaveContainerCollection.FirstOrDefault(i => i.CollectionIndex == collectionIndex);

    public IEnumerable<IContainer> GetSaveContainers() => SaveContainerCollection;

    // private //

    private static IEnumerable<SaveContextQueryEnum> GetContexts(JObject jsonObject)
    {
        // Check first whether there can be context keys.
        if (Constants.JSONPATH["ACTIVE_CONTEXT"].Any(jsonObject.ContainsKey))
        {
            // Then return all contexts that are in the specified JSON.
            if (Constants.JSONPATH["BASE_CONTEXT"].Any(jsonObject.ContainsKey))
                yield return SaveContextQueryEnum.Main;
            if (Constants.JSONPATH["EXPEDITION_CONTEXT"].Any(jsonObject.ContainsKey))
                yield return SaveContextQueryEnum.Season;
        }
        else
            yield return SaveContextQueryEnum.DontCare;
    }

    #endregion

    #region Setter

    /// <summary>
    /// Updates the instance with a new configuration. If null is passed, the settings will be reset to default.
    /// </summary>
    /// <param name="platformSettings"></param>
    public void SetSettings(PlatformSettings? platformSettings)
    {
        // Cache old values first to be able to properly react to the change.
        var oldMapping = Settings.UseMapping;
        var oldStrategy = Settings.LoadingStrategy;

        // Update.
        Settings = platformSettings ?? new();

        // Set new loadingStrategy and trigger collection operations.
        if (Settings.LoadingStrategy == LoadingStrategyEnum.Empty && oldStrategy > LoadingStrategyEnum.Empty)
        {
            // Clear container by removing its reference.
            AccountContainer = null!;
            SaveContainerCollection.Clear();

            DisableWatcher();
        }
        else if (Settings.LoadingStrategy > LoadingStrategyEnum.Empty && oldStrategy == LoadingStrategyEnum.Empty)
            InitializePlatform(); // calls EnableWatcher()

        // Ensure mapping is updated in the containers.
        if (Settings.UseMapping != oldMapping)
            foreach (var container in SaveContainerCollection.Where(i => i.IsLoaded))
                container.SetJsonObject(container.GetJsonObject());
    }

    #endregion

    // Interface

    #region IEquatable

    public override bool Equals(object? obj)
    {
        return Equals(obj as Platform);
    }

    public bool Equals(Platform? other)
    {
        return (this.PlatformEnum, this.PlatformUserIdentification.UID, this.Location?.FullName) == (other?.PlatformEnum, other?.PlatformUserIdentification.UID, other?.Location?.FullName);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode() + PlatformEnum.GetHashCode() + Location.GetHashCode();
    }

    #endregion

    #region ToString

    public override string ToString()
    {
        if (string.IsNullOrEmpty(PlatformUserIdentification.UID))
            return $"{PlatformEnum}";

        return $"{PlatformEnum} {PlatformUserIdentification.UID}";
    }

    #endregion
}
