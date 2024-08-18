// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0130:Namespace does not match folder structure", Justification = "IContainer and IPlatform implementations are moved to its own folders to improve perceptibility but should still be in the main namespace.", Scope = "namespace", Target = "~N:libNOM.io")]
