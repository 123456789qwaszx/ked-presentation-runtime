using System;

public class UIRoot<TRefs> : UIBase<TRefs>, IUIRoot, IUIResetOnAwake, IManagedUI
    where TRefs : struct, Enum
{ }