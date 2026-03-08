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
    public static UIManager Instance { get; private set; }

    [Header("Singleton")]
    [SerializeField] private bool _dontDestroyOnLoad = true;
    private void Awake()
    {
        // Singleton guard
        if (Instance != null && Instance != this)
        {
            // 중복 인스턴스 제거
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        Init();
    }

    // ---- Patch pipeline (optional)
    private int _showVersion; // late coroutine 방지용 토큰

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
            if (!IsManagedScreen(ui))
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

    private static bool IsManagedScreen(UIBase ui)
    {
        return ui is IUIRoot || ui is IUIPanel || ui is IUIOverlay || ui is IUITop || ui is IManagedUI;
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

    private void BumpShowVersion()
    {
        unchecked { _showVersion++; }
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