using libNOM.io.Settings;

using Newtonsoft.Json.Linq;

namespace libNOM.io;


/// <summary>
/// Abstract base for all platforms which just hook into the methods they need.
/// </summary>
// This partial class contains accessor related code.
public abstract partial class Platform : IPlatform, IEquatable<Platform>
{
    #region Getter

    // public //

    public IContainer? GetAccountContainer() => AccountContainer;

    public IContainer? GetSaveContainer(int collectionIndex) => SaveContainerCollection.FirstOrDefault(i => i.CollectionIndex == collectionIndex);

    public IEnumerable<IContainer> GetSaveContainers() => SaveContainerCollection;

    // protected //

    /// <summary>
    /// Gets the index of the matching anchor.
    /// </summary>
    /// <param name="directory"></param>
    /// <returns></returns>
    protected int GetAnchorFileIndex(DirectoryInfo? directory)
    {
        if (directory is not null)
            for (var i = 0; i < PlatformAnchorFilePattern.Length; i++)
                if (directory.GetFiles(PlatformAnchorFilePattern[i]).Length != 0)
                    return i;

        return -1;
    }

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
}
