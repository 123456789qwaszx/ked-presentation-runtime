using System;

public class UIPanel<TRefs> : UIBase<TRefs>, IUIPanel, IUIResetOnAwake, IManagedUI
    where TRefs : struct, Enum
{ }