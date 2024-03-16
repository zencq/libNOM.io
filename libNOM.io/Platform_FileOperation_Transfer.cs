using System.Globalization;

using CommunityToolkit.Diagnostics;

using libNOM.io.Interfaces;

using Microsoft.Extensions.Caching.Memory;

using Newtonsoft.Json.Linq;

namespace libNOM.io;


// This partial class contains file operation related code, especially for transfer between platform instances.
public abstract partial class Platform : IPlatform, IEquatable<Platform>
{
    #region Getter

    /// <summary>
    /// Creates an unique identifier for bases based on its location.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <returns></returns>
    private static string GetBaseIdentifier(JObject jsonObject)
    {
#if NETSTANDARD2_0
        var galacticAddress = jsonObject.GetValue<string>("RELATIVE_BASE_GALACTIC_ADDRESS")!;
        var galacticInteger = galacticAddress.StartsWith("0x") ? long.Parse(galacticAddress[2..], NumberStyles.HexNumber) : long.Parse(galacticAddress);
#else
        ReadOnlySpan<char> galacticAddress = jsonObject.GetValue<string>("RELATIVE_BASE_GALACTIC_ADDRESS");
        var galacticInteger = galacticAddress.StartsWith("0x") ? long.Parse(galacticAddress[2..], NumberStyles.HexNumber) : long.Parse(galacticAddress);
#endif

        var positionX = jsonObject.GetValue<int>("RELATIVE_BASE_POSITION_0");
        var positionY = jsonObject.GetValue<int>("RELATIVE_BASE_POSITION_1");
        var positionZ = jsonObject.GetValue<int>("RELATIVE_BASE_POSITION_2");

        return $"{galacticInteger}{positionX:+000000;-000000}{positionY:+000000;-000000}{positionZ:+000000;-000000}";
    }

    private static string GetBaseName(JObject jsonObject)
    {
        var name = jsonObject.GetValue<string>("RELATIVE_BASE_NAME");
        if (string.IsNullOrEmpty(name))
            name = EnumExtensions.Parse<PersistentBaseTypesEnum>(jsonObject.GetValue<string>("RELATIVE_BASE_TYPE")) switch
            {
                PersistentBaseTypesEnum.FreighterBase => "Freighter Base",
                PersistentBaseTypesEnum.HomePlanetBase => "Unnamed Planet Base",
                _ => "Unnamed Base",
            };

        return name!;
    }

    private (string Identifier, object?[] Interpolations)[] GetTransferIntersectionExpressionsByBase() => [
        ($"INTERSECTION_PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_TYPE_OR_TYPE", []),
        ($"INTERSECTION_PERSISTENT_PLAYER_BASE_OWNERSHIP_EXPRESSION_THIS_UID", [PlatformUserIdentification.UID]),
    ];

    #endregion

    #region Prepare

    /// <summary>
    /// Ensures that the destination is prepared for the incoming <see cref="Transfer(TransferData, int)"/>.
    /// Mainly to lookup the user identification.
    /// </summary>
    /// <param name="destinationSlotIndex"></param>
    protected void PrepareTransferDestination(int destinationSlotIndex)
    {
        // Load destination as they are needed anyway.
        foreach (var container in SaveContainerCollection.Where(i => i.SlotIndex == destinationSlotIndex))
            if (container.Exists && !container.IsLoaded)
                BuildContainerFull(container);

        PrepareUserIdentification();
    }

    private void PrepareUserIdentification()
    {
        // If user identification is not complete, load saves until it is.
        foreach (var container in SaveContainerCollection.Where(i => i.Exists && !i.IsLoaded))
        {
            // Faking while-loop by checking first.
            if (PlatformUserIdentification.IsComplete())
                break;

            BuildContainerFull(container);
        }
    }

    #endregion

    #region Source

    public TransferData GetSourceTransferData(int sourceSlotIndex)
    {
        PrepareUserIdentification();

        var sourceTransferData = new TransferData(SaveContainerCollection.Where(i => i.SlotIndex == sourceSlotIndex), true, [], true, true, true, PlatformUserIdentification);

        foreach (var container in sourceTransferData.Containers)
        {
            if (!container.Exists)
                continue;

            if (!container.IsLoaded)
                BuildContainerFull(container);

            var jsonObject = container.GetJsonObject();

            var expressions = GetTransferIntersectionExpressionsByBase().Select(i => Json.GetPath(i.Identifier, jsonObject, jsonObject, i.Interpolations));

            foreach (var context in GetContexts(jsonObject))
            {
                var path = Json.GetPath("INTERSECTION_PERSISTENT_PLAYER_BASE_FOR_TRANSFER", jsonObject, context);
                foreach (var persistentPlayerBase in jsonObject.SelectTokensWithIntersection<JObject>(expressions.Select(i => string.Format(path, i))))
                    sourceTransferData.TransferBaseUserDecision[GetBaseIdentifier(persistentPlayerBase)] = new(context, GetBaseName(persistentPlayerBase), true);
            }
        }

        UpdateUserIdentification();

        return sourceTransferData with { UserIdentification = PlatformUserIdentification };
    }

    #endregion

    #region Destination

    public void Transfer(TransferData sourceTransferData, int destinationSlotIndex) => Transfer(sourceTransferData, destinationSlotIndex, true);

    /// <inheritdoc cref="Transfer(TransferData, int)"/>
    /// <param name="write"></param>
    /// <exception cref="InvalidOperationException"></exception>
    protected virtual void Transfer(TransferData sourceTransferData, int destinationSlotIndex, bool write)
    {
        PrepareTransferDestination(destinationSlotIndex);

        if (!sourceTransferData.UserIdentification.IsComplete() || !PlatformUserIdentification.IsComplete())
            ThrowHelper.ThrowInvalidOperationException("Cannot transfer as at least one user identification is not complete.");

        foreach (var (Source, Destination) in sourceTransferData.Containers.Zip(SaveContainerCollection.Where(i => i.SlotIndex == destinationSlotIndex), (Source, Destination) => (Source, Destination)))
            if (!Source.Exists)
            {
                Delete(Destination, write);
            }
            else if (Destination.Exists || (!Destination.Exists && CanCreate))
            {
                if (!Source.IsCompatible)
                    ThrowHelper.ThrowInvalidOperationException($"Cannot copy as the source container is not compatible: {Source.IncompatibilityTag}");

                // Needs to be set first to use the correct obfuscation state.
                Destination.Platform = this;

                Destination.SetJsonObject(Source.GetJsonObject());
                Destination.ClearIncompatibility();

                // Due to this CanCreate can be true.
                CreatePlatformExtra(Destination, Source);

                // Faking relevant properties to force it to Write().
                Destination.Exists = true;

                // Additional properties required to properly rebuild the container.
                Destination.GameVersion = Source.GameVersion;
                Destination.SaveVersion = Source.SaveVersion;
                Destination.UserIdentification = PlatformUserIdentification; // update to match new platform

                TransferOwnership(Destination, sourceTransferData);

                // This "if" is not really useful in this method but properly implemented nonetheless.
                if (write)
                    Write(Destination, Source.LastWriteTime ?? DateTimeOffset.Now);
            }
    }

    #endregion

    #region Transfer Ownership

    /// <summary>
    /// Transfers ownerships to new container according to the prepared data.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="sourceTransferData"></param>
    protected void TransferOwnership(Container container, TransferData sourceTransferData)
    {
        var jsonObject = container.GetJsonObject();

        // Change token for Platform.
        jsonObject.SetValue(PlatformArchitecture, "PLATFORM");

        if (sourceTransferData.TransferDiscovery) // 1.0
            TransferCommonOwnership(jsonObject, sourceTransferData, SaveContextQueryEnum.DontCare, "TRANSFER_UID_DISCOVERY");

        if (container.IsVersion(GameVersionEnum.Foundation) && sourceTransferData.TransferBase) // 1.1
            foreach (var context in GetContexts(jsonObject))
                TransferBaseOwnership(jsonObject, sourceTransferData, context);

        if (container.IsVersion351PrismsWithByteBeatAuthor && sourceTransferData.TransferByteBeat) // 3.51
            TransferByteBeatOwnership(jsonObject, sourceTransferData);

        if (container.IsVersion360Frontiers && sourceTransferData.TransferSettlement) // 3.6
            foreach (var context in GetContexts(jsonObject))
                TransferCommonOwnership(jsonObject, sourceTransferData, context, "TRANSFER_UID_SETTLEMENT");
    }

    /// <summary>
    /// Generic method that transfers ownerships according to the specified path.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="sourceTransferData"></param>
    /// <param name="context"></param>
    /// <param name="pathIdentifier"></param>
    private void TransferCommonOwnership(JObject jsonObject, TransferData sourceTransferData, SaveContextQueryEnum context, string pathIdentifier)
    {
        var path = Json.GetPath(pathIdentifier, jsonObject, context, sourceTransferData.UserIdentification.UID);
        foreach (var ownership in jsonObject.SelectTokens(path).Cast<JObject>())
            TransferCommonOwnership(ownership);
    }

    /// <summary>
    /// Transfers ownerships in the specified JSON token.
    /// </summary>
    /// <param name="jsonObject"></param>
    private void TransferCommonOwnership(JObject jsonObject)
    {
        // Only UID is guaranteed.
        jsonObject.SetValue(PlatformUserIdentification.UID, "RELATIVE_OWNER_UID");

        // Replace LID, PTK, and USN if it is not empty.
        jsonObject.SetValueIfNotNullOrEmpty(PlatformUserIdentification.LID, "RELATIVE_OWNER_LID");
        jsonObject.SetValueIfNotNullOrEmpty(PlatformUserIdentification.USN, "RELATIVE_OWNER_USN");
        jsonObject.SetValueIfNotNullOrEmpty(PlatformToken, "RELATIVE_OWNER_PTK");
    }

    /// <summary>
    /// Transfers ownerships of all selected bases.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="sourceTransferData"></param>
    /// <param name="context"></param>
    private void TransferBaseOwnership(JObject jsonObject, TransferData sourceTransferData, SaveContextQueryEnum context)
    {
        var path = Json.GetPath("TRANSFER_UID_BASE", jsonObject, context);
        foreach (var persistentPlayerBase in jsonObject.SelectTokens(path).Cast<JObject>())
            if (sourceTransferData.TransferBaseUserDecision.TryGetValue(GetBaseIdentifier(persistentPlayerBase), out var userDecision) && userDecision.DoTransfer)
                TransferCommonOwnership(persistentPlayerBase.GetValue<JObject>("RELATIVE_BASE_OWNER")!);
    }

    /// <summary>
    /// Transfers ownerships of the ByteBeat library.
    /// </summary>
    /// <param name="jsonObject"></param>
    /// <param name="sourceTransferData"></param>
    private void TransferByteBeatOwnership(JObject jsonObject, TransferData sourceTransferData)
    {
        var path = Json.GetPath("TRANSFER_UID_BYTEBEAT", jsonObject, sourceTransferData.UserIdentification.UID);
        foreach (var mySong in jsonObject.SelectTokens(path).Cast<JObject>())
        {
            mySong.SetValueIfNotNullOrEmpty(PlatformUserIdentification.UID, "RELATIVE_SONG_AUTHOR_ID");
            mySong.SetValueIfNotNullOrEmpty(PlatformUserIdentification.USN, "RELATIVE_SONG_AUTHOR_USERNAME");
            mySong.SetValueIfNotNullOrEmpty(PlatformToken, "RELATIVE_SONG_AUTHOR_PLATFORM");
        }
    }

    #endregion
}
