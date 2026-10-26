using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class UITypeCache<T>
{
    public static readonly Type Type = typeof(T);
    public static readonly string Name = Type.Name;
}

public partial class UIManager : MonoBehaviour
{
    [SerializeField] private bool _dontDestroyOnLoad = true;

    // ---- UI registry / stack
    private readonly Dictionary<Type, UIBase> _uiMap = new();
    private readonly Stack<UIBase> _panelStack = new();

    [Header("Layer Slots")]
    [SerializeField] private Transform _layerUIRoot;
    [SerializeField] private Transform _layerPanels;
    [SerializeField] private Transform _layerOverlay;
    [SerializeField] private Transform _layerTop;

    [Header("Panel Stack Visual Policy")]
    [SerializeField, Min(1)] private int _keepAliveDepth = 3;

    [SerializeField, Range(0f, 1f)] private float _coveredAlpha = 0.9f;

    public bool HasPanel => _panelStack.Count > 0;
    
    public UIBase CurSceneRoot { get; private set; }
    

    // ----------------------------
    // Initialize
    // ----------------------------
    public void Init()
    {
        if (_dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        DisableLayerSelfRaycasts();
        RegisterChildUIs();
    }

    private void RegisterChildUIs()
    {
        _uiMap.Clear();
        ClearPlacementCacheForEditor();

        RegisterLayer(transform);

        // Init() 이후 등록된 관리 UI는 준비 완료라는 사후조건을 보장한다.
        // 씬의 관리 UI는 Awake에서 이미 초기화를 마쳤으므로 대개 no-op이다.
        UIInitializer.Run(_uiMap.Values);
    }

    private void RegisterLayer(Transform layer)
    {
        if (layer == null)
            return;

        UIBase[] list = layer.GetComponentsInChildren<UIBase>(includeInactive: true);

        foreach (UIBase ui in list)
        {
            if (ui is not IManagedUI)
                continue;

            Type key = ui.GetType();

            EnsureCanvasGroup(ui);
            CaptureInitialPositionForEditor(ui);

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
        Type key = UITypeCache<T>.Type;

        if (!_uiMap.TryGetValue(key, out UIBase ui))
            return null;

        return (T)ui;
    }

    private bool TryResolve<T>(string kind, out T typed) where T : UIBase
    {
        Type key = UITypeCache<T>.Type;

        if (!_uiMap.TryGetValue(key, out UIBase raw))
        {
            Debug.LogError($"[UIManager] {kind} not registered: {UITypeCache<T>.Name}", this);
            typed = null;
            return false;
        }

        typed = (T)raw;
        return true;
    }

    private void Mount(UIBase ui, Transform slot)
    {
        if (ui == null)
            return;

        if (slot != null)
            ui.transform.SetParent(slot, worldPositionStays: false);

        ApplyMountPlacement(ui);

        ui.transform.SetAsLastSibling();
    }

    private void HideManagedUI(UIBase ui)
    {
        if (ui == null)
            return;

        ApplyState(
            ui,
            active: false,
            interactable: false,
            blocksRaycasts: false,
            alpha: 0f);

        RestoreInitialPositionForEditor(ui);
    }

    private static void ApplyState(
        UIBase ui,
        bool active,
        bool interactable,
        bool blocksRaycasts,
        float alpha)
    {
        if (ui == null)
            return;

        ui.gameObject.SetActive(active);

        CanvasGroup canvasGroup = ui.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = active ? alpha : 0f;
        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = blocksRaycasts;
    }

    private static void EnsureCanvasGroup(UIBase ui)
    {
        if (ui == null)
            return;

        CanvasGroup canvasGroup = ui.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            ui.gameObject.AddComponent<CanvasGroup>();
    }
    
    
    private void DisableLayerSelfRaycasts()
    {
        DisableLayerSelfRaycast(transform);
        DisableLayerSelfRaycast(_layerUIRoot);
        DisableLayerSelfRaycast(_layerPanels);
        DisableLayerSelfRaycast(_layerOverlay);
        DisableLayerSelfRaycast(_layerTop);
    }

    private static void DisableLayerSelfRaycast(Transform layer)
    {
        if (layer == null)
            return;

        Graphic graphic = layer.GetComponent<Graphic>();
        if (graphic != null)
            graphic.raycastTarget = false;
    }
}