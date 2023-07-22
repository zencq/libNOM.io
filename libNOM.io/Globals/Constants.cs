namespace libNOM.io.Globals;


public static class Constants

{
    internal const int CACHE_EXPIRATION = 250; // milliseconds
    internal const string FILE_TIMESTAMP_FORMAT = "yyyyMMddHHmmssfff";
    internal const VersionEnum LOWEST_SUPPORTED_VERSION = VersionEnum.BeyondWithVehicleCam;

    internal const int GAMEMODE_INT_UNSPECIFIED = (int)(PresetGameModeEnum.Unspecified); // 0
    internal const int GAMEMODE_INT_NORMAL = (int)(PresetGameModeEnum.Normal); // 1
    internal const int GAMEMODE_INT_PERMADEATH = (int)(PresetGameModeEnum.Permadeath); // 5

    internal const string HEADER_PLAINTEXT = "{\"Version\":";
    internal const string HEADER_PLAINTEXT_OBFUSCATED = "{\"F2P\":";
    internal const uint HEADER_SAVE_STREAMING_CHUNK = 0xFEEDA1E5; // 4276986341
    internal const string HEADER_SAVEWIZARD = "NOMANSKY";

    // G = General
    // M = Microsoft
    // O = GoG
    // P = PlayStation
    // S = Steam
    // W = Switch
    public const string INCOMPATIBILITY_001 = "001G_Empty";
    public const string INCOMPATIBILITY_002 = "002G_Deserialization_Exception";
    public const string INCOMPATIBILITY_003 = "003G_Deserialization_Null";
    public const string INCOMPATIBILITY_004 = "004M_Microsoft_Deleted";
    public const string INCOMPATIBILITY_005 = "005M_Microsoft_Missing_Blob";
    public const string INCOMPATIBILITY_006 = "006G_Non_Existent";

    internal const int OFFSET_GAMEMODE = 512;
    internal const int OFFSET_INDEX = 2;
    internal const int OFFSET_SEASON = 128;

    internal const uint SAVE_FORMAT_1 = 0x7D0; // 2000 (1.0) // not used but for completeness
    internal const uint SAVE_FORMAT_2 = 0x7D1; // 2001 (1.1)
    internal const uint SAVE_FORMAT_3 = 0x7D2; // 2002 (3.6)

    internal const int SAVE_STREAMING_HEADER_SIZE = 0x10; // 16
    internal const int SAVE_STREAMING_CHUNK_SIZE = 0x80000; // 524288

    internal const int THRESHOLD_VANILLA = 4098;
    internal const int THRESHOLD_VANILLA_GAMEMODE = THRESHOLD_VANILLA + OFFSET_GAMEMODE;
    internal const int THRESHOLD_WAYPOINT = 4140;
    internal const int THRESHOLD_WAYPOINT_GAMEMODE = THRESHOLD_WAYPOINT + OFFSET_GAMEMODE;
}
