using libNOM.io.Data;
using libNOM.io.Delegates;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;

namespace libNOM.io;


/// <summary>
/// Holds all necessary information about a single save.
/// </summary>
public partial class Container : IComparable<Container>, IEquatable<Container>
{
    #region Field

    private bool? _exists;
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

    public bool IsBackup { get; internal set; }

    /// <summary>
    /// Whether there was an exception while loading or an other reason why it is incompatible.
    /// </summary>
    public bool IsCompatible => Exists && string.IsNullOrEmpty(IncompatibilityTag); // { get; }

    public bool IsHardMode => GameModeEnum is PresetGameModeEnum.Survival or PresetGameModeEnum.Permadeath; // { get; }

    /// <summary>
    /// Whether it contains loaded JSON data and is ready to use.
    /// </summary>
    public bool IsLoaded => Exists && _jsonObject is not null; // { get; }

    /// <summary>
    /// Whether it is older than the lowest supported version.
    /// </summary>
    public bool IsOld { get; set; }

    /// <summary>
    /// Whether it is an actual save and not something else like account data.
    /// </summary>
    public bool IsSave => MetaIndex >= Global.OFFSET_INDEX; // { get; }

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

    public int BaseVersion { get; internal set; }

    public PresetGameModeEnum GameModeEnum // { get; set; }
    {
        get
        {
            return Global.GetGameModeEnum(this);
        }
        set
        {
            Version = Global.CalculateVersion(BaseVersion, value, SeasonEnum);
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

    public SaveTypeEnum SaveTypeEnum { get; }

    public SeasonEnum SeasonEnum { get; internal set; } = SeasonEnum.Unspecified;

    public long TotalPlayTime // { get; set; }
    {
        get => _jsonObject is not null ? Global.GetTotalPlayTime(_jsonObject) : _totalPlayTime;
        set
        {
            if (_jsonObject is not null)
            {
                if (_jsonObject.UseMapping())
                {
                    _jsonObject["PlayerStateData"]![nameof(TotalPlayTime)] = value;
                }
                else
                {
                    _jsonObject["6f="]!["Lg8"] = value;
                }
            }
            _totalPlayTime = value;
        }
    }

    internal int Version // { get; set; }
    {
        get => _jsonObject is not null ? Global.GetVersion(_jsonObject) : _version;
        set
        {
            if (_jsonObject is not null)
            {
                if (_jsonObject.UseMapping())
                {
                    _jsonObject[nameof(Version)] = value;
                }
                else
                {
                    _jsonObject["F2P"] = value;
                }
            }
            _version = value;
        }
    }

    public VersionEnum VersionEnum { get; internal set; } = VersionEnum.Unknown;

    #endregion

    #endregion

    #region Getter

    public JObject? GetJsonObject()
    {
        return _jsonObject;
    }

    private bool IsVersion(VersionEnum versionEnum)
    {
        return VersionEnum >= versionEnum;
    }

    #endregion

    #region Setter

    public void SetJsonObject(JObject? value)
    {
        _jsonObject = value;
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
        CollectionIndex = metaIndex - Global.OFFSET_INDEX;
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
        _jsonObject = null;
        _lastWriteTime = null;
        _version = -1;

        BackupCollection.Clear();
        BaseVersion = default;
        IsBackup = false;
        IsOld = false;
        IsSynced = true;
        SeasonEnum = SeasonEnum.Unspecified;
        VersionEnum = VersionEnum.Unknown;
        UserIdentification = null;
        UnknownKeys.Clear();

        ClearIncompatibility();
        RefreshFileInfo();
        ResolveWatcherChange();
    }

    /// <summary>
    /// Resets the property that a watcher changed this container.
    /// </summary>
    internal void ResolveWatcherChange()
    {
        HasWatcherChange = false;
    }
}
