namespace libNOM.io;


/// <summary>
/// Implementation for the Steam platform.
/// </summary>
// This partial class contains reading and processing related code.
public partial class PlatformSteam : Platform
{
    #region Meta

    protected override ReadOnlySpan<uint> DecryptMeta(Container container, ReadOnlySpan<byte> meta)
    {
        var result = base.DecryptMeta(container, meta);

        // Ensure it has a known length before continuing decrypting.
        if (meta.Length != META_LENGTH_TOTAL_VANILLA && meta.Length != META_LENGTH_TOTAL_WAYPOINT && meta.Length != META_LENGTH_TOTAL_WORLDS)
            return result;

        // Best case is that it works with the value of the file but in case it was moved manually, try all other values as well.
        var enumValues = new StoragePersistentSlotEnum[] { container.PersistentStorageSlot }.Concat(EnumExtensions.GetValues<StoragePersistentSlotEnum>().Where(i => i != container.PersistentStorageSlot && (container.IsAccount ? i <= StoragePersistentSlotEnum.AccountData : i > StoragePersistentSlotEnum.AccountData)));
        var iterations = meta.Length == META_LENGTH_TOTAL_VANILLA ? 8 : 6; // byte
        var lastIndex = result.Length - 1; // uint

        foreach (var entry in enumValues)
        {
            var decrypted = DecryptMetaStorageEntry(entry, iterations, lastIndex, result);
            if (decrypted[0] == META_HEADER)
                return decrypted;
        }

        return result;
    }

    private Span<uint> DecryptMetaStorageEntry(StoragePersistentSlotEnum storage, int iterations, int lastIndex, ReadOnlySpan<uint> meta)
    {
        // When overwriting META_ENCRYPTION_KEY[0] it can happen that the value is not set afterwards and therefore create a new collection to ensure it will be correct.
        ReadOnlySpan<uint> key = [(((uint)(storage) ^ 0x1422CB8C).RotateLeft(13) * 5) + 0xE6546B64, META_ENCRYPTION_KEY[1], META_ENCRYPTION_KEY[2], META_ENCRYPTION_KEY[3]];

        // DeepCopy as value would be changed otherwise and casting again does not work.
        Span<uint> result = Common.DeepCopy(meta);

        uint hash = 0;

        // Results in 0xF1BBCDC8 for META_FORMAT_1 as in the original algorithm.
        for (int i = 0; i < iterations; i++)
            hash += 0x9E3779B9;

        for (int i = 0; i < iterations; i++)
        {
            uint current = result[0];
            int keyIndex = (int)(hash >> 2 & 3);
            int valueIndex = lastIndex;

            for (int j = lastIndex; j > 0; j--, valueIndex--)
            {
                //uint j1 = (current >> 3) ^ (result[valueIndex - 1] << 4);
                //uint j2 = (current * 4) ^ (result[valueIndex - 1] >> 5);
                //uint j3 = (result[valueIndex - 1] ^ key[(j & 3) ^ keyIndex]);
                //uint j4 = (current ^ hash);
                //result[valueIndex] -= (j1 + j2) ^ (j3 + j4);
                result[valueIndex] -= (((current >> 3) ^ (result[valueIndex - 1] << 4)) + ((current * 4) ^ (result[valueIndex - 1] >> 5))) ^ ((result[valueIndex - 1] ^ key[(j & 3) ^ keyIndex]) + (current ^ hash));
                current = result[valueIndex];
            }

            valueIndex = lastIndex;

            //uint i1 = (current >> 3) ^ (result[valueIndex] << 4);
            //uint i2 = (current * 4) ^ (result[valueIndex] >> 5);
            //uint i3 = (result[valueIndex] ^ key[keyIndex]); // [(0 & 3) ^ keyIndex] as in j3 to be precise (0 would be the last executed index) but (0 & 3) is always zero and therefore does not matter
            //uint i4 = (current ^ hash);
            //result[0] -= (i1 + i2) ^ (i3 + i4);
            result[0] -= (((current >> 3) ^ (result[valueIndex] << 4)) + ((current * 4) ^ (result[valueIndex] >> 5))) ^ ((result[valueIndex] ^ key[keyIndex]) + (current ^ hash));

            hash += 0x61C88647;
        }

        return result;
    }

    #endregion
}
