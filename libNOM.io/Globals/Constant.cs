namespace libNOM.io.Globals;


internal static class Constant
{
    internal const int CACHE_EXPIRATION = 250; // milliseconds
    internal const string FILE_TIMESTAMP_FORMAT = "yyyyMMddHHmmss";
    internal const VersionEnum LOWEST_SUPPORTED_VERSION = VersionEnum.BeyondWithVehicleCam;

    internal const int GAMEMODE_INT_NORMAL = (int)(PresetGameModeEnum.Normal); // 1
    internal const int GAMEMODE_INT_PERMADEATH = (int)(PresetGameModeEnum.Permadeath); // 5

    internal const string HEADER_PLAINTEXT = "{\"Version\":";
    internal const string HEADER_PLAINTEXT_OBFUSCATED = "{\"F2P\":";
    internal const uint HEADER_SAVE_STREAMING_CHUNK = 0xFEEDA1E5; // 4276986341
    internal const string HEADER_SAVEWIZARD = "NOMANSKY";

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
