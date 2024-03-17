using Newtonsoft.Json.Linq;

namespace libNOM.io;


// This partial class contains UserIdentification related code.
public partial class PlatformMicrosoft : Platform
{
    protected override string GetUserIdentification(JObject jsonObject, string key)
    {
        if (key is "UID" && _uid is not null)
            return _uid;

        return base.GetUserIdentification(jsonObject, key);
    }
}
