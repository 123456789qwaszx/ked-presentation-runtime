using System;

public interface IUIPage { }

public class UIPage<TRefs> : UIBase<TRefs>, IUIPage, IUIResetOnAwake, IManagedUI
    where TRefs : struct, Enum
{ }
