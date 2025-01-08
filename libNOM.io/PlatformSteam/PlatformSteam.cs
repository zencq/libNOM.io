﻿using System.Text;

using CommunityToolkit.HighPerformance;

namespace libNOM.io;


/// <summary>
/// Implementation for the Steam platform.
/// </summary>
// This partial class contains some general code.
public partial class PlatformSteam : Platform
{
    #region Constant

    internal const string ACCOUNT_PATTERN = "st_76561198*";

    internal static readonly string[] ANCHOR_FILE_PATTERN = ["save??.hg"];

    public static readonly string PATH = ((Func<string>)(() =>
    {
        if (Common.IsWindows())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HelloGames", "NMS");

        if (Common.IsLinux()) // SteamDeck
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "Steam", "steamapps", "compatdata", "275850", "pfx", "drive_c", "users", "steamuser", "Application Data", "HelloGames", "NMS");

        if (Common.IsMac())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "HelloGames", "NMS");

        return string.Empty; // same as if not defined at all
    }))();

    protected static readonly uint[] META_ENCRYPTION_KEY = Encoding.ASCII.GetBytes("NAESEVADNAYRTNRG").AsSpan().Cast<byte, uint>().ToArray();
    protected const uint META_HEADER = 0xEEEEEEBE; // 4,008,636,094
    protected override int META_LENGTH_KNOWN_VANILLA => 0x58; // 88
    internal override int META_LENGTH_TOTAL_VANILLA => 0x68; // 104
    internal override int META_LENGTH_TOTAL_WAYPOINT => 0x168; // 360
    internal override int META_LENGTH_TOTAL_WORLDS => 0x180; // 384

    #endregion
}
