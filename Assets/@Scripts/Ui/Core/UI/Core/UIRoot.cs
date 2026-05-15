using System;
using UnityEngine;
using UnityEngine.UI;

public class UIRoot<TRefs> : UIBase<TRefs>, IUIRoot, IUIResetOnAwake, IManagedUI
    where TRefs : struct, Enum
{
    public RectTransform ResolveRect(TRefs key) => View.Rect(key);
    public CanvasGroup ResolveCanvasGroup(TRefs key) => View.CanvasGroup(key);
    public Image ResolveImage(TRefs key) => View.Image(key);
}