using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class UIRefValidation
{
    public static void AppendMissing<TRef>(ref string acc, Object obj, TRef key)
        where TRef : struct, Enum
    {
        if (obj) return;

        if (acc.Length > 0) acc += "\n";
        acc += $"- {key}";
    }
}

public enum ETouchEvent
{
    PointerUp,
    PointerDown,
    Click,
    LongPressed,
    BeginDrag,
    Drag,
    EndDrag,
}

public interface IUIPanel { }
public interface IUIOverlay { }
public interface IUITop { }
public interface IUIRoot { }
public interface IManagedUI { }

public abstract class UIBase : MonoBehaviour
{
    private void Awake()
    {
        PreInitialize(); // optional, mostly for derived base classes
        Initialize();    // main hook for per-UI
    }

    protected virtual void PreInitialize() { }
    protected virtual void Initialize() { }

    protected static void BindEvent(Button btn, Action<PointerEventData> action, ETouchEvent type = ETouchEvent.Click)
    {
        if (!btn) return;
        BindEvent(btn.gameObject, action, type);
    }

    public static void BindEvent(GameObject go, Action<PointerEventData> action, ETouchEvent type = ETouchEvent.Click)
    {
        UI_EventHandler evt = GetOrAddComponent<UI_EventHandler>(go);

        switch (type)
        {
            case ETouchEvent.Click:
                evt.OnClickHandler -= action;
                evt.OnClickHandler += action;
                break;
            case ETouchEvent.PointerDown:
                evt.OnPointerDownHandler -= action;
                evt.OnPointerDownHandler += action;
                break;
            case ETouchEvent.PointerUp:
                evt.OnPointerUpHandler -= action;
                evt.OnPointerUpHandler += action;
                break;
            case ETouchEvent.Drag:
                evt.OnDragHandler -= action;
                evt.OnDragHandler += action;
                break;
            case ETouchEvent.BeginDrag:
                evt.OnBeginDragHandler -= action;
                evt.OnBeginDragHandler += action;
                break;
            case ETouchEvent.EndDrag:
                evt.OnEndDragHandler -= action;
                evt.OnEndDragHandler += action;
                break;
            case ETouchEvent.LongPressed:
                evt.OnLongPressHandler -= action;
                evt.OnLongPressHandler += action;
                break;
        }
    }
    
    private static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        if (component == null)
            component = go.AddComponent<T>();
        return component;
    }
}

public abstract partial class UIBase<TRefs> : UIBase
    where TRefs : struct, Enum
{
    public sealed class RefView
    {
        private readonly UIBase<TRefs> _ui;
        internal RefView(UIBase<TRefs> ui) => _ui = ui;

        public RectTransform Rect(TRefs key) => _ui.GetRectCached(key);
        public TMP_Text Text(TRefs key) => _ui.GetCached<TMP_Text>(key);
        public Image Image(TRefs key) => _ui.GetCached<Image>(key);
        public Graphic Graphic(TRefs key) => _ui.GetCached<Graphic>(key);
        public Button Button(TRefs key) => _ui.GetCached<Button>(key);
        public CanvasGroup CanvasGroup(TRefs key) => _ui.GetOrAddCanvasGroupCached(key);
        
        public T Widget<T>(TRefs key) where T : UIBase => _ui.GetCached<T>(key);
    }

    private GameObject[] _gos;
    private readonly Dictionary<(int, Type), Component> _componentCache = new();
    private bool _refsBuilt;

    protected RefView View { get; private set; }

    protected virtual void OnDestroy() { _componentCache.Clear(); }

    #region PreInitialize
    protected override void PreInitialize()
    {
        if (_refsBuilt) return;

        BindObjects();
        View = new RefView(this);
        
        if (this is IUIRoot || this is IUIPanel || this is IUIOverlay || this is IUITop)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            
            //gameObject.SetActive(false);
            CloseByCanvasGroup(gameObject);
        }

        _refsBuilt = true;
    }

    private void BindObjects()
    {
        var objNames = Enum.GetNames(typeof(TRefs));
        _gos = new GameObject[objNames.Length];

        for (int i = 0; i < objNames.Length; i++)
        {
            GameObject go = FindChildGameObjectRecursive(gameObject, objNames[i], true);
            _gos[i] = go;

            //if (go == null) Debug.LogWarning($"[UIBase] Failed to bind GameObject: '{objNames[i]}'", this);
        }
    }
    #endregion

    private GameObject TryGetBoundGameObject(TRefs key)
    {
        if (!_refsBuilt)
        {
            Debug.LogError($"[UIBase] PreInitialize() was not called before resolving refs. type={GetType().Name}", this);
            return null;
        }

        if (_gos == null)
        {
            Debug.LogWarning($"[UIBase] BindObjects() was not called. key={key}", this);
            return null;
        }

        int idx = Convert.ToInt32(key);
        if ((uint)idx >= (uint)_gos.Length)
        {
            Debug.LogWarning(
                $"[UIBase] Ref index out of range. key={key}, idx={idx}, len={_gos.Length} ({typeof(TRefs).Name})",
                this);
            return null;
        }

        GameObject go = _gos[idx];
        if (!go)
        {
//            Debug.LogWarning($"[UIBase] Missing bound GameObject. key={key}, idx={idx} ({typeof(TRefs).Name})",this);
        }

        return go;
    }

    #region Cache
    private T GetCached<T>(TRefs key) where T : Component
    {
        int idx = Convert.ToInt32(key);
        var cacheKey = (idx, typeof(T));

        if (_componentCache.TryGetValue(cacheKey, out var cached) && cached)
            return (T)cached;

        var go = TryGetBoundGameObject(key);
        if (!go) return null;

        if (!go.TryGetComponent(out T comp) || !comp)
            return null;

        _componentCache[cacheKey] = comp;
        return comp;
    }

    private CanvasGroup GetOrAddCanvasGroupCached(TRefs key)
    {
        int idx = Convert.ToInt32(key);
        var cacheKey = (idx, typeof(CanvasGroup));

        if (_componentCache.TryGetValue(cacheKey, out var cached) && cached)
            return (CanvasGroup)cached;

        var go = TryGetBoundGameObject(key);
        if (!go) return null;

        if (!go.TryGetComponent(out CanvasGroup cg) || !cg)
        {
            Debug.LogWarning($"CanvasGroup missing. Auto-adding. key={key}", go);
            cg = go.AddComponent<CanvasGroup>();
        }

        _componentCache[cacheKey] = cg;
        return cg;
    }

    private RectTransform GetRectCached(TRefs key)
    {
        var rt = GetCached<RectTransform>(key);
        if (rt) return rt;

        var g = GetCached<Graphic>(key);
        if (g)
        {
            var rectFromGraphic = g.rectTransform;
            if (rectFromGraphic)
            {
                int idx = Convert.ToInt32(key);
                _componentCache[(idx, typeof(RectTransform))] = rectFromGraphic;
                return rectFromGraphic;
            }
        }

        var go = TryGetBoundGameObject(key);
        if (!go) return null;

        var tr = go.transform as RectTransform;
        if (tr)
        {
            int idx = Convert.ToInt32(key);
            _componentCache[(idx, typeof(RectTransform))] = tr;
        }

        return tr;
    }
    #endregion

    #region Helper
    private GameObject FindChildGameObjectRecursive(GameObject go, string name = null, bool recursive = true)
    {
        Transform transform = FindChildGameObjectRecursive<Transform>(go, name, recursive);
        if (transform == null)
            return null;

        return transform.gameObject;
    }

    private T FindChildGameObjectRecursive<T>(GameObject go, string name = null, bool recursive = true) where T : Object
    {
        if (go == null)
            return null;

        if (recursive == false)
        {
            for (int i = 0; i < go.transform.childCount; i++)
            {
                Transform t = go.transform.GetChild(i);
                if (string.IsNullOrEmpty(name) || t.name == name)
                {
                    T component = t.GetComponent<T>();
                    if (component != null)
                        return component;
                }
            }
        }
        else
        {
            foreach (T component in go.GetComponentsInChildren<T>(true))
            {
                if (string.IsNullOrEmpty(name) || component.name == name)
                    return component;
            }
        }

        return null;
    }
    
    private static void CloseByCanvasGroup(GameObject root)
    {
        if (!root) return;

        // 루트에 CanvasGroup이 없으면 추가 (한 번만)
        if (!root.TryGetComponent(out CanvasGroup cg) || !cg)
        {
            Debug.Log($"[UIBase] CanvasGroup missing on root. Auto-added for safety. root={root.name}" );
            cg = root.AddComponent<CanvasGroup>();
        }

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }
    
    #endregion
}