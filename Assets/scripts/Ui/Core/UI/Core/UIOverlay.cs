using System;

public class UIOverlay<TRefs> : UIBase<TRefs>, IUIOverlay, IUIResetOnAwake, IManagedUI
    where TRefs : struct, Enum
{ }