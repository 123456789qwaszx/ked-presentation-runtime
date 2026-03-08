using System;

public class UIPanel<TRefs> : UIBase<TRefs>, IUIPanel
    where TRefs : struct, Enum
{ }