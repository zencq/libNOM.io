using libNOM.io.Delegates;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;

namespace libNOM.io;


/// <summary>
/// Holds all necessary information about a single save.
/// </summary>
public partial class Container : IComparable<Container>, IEquatable<Container>
{
    #region Constant

    private const string MISSION_CREATIVE = "^CREATIVE";

    #endregion

    #region Field

    private bool? _exists;
    private PresetGameModeEnum? _gameMode;
    private JObject? _jsonObject;
    private DateTimeOffset? _lastWriteTime;
    private long _totalPlayTime;
    private int _version = -1;

    #endregion

    #region Property

    /// <summary>
    /// List of related backups.
    /// </summary>
    public ObservableCollection<Container> BackupCollection { get; } = new();

    public string Identifier { get; }

    public Exception? IncompatibilityException { get; internal set; }

    public string? IncompatibilityTag { get; internal set; }

    internal UserIdentificationData? UserIdentification { get; set; }

    /// <summary>
    /// List of unknown keys collected during deobfuscation.
    /// </summary>
    public HashSet<string> UnknownKeys { get; set; } = new();

    #region Flags

    /// <summary>
    /// Whether this is from a backup file.
    /// </summary>
    public bool IsBackup { get; internal set; }

    /// <summary>
    /// Whether there was an exception while loading or an other reason why it is incompatible.
    /// </summary>
    public bool IsCompatible => Exists && string.IsNullOrEmpty(IncompatibilityTag); // { get; }

    /// <summary>
    /// Whether the game mode is either Survival or Permadeath.
    /// </summary>
    public bool IsHardMode => GameModeEnum is PresetGameModeEnum.Survival or PresetGameModeEnum.Permadeath; // { get; }

    /// <summary>
    /// Whether it contains loaded JSON data and is ready to use.
    /// </summary>
    public bool IsLoaded => Exists && _jsonObject is not null; // { get; }

    /// <summary>
    /// Whether it is older than the lowest supported version.
    /// </summary>
    public bool IsOld => VersionEnum < Globals.Constant.LOWEST_SUPPORTED_VERSION; // { get; }

    /// <summary>
    /// Whether it is an actual save and not something else like account data.
    /// </summary>
    public bool IsSave => MetaIndex >= Globals.Constant.OFFSET_INDEX; // { get; }

    /// <summary>
    /// Whether it is identical to the data on the drive.
    /// </summary>
    public bool IsSynced { get; set; } = true;

    #endregion

    #region FileInfo

    public FileInfo? DataFile { get; set; }

    public bool Exists // { get; set; }
    {
        get => _exists ?? DataFile?.Exists == true;
        internal set => _exists = value;
    }

    public DateTimeOffset LastWriteTime // { get; set; }
    {
        get => _lastWriteTime ?? DataFile?.LastWriteTime ?? DateTimeOffset.MinValue;
        set => _lastWriteTime = value;
    }

    public FileInfo? MetaFile { get; set; }

    #endregion

    #region FileSystemWatcher

    public WatcherChangeTypes WatcherChangeType { get; private set; }

    public bool HasWatcherChange { get; private set; }

    #endregion

    #region Index

    public int CollectionIndex { get; }

    public int MetaIndex { get; }

    public int SlotIndex { get; }

    #endregion

    #region Save

    // public //

    public int BaseVersion { get; internal set; }

    public PresetGameModeEnum? GameModeEnum // { get; private set; }
    {
        get => _jsonObject is not null ? Globals.Json.GetGameModeEnum(this, _jsonObject) : _gameMode;
        internal set
        {
            if (_jsonObject is not null)
            {
                Version = Globals.Calculate.CalculateVersion(BaseVersion, value, SeasonEnum);
            }
            _gameMode = value;
        }
    }

    public bool IsBeyondWithVehicleCam => IsVersion(VersionEnum.BeyondWithVehicleCam); // { get; }

    public bool IsSynthesis => IsVersion(VersionEnum.Synthesis); // { get; }

    public bool IsSynthesisWithJetpack => IsVersion(VersionEnum.SynthesisWithJetpack); // { get; }

    public bool IsLivingShip => IsVersion(VersionEnum.LivingShip); // { get; }

    public bool IsExoMech => IsVersion(VersionEnum.ExoMech); // { get; }

    public bool IsCrossplay => IsVersion(VersionEnum.Crossplay); // { get; }

    public bool IsDesolation => IsVersion(VersionEnum.Desolation); // { get; }

    public bool IsOrigins => IsVersion(VersionEnum.Origins); // { get; }

    public bool IsNextGeneration => IsVersion(VersionEnum.NextGeneration); // { get; }

    public bool IsCompanions => IsVersion(VersionEnum.Companions); // { get; }

    public bool IsExpeditions => IsVersion(VersionEnum.Expeditions); // { get; }

    public bool IsBeachhead => IsVersion(VersionEnum.Beachhead); // { get; }

    public bool IsPrisms => IsVersion(VersionEnum.Prisms); // { get; }

    public bool IsPrismsWithBytebeatAuthor => IsVersion(VersionEnum.PrismsWithBytebeatAuthor); // { get; }

    public bool IsFrontiers => IsVersion(VersionEnum.Frontiers); // { get; }

    public bool IsEmergence => IsVersion(VersionEnum.Emergence); // { get; }

    public bool IsSentinel => IsVersion(VersionEnum.Sentinel); // { get; }

    public bool IsSentinelWithWeaponResource => IsVersion(VersionEnum.SentinelWithWeaponResource); // { get; }

    public bool IsSentinelWithVehicleAI => IsVersion(VersionEnum.SentinelWithVehicleAI); // { get; }

    public bool IsOutlaws => IsVersion(VersionEnum.Outlaws); // { get; }

    public bool IsLeviathan => IsVersion(VersionEnum.Leviathan); // { get; }

    public bool IsEndurance => IsVersion(VersionEnum.Endurance); // { get; }

    public bool IsWaypoint => IsVersion(VersionEnum.Waypoint); // { get; }

    public bool IsWaypointWithAgileStat => IsVersion(VersionEnum.WaypointWithAgileStat); // { get; }

    public bool IsWaypointWithSuperchargedSlots => IsVersion(VersionEnum.WaypointWithSuperchargedSlots); // { get; }

    public bool IsFractal => IsVersion(VersionEnum.Fractal); // { get; }

    public bool IsInterceptor => IsVersion(VersionEnum.Interceptor); // { get; }

    public bool IsSingularity => IsVersion(VersionEnum.Singularity); // { get; }

    public string SaveName // { get; set; }
    {
        get => GetJsonValue<string>("6f=.Pk4", "PlayerStateData.SaveName") ?? string.Empty;
        set => SetJsonValue(value, "6f=.Pk4", "PlayerStateData.SaveName");
    }

    public string SaveSummary // { get; set; }
    {
        get => GetJsonValue<string>("6f=.n:R", "PlayerStateData.SaveSummary") ?? string.Empty;
        set => SetJsonValue(value, "6f=.n:R", "PlayerStateData.SaveSummary");
    }

    public SaveTypeEnum SaveTypeEnum { get; }

    public SeasonEnum SeasonEnum { get; internal set; } = SeasonEnum.Pioneers;

    public long TotalPlayTime // { get; set; }
    {
        get => _jsonObject is not null ? Globals.Json.GetTotalPlayTime(_jsonObject) : _totalPlayTime;
        set
        {
            SetJsonValue(value, "6f=.Lg8", "PlayerStateData.TotalPlayTime");
            _totalPlayTime = value;
        }
    }

    public VersionEnum VersionEnum { get; internal set; } = VersionEnum.Unknown;

    // internal //

    //internal bool UsesMapping => _jsonObject?.ContainsKey(nameof(Version)) == true; // { get; }

    internal int Version // { get; set; }
    {
        get => _jsonObject is not null ? Globals.Json.GetVersion(_jsonObject) : _version;
        set
        {
            SetJsonValue(value, "F2P", nameof(Version));
            _version = value;
        }
    }

    #endregion

    #endregion

    #region Getter

    public JObject? GetJsonObject()
    {
        return _jsonObject;
    }

    public JToken? GetJsonToken(params string[] paths)
    {
        foreach (var path in paths)
        {
            var jToken = _jsonObject?.SelectToken(path);
            if (jToken is not null)
                return jToken;
        }
        return null;
    }

    public IEnumerable<JToken> GetJsonTokens(params string[] paths)
    {
        foreach (var path in paths)
        {
            var jTokens = _jsonObject?.SelectTokens(path);
            if (jTokens is not null)
                return jTokens;
        }
        return Array.Empty<JToken>();
    }

    public T? GetJsonValue<T>(params string[] paths)
    {
        if (_jsonObject is not null)
            return _jsonObject.GetValue<T>(paths);

        return default;
    }

    // private //

    private bool IsVersion(VersionEnum versionEnum)
    {
        return VersionEnum >= versionEnum;
    }

    #endregion

    #region Setter

    public void SetGameMode(PresetGameModeEnum mode)
    {
        if (_jsonObject is null)
            return;

        var mission = GetJsonToken($"6f=.dwb[?(@.p0c == '{MISSION_CREATIVE}')]", $"PlayerStateData.MissionProgress[?(@.Mission == '{MISSION_CREATIVE}')]");

        // Remove MISSION_CREATIVE if new mode is not Creative.
        if (mode != PresetGameModeEnum.Creative)
        {
            mission?.Remove();
        }
        // Add MISSION_CREATIVE if new mode is Creative but mission does not exist.
        else if (mission is null)
        {
#if NETSTANDARD2_0_OR_GREATER
            var participantTypes = Enum.GetNames(typeof(ParticipantTypeEnum));
#else
            var participantTypes = Enum.GetNames<ParticipantTypeEnum>();
#endif

            // Will be obfuscated when written.
            mission = new JObject
            {
                { "Mission", MISSION_CREATIVE },
                { "Progress", 1 },
                { "Seed", 0 },
                { "Data", 0 },
                { "Participants", new JArray() },
            };
            for (var i = 0; i < participantTypes.Length; i++)
            {
                (mission["Participants"] as JArray)!.Add(new JObject
                {
                    { "UA", 0 },
                    { "BuildingSeed", new JArray { true, "0x0" } },
                    { "BuildingLocation", new JArray { 0, 0, 0 } },
                    { "ParticipantType", new JObject
                        {
                            { "ParticipantType", participantTypes[i] },
                        }
                    },
                });
            }
            (GetJsonToken($"6f=.dwb", $"PlayerStateData.MissionProgress") as JArray)?.Add(mission);
        }

        // Set value.
        GameModeEnum = mode;
    }

    public void SetJsonObject(JObject? value)
    {
        if (_jsonObject?.Equals(value) != true)
            IsSynced = false;

        _jsonObject = value;
    }

    public void SetJsonValue(JToken value, params string[] paths)
    {
        if (_jsonObject is null)
            return;

        foreach (var path in paths)
        {
            JToken? token;
            if (path.IsAllDigits())
            {
                var indices = path.ToCharArray();
                token = _jsonObject;

                for (var i = 0; i < indices.Length - 1; i++)
                {
                    token = token!.Type switch
                    {
                        JTokenType.Object => token.Children().ElementAtOrDefault(indices[i]), // default = null
                        JTokenType.Array => (token as JArray)?.ContainsIndex(indices[i]) == true ? token[indices[i]] : null,
                        _ => null,
                        //_ => throw new IndexOutOfRangeException($"Index {indices[i]} at position {i} is not available ({token.Path})"),
                    };
                    if (token is null)
                        break;
                }
            }
            else
            {
                token = _jsonObject?.SelectToken(path);
            }

            if (token is not null)
            {
                token.Replace(value);
                IsSynced = false;
                break;
            }
        }
    }

    public void SetWatcherChange(WatcherChangeTypes changeType)
    {
        HasWatcherChange = true;
        WatcherChangeType = changeType;
    }

    #endregion

    #region Delegate

    public NotifyBackupCreatedEventHandler BackupCreatedCallback { get; set; } = delegate { };

    public NotifyBackupRestoredEventHandler BackupRestoredCallback { get; set; } = delegate { };

    public NotifyWriteEventHandler WriteCallback { get; set; } = delegate { };

    #endregion

    // //

    #region Constructor

    public Container(int metaIndex)
    {
        CollectionIndex = metaIndex - Globals.Constant.OFFSET_INDEX;
        MetaIndex = metaIndex;

        SaveTypeEnum = (SaveTypeEnum)(CollectionIndex % 2);
        SlotIndex = CollectionIndex / 2; // integer division

        Identifier = MetaIndex == 0 ? "AccountData" : MetaIndex == 1 ? "Settings" : $"Slot{SlotIndex + 1}{SaveTypeEnum}";
    }

    #endregion

    #region IComparable, IEquatable

    public int CompareTo(Container? other)
    {
        return MetaIndex.CompareTo(other?.MetaIndex);
    }

    public bool Equals(Container? other)
    {
        if (other is null)
            return this is null;

        return GetHashCode() == other.GetHashCode();
    }

    public override bool Equals(object? other)
    {
        return other is Container otherContainer && Equals(otherContainer);
    }

    public override int GetHashCode()
    {
        return DataFile?.GetHashCode() ?? MetaIndex.GetHashCode();
    }

    public static bool operator ==(Container left, Container right)
    {
        if (left is null)
            return right is null;

        return left.Equals(right);
    }

    public static bool operator !=(Container left, Container right)
    {
        return !(left == right);
    }

    public static bool operator <(Container left, Container right)
    {
        return left is null ? right is not null : left.CompareTo(right) < 0;
    }

    public static bool operator <=(Container left, Container right)
    {
        return left is null || left.CompareTo(right) <= 0;
    }

    public static bool operator >(Container left, Container right)
    {
        return left is not null && left.CompareTo(right) > 0;
    }

    public static bool operator >=(Container left, Container right)
    {
        return left is null ? right is null : left.CompareTo(right) >= 0;
    }

    #endregion

    #region ToString

    public override string ToString()
    {
        return $"{nameof(Container)} {MetaIndex} {Identifier}";
    }

    #endregion

    // //

    /// <summary>
    /// Resets the incompatibility properties.
    /// </summary>
    internal void ClearIncompatibility()
    {
        IncompatibilityException = null;
        IncompatibilityTag = null;
    }

    /// <summary>
    /// Refreshes file info for data and meta.
    /// </summary>
    internal void RefreshFileInfo()
    {
        DataFile?.Refresh();
        MetaFile?.Refresh();

        // Reset to use latest data from property.
        _lastWriteTime = null;
    }

    /// <summary>
    /// Resets the container to the default state except for properties set in ctor.
    /// </summary>
    internal void Reset()
    {
        _exists = null;
        _gameMode = null;
        _jsonObject = null;
        _lastWriteTime = null;
        _totalPlayTime = 0;
        _version = -1;

        BackupCollection.Clear();
        UserIdentification = null;
        UnknownKeys.Clear();

        ClearIncompatibility();

        IsSynced = true;

        RefreshFileInfo();
        ResolveWatcherChange();

        BaseVersion = 0;
        SeasonEnum = SeasonEnum.None;
        VersionEnum = VersionEnum.Unknown;
    }

    /// <summary>
    /// Resets the property that a watcher changed this container.
    /// </summary>
    internal void ResolveWatcherChange()
    {
        HasWatcherChange = false;
    }
}
