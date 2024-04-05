using libNOM.io.Interfaces;
using libNOM.map;

using Newtonsoft.Json.Linq;

namespace libNOM.io;


// This partial class contains accessor related code.
public partial class Container : IContainer
{
    #region Field

    private JObject? _jsonObject;

    #endregion

    #region Getter

    public JObject GetJsonObject()
    {
        ThrowHelperIsLoaded();
        return _jsonObject!;
    }

    public JToken? GetJsonToken(string pathIdentifier) => GetJsonToken(pathIdentifier, ActiveContext);

    public JToken? GetJsonToken(string pathIdentifier, SaveContextQueryEnum context)
    {
        ThrowHelperIsLoaded();
        return _jsonObject!.GetValue<JToken>(pathIdentifier, context);
    }

    public IEnumerable<JToken> GetJsonTokens(string pathIdentifier) => GetJsonTokens(pathIdentifier, ActiveContext);

    public IEnumerable<JToken> GetJsonTokens(string pathIdentifier, SaveContextQueryEnum context)
    {
        ThrowHelperIsLoaded();
        return _jsonObject!.GetValues<JToken>(pathIdentifier, context);
    }

    public T? GetJsonValue<T>(ReadOnlySpan<int> indices)
    {
        ThrowHelperIsLoaded();
        return _jsonObject!.GetValue<T>(indices);
    }

    public T? GetJsonValue<T>(string pathIdentifier) => GetJsonValue<T>(pathIdentifier, ActiveContext);

    public T? GetJsonValue<T>(string pathIdentifier, SaveContextQueryEnum context)
    {
        ThrowHelperIsLoaded();
        return _jsonObject!.GetValue<T>(pathIdentifier, context);
    }

    public IEnumerable<T?> GetJsonValues<T>(string pathIdentifier) => GetJsonValues<T>(pathIdentifier, ActiveContext);

    public IEnumerable<T?> GetJsonValues<T>(string pathIdentifier, SaveContextQueryEnum context)
    {
        ThrowHelperIsLoaded();
        return _jsonObject!.GetValues<T>(pathIdentifier, context);
    }

    #endregion

    #region Setter

    public void SetJsonObject(JObject? value)
    {
        // No ThrowHelperIsLoaded as setting this will determine the result.
        _jsonObject = value;

        IsSynced = false;

        // Make sure the data are always in the format that was set in the settings.
        if (_jsonObject is not null && Platform is not null) // happens when the container is unloaded
            if (Platform.Settings.UseMapping)
            {
                UnknownKeys = Mapping.Deobfuscate(_jsonObject, IsAccount);
            }
            else
            {
                Mapping.Obfuscate(_jsonObject, IsAccount);
            }
    }

    public void SetJsonValue(JToken value, ReadOnlySpan<int> indices)
    {
        ThrowHelperIsLoaded();
        // If setting the value was successful, it is not synced anymore.
        IsSynced = !_jsonObject!.SetValue(value, indices);
    }

    public void SetJsonValue(JToken value, string pathIdentifier) => SetJsonValue(value, pathIdentifier, ActiveContext);

    public void SetJsonValue(JToken value, string pathIdentifier, SaveContextQueryEnum context)
    {
        ThrowHelperIsLoaded();
        // If setting the value was successful, it is not synced anymore.
        IsSynced = !_jsonObject!.SetValue(value, pathIdentifier, context);
    }

    public void SetWatcherChange(WatcherChangeTypes changeType)
    {
        HasWatcherChange = true;
        WatcherChangeType = changeType;
    }

    #endregion
}
