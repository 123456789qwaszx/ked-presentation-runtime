using System;
using System.Collections.Generic;
using UnityEngine;

public static class UITypeCache<T>
{
    public static readonly Type Type = typeof(T);
    public static readonly string Name = Type.Name;
}

public partial class UIManager : MonoBehaviour
{
    #region Singleton
    public static UIManager Instance { get; private set; }

    [Header("Singleton")]
    [SerializeField] private bool _dontDestroyOnLoad = true;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        Init();
    }
    #endregion
    
    // ---- UI registry / stack
    private readonly Dictionary<Type, UIBase> _uiMap = new();
    private readonly Stack<UIBase> _panelStack = new();

    [Header("Layer Slots")]
    [SerializeField] private Transform _layerUIRoot;
    [SerializeField] private Transform _layerPanels;
    [SerializeField] private Transform _layerOverlay;
    [SerializeField] private Transform _layerTop;

    [Header("Panel Stack Visual Policy")]
    [SerializeField, Min(1)] private int _keepAliveDepth = 2;

    [SerializeField, Range(0f, 1f)] private float _coveredAlpha = 0f;

    public UIBase CurSceneRoot { get; private set; }

    // ----------------------------
    // Initialize
    // ----------------------------
    public void Init()
    {
        RegisterChildUIs();
    }

    private void RegisterChildUIs()
    {
        _uiMap.Clear();
        RegisterLayer(transform);
    }

    private void RegisterLayer(Transform layer)
    {
        if (layer == null) return;

        var list = layer.GetComponentsInChildren<UIBase>(includeInactive: true);
        foreach (var ui in list)
        {
            if (ui is not IManagedUI)
                continue;

            var key = ui.GetType();

            EnsureCanvasGroup(ui);

            if (_uiMap.ContainsKey(key))
            {
                Debug.LogWarning($"[UIManager] Duplicate managed UI detected: {key.Name}", ui);
                continue;
            }

            _uiMap.Add(key, ui);
        }
    }

    // ----------------------------
    // Public API
    // ----------------------------
    public T GetUI<T>() where T : UIBase
    {
        var key = UITypeCache<T>.Type;

        if (!_uiMap.TryGetValue(key, out UIBase ui))
        {
            //Debug.LogWarning($"[UIManager] Missing UI: {UITypeCache<T>.Name}", this);
            return null;
        }

        return (T)ui;
    }

    private bool TryResolve<T>(string kind, out T typed) where T : UIBase
    {
        var key = UITypeCache<T>.Type;

        if (!_uiMap.TryGetValue(key, out var raw))
        {
            Debug.LogError($"[UIManager] {kind} not registered: {UITypeCache<T>.Name}", this);
            typed = null;
            return false;
        }

        typed = (T)raw;
        return true;
    }

    private static void Mount(UIBase ui, Transform slot)
    {
        if (slot != null)
            ui.transform.SetParent(slot, worldPositionStays: false);

        ui.transform.SetAsLastSibling();
    }

    private static void ApplyState(UIBase ui, bool active, bool interactable, bool blocksRaycasts, float alpha)
    {
        if (ui == null) return;

        ui.gameObject.SetActive(active);

        var canvasGroup = ui.GetComponent<CanvasGroup>();
        if (canvasGroup == null) return;

        canvasGroup.alpha = active ? alpha : 0f;
        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = blocksRaycasts;
    }

    private static void EnsureCanvasGroup(UIBase ui)
    {
        if (ui == null) return;
        var canvasGroup = ui.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            ui.gameObject.AddComponent<CanvasGroup>();
    }
}