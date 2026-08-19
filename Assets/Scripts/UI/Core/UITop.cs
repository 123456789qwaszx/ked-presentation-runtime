using System;

public class UITop<TRefs> : UIBase<TRefs>, IUITop, IUIResetOnAwake, IManagedUI
    where TRefs : struct, Enum
{ }