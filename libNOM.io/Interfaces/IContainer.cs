using System.Collections.ObjectModel;

using libNOM.io.Delegates;
using libNOM.io.Trace;

using Newtonsoft.Json.Linq;

namespace libNOM.io.Interfaces;


/// <summary>
/// Holds all information about a single save.
/// </summary>
public interface IContainer : IComparable<IContainer>, IEquatable<IContainer>
{
    #region Delegate

    /// <summary>
    /// Gets triggered when a backup was created.
    /// </summary>
    public NotifyBackupCreatedEventHandler BackupCreatedCallback { get; set; }

    /// <summary>
    /// Gets triggered when the JSON of this <see cref="IContainer"/> was changed.
    /// </summary>
    public NotifyJsonChangedEventHandler JsonChangedCallback { get; set; }

    /// <summary> 
    /// Gets triggered when properties may have changed (internal or external).
    /// </summary>
    public NotifyPropertiesChangedEventHandler PropertiesChangedCallback { get; set; }

    #endregion

    // Property

    #region Flags

    /// <summary>
    /// Whether it is possible to switch context between the main/primary save and an active expedition/season.
    /// </summary>
    public bool CanSwitchContext { get; }

    /// <summary>
    /// Whether this is a save with an ongoing expedition (<see cref="PresetGameModeEnum.Seasonal"/>).
    /// </summary>
    public bool HasActiveExpedition { get; }

    /// <summary>
    /// Whether this contains potential user owned bases.
    /// </summary>
    public bool HasBase { get; }

    /// <summary>
    /// Whether this contains a user owned freighter.
    /// </summary>
    public bool HasFreighter { get; }

    /// <summary>
    /// Whether this contains a potential user owned settlement.
    /// </summary>
    public bool HasSettlement { get; }

    /// <summary>
    /// Whether this contains account data and is not a regular save.
    /// </summary>
    public bool IsAccount { get; }

    /// <summary>
    /// Whether this is a backup.
    /// </summary>
    public bool IsBackup { get; }

    /// <summary>
    /// Whether this was correctly loaded and no exception or an other reason occurred while loading that made it incompatible.
    /// </summary>
    public bool IsCompatible { get; }

    /// <summary>
    /// Whether this contains loaded JSON data and is ready to use.
    /// </summary>
    public bool IsLoaded { get; }

    /// <summary>
    /// Whether this is older than the lowest supported version.
    /// </summary>
    public bool IsOld { get; }

    /// <summary>
    /// Whether this is an actual save and not something else like account data.
    /// </summary>
    public bool IsSave { get; }

    /// <summary>
    /// Whether this is identical to the data on the drive.
    /// </summary>
    public bool IsSynced { get; set; }

    #endregion

    #region FileInfo

    /// <summary>
    /// The file that contains the actual save data.
    /// </summary>
    public FileInfo? DataFile { get; }

    /// <summary>
    /// Whether the data file and therefore the save exists.
    /// </summary>
    public bool Exists { get; }

    /// <summary>
    /// Timestamp when the save was written the last time.
    /// </summary>
    public DateTimeOffset? LastWriteTime { get; }

    /// <summary>
    /// The file that contains additional meta data about the save.
    /// </summary>
    public FileInfo? MetaFile { get; }

    #endregion

    #region FileSystemWatcher

    /// <summary>
    /// How that save was altered by an external application (created, changed, etc).
    /// </summary>
    public WatcherChangeTypes? WatcherChangeType { get; }

    /// <summary>
    /// Whether the save has been altered by an external application.
    /// </summary>
    public bool HasWatcherChange { get; }

    #endregion

    #region Index

    /// <summary>
    /// Starts at 0 for PlayerState1, AccountData will be -2.
    /// </summary>
    public int CollectionIndex { get; }

    /// <summary>
    /// Starts at 0 for AccountData, PlayerState1 will be 2.
    /// </summary>
    public int MetaIndex { get; }

    /// <summary>
    /// Starts at 0 for PlayerState1 and PlayerState2, AccountData will be -1.
    /// </summary>
    public int SlotIndex { get; }

    #endregion

    // EXTERNAL RELEASE: Add new IsVersion flag.
    #region IsVersion

    public bool IsVersion211BeyondWithVehicleCam { get; }

    public bool IsVersion220Synthesis { get; }

    public bool IsVersion226SynthesisWithJetpack { get; }

    public bool IsVersion230LivingShip { get; }

    public bool IsVersion240ExoMech { get; }

    public bool IsVersion250Crossplay { get; }

    public bool IsVersion260Desolation { get; }

    public bool IsVersion300Origins { get; }

    public bool IsVersion310NextGeneration { get; }

    public bool IsVersion320Companions { get; }

    public bool IsVersion330Expeditions { get; }

    public bool IsVersion340Beachhead { get; }

    public bool IsVersion350Prisms { get; }

    public bool IsVersion351PrismsWithByteBeatAuthor { get; }

    public bool IsVersion360Frontiers { get; }

    public bool IsVersion370Emergence { get; }

    public bool IsVersion380Sentinel { get; }

    public bool IsVersion381SentinelWithWeaponResource { get; }

    public bool IsVersion384SentinelWithVehicleAI { get; }

    public bool IsVersion385Outlaws { get; }

    public bool IsVersion390Leviathan { get; }

    public bool IsVersion394Endurance { get; }

    public bool IsVersion400Waypoint { get; }

    public bool IsVersion404WaypointWithAgileStat { get; }

    public bool IsVersion405WaypointWithSuperchargedSlots { get; }

    public bool IsVersion410Fractal { get; }

    public bool IsVersion420Interceptor { get; }

    public bool IsVersion430Singularity { get; }

    public bool IsVersion440Echoes { get; }

    public bool IsVersion450Omega { get; }

    public bool IsVersion452OmegaWithMicrosoftV2 { get; }

    public bool IsVersion460Orbital { get; }

    public bool IsVersion470Adrift { get; }

    public bool IsVersion500WorldsPartI { get; }

    public bool IsVersion510Aquarius { get; }

    public bool IsVersion520TheCursed { get; }

    public bool IsVersion525TheCursedWithCrossSave { get; }

    public bool IsVersion529TheCursedWithStarbornPhoenix { get; }

    public bool IsVersion550WorldsPartII { get; }

    #endregion

    #region Miscellaneous

    /// <summary>
    /// List of related backups.
    /// </summary>
    public ObservableCollection<IContainer> BackupCollection { get; }

    /// <summary>
    /// Identifier of the save containing the slot number and save type.
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    /// If the incompatibility was caused by an unexpected exception, it stored here.
    /// </summary>
    public Exception? IncompatibilityException { get; }

    /// <summary>
    /// A tag with information why this save is incompatible. To see what reasons are available have a look at <see cref="Constants"/>.INCOMPATIBILITY_\d{3}.
    /// </summary>
    public string? IncompatibilityTag { get; }

    /// <summary>
    /// Exposes a lot of additional information that are usually not necessary while using this library but might useful for debugging purposes.
    /// Not intended to show the end user.
    /// </summary>
    public ContainerTrace? Trace { get; }

    /// <summary>
    /// List of unknown keys collected during deobfuscation.
    /// </summary>
    public HashSet<string> UnknownKeys { get; }

    #endregion

    #region Save

    /// <summary>
    /// The active context or <see cref="SaveContextQueryEnum.DontCare"/> for pre-Omega saves.
    /// </summary>
    public SaveContextQueryEnum ActiveContext { get; set; }

    /// <summary>
    /// Difficulty setting of the save.
    /// For pre-Waypoint this matches the <see cref="PresetGameModeEnum"/> and for newer ones the available preset are checked.
    /// </summary>
    public DifficultyPresetTypeEnum Difficulty { get; set; }

    /// <summary>
    /// Version (named update) of the game the save was last used with.
    /// </summary>
    public GameVersionEnum GameVersion { get; }

    public string SaveName { get; set; }

    public string SaveSummary { get; set; }

    /// <summary>
    /// Whether this save is used as auto or manual.
    /// </summary>
    public SaveTypeEnum SaveType { get; }

    /// <summary>
    /// For <see cref="PresetGameModeEnum.Seasonal"/> saves this tells which one.
    /// </summary>
    public SeasonEnum Season { get; }

    /// <summary>
    /// Total time played in seconds.
    /// </summary>
    public uint TotalPlayTime { get; set; }

    #endregion

    // Accessor

    #region Getter

    /// <summary>
    /// Gets the entire JSON object.
    /// </summary>
    /// <returns></returns>
    public JObject GetJsonObject();

    /// <inheritdoc cref="GetJsonToken(string, SaveContextQueryEnum)"/>
    /// <summary>
    /// Gets a JSON element that matches the JSONPath expression.
    /// For saves from Omega and up this will use the active context if the path goes there and you use "{0}.PlayerStateData" in the expression.
    /// </summary>
    public JToken? GetJsonToken(string pathIdentifier);

    /// <summary>
    /// Gets a JSON element that matches the JSONPath expression.
    /// For saves from Omega and up this will use the specified context if the path goes there and you use "{0}.PlayerStateData" in the expression.
    /// </summary>
    /// <param name="pathIdentifier">A JSONPath expressions.</param>
    /// <param name="context"></param>
    /// <returns></returns>
    public JToken? GetJsonToken(string pathIdentifier, SaveContextQueryEnum context);

    /// <inheritdoc cref="GetJsonTokens(string, SaveContextQueryEnum)"/>
    /// <summary>
    /// Gets a collection of JSON elements that matches the JSONPath expression.
    /// For saves from Omega and up this will use the active context if the path goes there and you use "{0}.PlayerStateData" in the expression.
    /// </summary>
    public IEnumerable<JToken> GetJsonTokens(string pathIdentifier);

    /// <summary>
    /// Gets a collection of JSON elements that matches the JSONPath expression.
    /// For saves from Omega and up this will use the specified context if the path goes there and you use "{0}.PlayerStateData" in the expression.
    /// </summary>
    /// <param name="pathIdentifier">A JSONPath expressions.</param>
    /// <param name="context"></param>
    /// <returns></returns>
    public IEnumerable<JToken> GetJsonTokens(string pathIdentifier, SaveContextQueryEnum context);

    /// <summary>
    /// Gets the value of the JSON element that matches the path of indices.
    /// Except the last one, each index in the entire path must point to either a JArray or a JObject.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="indices"></param>
    /// <returns>The value at the end of the path of indices.</returns>
    public T? GetJsonValue<T>(ReadOnlySpan<int> indices);

    /// <inheritdoc cref="GetJsonValue{T}(string, SaveContextQueryEnum)"/>
    public T? GetJsonValue<T>(string pathIdentifier);

    /// <summary>
    /// Gets the actual value of the JSON element that matches the JSONPath expression.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="pathIdentifier">A JSONPath expressions.</param>
    /// <param name="context"></param>
    /// <returns></returns>
    public T? GetJsonValue<T>(string pathIdentifier, SaveContextQueryEnum context);

    /// <inheritdoc cref="GetJsonValue{T}(string, SaveContextQueryEnum)"/>
    public IEnumerable<T?> GetJsonValues<T>(string pathIdentifier);

    /// <summary>
    /// Gets the actual values of all JSON elements that matches the JSONPath expression.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="pathIdentifier">A JSONPath expressions.</param>
    /// <param name="context">A JSONPath expressions.</param>
    /// <returns></returns>
    public IEnumerable<T?> GetJsonValues<T>(string pathIdentifier, SaveContextQueryEnum context);

    #endregion

    #region Setter

    /// <summary>
    /// Sets the value of the JSON element that matches the path of indices.
    /// Except the last one, each index in the entire path must point to either a JArray or a JObject.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="indices"></param>
    public void SetJsonValue(JToken value, ReadOnlySpan<int> indices);

    /// <inheritdoc cref="SetJsonValue(JToken, string, SaveContextQueryEnum)"/>
    public void SetJsonValue(JToken value, string pathIdentifier);

    /// <summary>
    /// Sets the actual value of the JSON element that matches the JSONPath expression.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="pathIdentifier">A JSONPath expressions.</param>
    /// <param name="context"></param>
    public void SetJsonValue(JToken value, string pathIdentifier, SaveContextQueryEnum context);

    /// <summary>
    /// Sets <see cref="WatcherChangeType"/> to the specified value and activates <see cref="HasWatcherChange"/>.
    /// </summary>
    /// <param name="changeType"></param>
    public void SetWatcherChange(WatcherChangeTypes changeType);

    #endregion
}
