using System;

public class UIRoot<TRefs> : UIBase<TRefs>, IUIRoot
    where TRefs : struct, Enum
{ }