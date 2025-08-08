using System;

namespace eforms_middleware.Constants;

[Flags]
public enum PermissionFlag: byte
{
    None = 0,
    View = 1,
    Edit = 1 << 1,
    GroupFallback = 1 << 2,
    UserActionable = View | Edit
}
