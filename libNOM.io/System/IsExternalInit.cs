// See https://github.com/dotnet/roslyn/issues/45510#issuecomment-725091019 for more information.
#if NETSTANDARD2_0_OR_GREATER

using System.ComponentModel;

namespace System.Runtime.CompilerServices;

[EditorBrowsable(EditorBrowsableState.Never)]
internal static class IsExternalInit { }

#endif
