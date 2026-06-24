using System;
using UnityEngine;
using UnityEngine.UI;

public class UIRoot<TRefs> : UIBase<TRefs>, IUIRoot, IUIResetOnAwake, IManagedUI
    where TRefs : struct, Enum
{ }