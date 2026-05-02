using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Presentation shot response의 중심 런타임.
/// PresentationUIRoot의 ref 스키마를 기준으로 binding / focus provider를 자동 구성할 수 있다.
/// </summary>
public sealed class PresentationResponseRig : MonoBehaviour
{
    [Header("Root References")]
    [SerializeField] private PresentationUIRoot _presentationRoot;
    [SerializeField] private RectTransform _spaceRoot;

    [Header("Shot Settings")]
    [SerializeField] private Vector2 _defaultFramingPoint = Vector2.zero;
    [SerializeField] private Vector2 _manualPanPixelsPerUnit = new Vector2(64f, 36f);

    [Header("Bindings")]
    [SerializeField] private List<PresentationResponseBinding> _bindings = new List<PresentationResponseBinding>();

    [Header("Focus Providers")]
    [SerializeField] private List<NamedFocusProvider> _focusProviders = new List<NamedFocusProvider>();

    private PresentationIntentState _currentState;

    public PresentationUIRoot PresentationRoot => _presentationRoot;
    public RectTransform SpaceRoot => _spaceRoot;
    public Vector2 DefaultFramingPoint => _defaultFramingPoint;
    public Vector2 ManualPanPixelsPerUnit => _manualPanPixelsPerUnit;
    public PresentationIntentState CurrentState => _currentState;

    private void Reset()
    {
        TryResolvePresentationRoot();
        TryResolveSpaceRoot();
    }

    private void Awake()
    {
        TryResolvePresentationRoot();
        TryResolveSpaceRoot();
        _currentState = PresentationIntentState.Default;
    }

    public void ApplyImmediate(PresentationIntentState state)
    {
        _currentState = state;
        ApplyToAllBindings(in state);
    }

    public void SetCurrentStateOnly(PresentationIntentState state)
    {
        _currentState = state;
    }

    // focus reframe 계산의 기준점을 제공
    public Vector2 ComposePanForFocus(Vector2 focusPoint)
    {
        return ComposePanForFocus(focusPoint, _defaultFramingPoint);
    }

    public Vector2 ComposePanForFocus(Vector2 focusPoint, Vector2 desiredFramingPoint)
    {
        return desiredFramingPoint - focusPoint;
    }

    public Vector2 GetManualPanPixels(Vector2 authoringPanUnits)
    {
        return new Vector2(
            authoringPanUnits.x * _manualPanPixelsPerUnit.x,
            authoringPanUnits.y * _manualPanPixelsPerUnit.y);
    }

    public Vector3 SpaceToWorldPoint(Vector2 pointInRigSpace)
    {
        if (_spaceRoot == null)
            return new Vector3(pointInRigSpace.x, pointInRigSpace.y, 0f);

        return _spaceRoot.TransformPoint(new Vector3(pointInRigSpace.x, pointInRigSpace.y, 0f));
    }

    public Vector2 WorldToSpacePoint(Vector3 worldPoint)
    {
        if (_spaceRoot == null)
            return new Vector2(worldPoint.x, worldPoint.y);

        Vector3 local = _spaceRoot.InverseTransformPoint(worldPoint);
        return new Vector2(local.x, local.y);
    }

    public bool TryGetFocusPoint(string key, out Vector2 focusPoint)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            focusPoint = Vector2.zero;
            return false;
        }

        for (int i = 0; i < _focusProviders.Count; i++)
        {
            NamedFocusProvider named = _focusProviders[i];
            if (named == null || named.provider == null)
                continue;

            if (!string.Equals(named.key, key, System.StringComparison.OrdinalIgnoreCase))
                continue;

            IPresentationFocusProvider provider = named.provider as IPresentationFocusProvider;
            if (provider == null)
            {
                Debug.LogWarning($"[PresentationResponseRig] Focus provider '{named.key}' does not implement IPresentationFocusProvider.");
                focusPoint = Vector2.zero;
                return false;
            }

            focusPoint = provider.GetFocusPoint();
            return true;
        }

        focusPoint = Vector2.zero;
        return false;
    }

    public bool TryGetGroupFocusPoint(IReadOnlyList<string> keys, out Vector2 focusPoint)
    {
        Vector2 sum = Vector2.zero;
        int count = 0;

        if (keys != null)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                if (TryGetFocusPoint(keys[i], out Vector2 point))
                {
                    sum += point;
                    count++;
                }
            }
        }

        if (count > 0)
        {
            focusPoint = sum / count;
            return true;
        }

        focusPoint = Vector2.zero;
        return false;
    }

    [ContextMenu("PRS/Auto Wire From PresentationUIRoot")]
    public void AutoWireFromPresentationUIRoot()
    {
        TryResolvePresentationRoot();
        if (_presentationRoot == null)
        {
            Debug.LogWarning("[PresentationResponseRig] PresentationUIRoot를 찾지 못했습니다.");
            return;
        }

        _spaceRoot = _presentationRoot.ResolveRect(PresentationUIRoot.Refs.Stage_Root);

        _bindings.Clear();
        _focusProviders.Clear();

        AddBinding("bg", _presentationRoot.ResolveRect(PresentationUIRoot.Refs.BGContent_Root), PresentationResponseProfile.Background);
        AddBinding("bg_overlay", _presentationRoot.ResolveRect(PresentationUIRoot.Refs.BGOverlay_Root), PresentationResponseProfile.Prop);
        AddBinding("left", _presentationRoot.ResolveRect(PresentationUIRoot.Refs.CharSlotLeft_Root), PresentationResponseProfile.CharacterSlot);
        AddBinding("center", _presentationRoot.ResolveRect(PresentationUIRoot.Refs.CharSlotCenter_Root), PresentationResponseProfile.CharacterSlot);
        AddBinding("right", _presentationRoot.ResolveRect(PresentationUIRoot.Refs.CharSlotRight_Root), PresentationResponseProfile.CharacterSlot);
        AddBinding("foreground", _presentationRoot.ResolveRect(PresentationUIRoot.Refs.Foreground_Root), PresentationResponseProfile.Foreground);

        AddFocusProvider("left", _presentationRoot.ResolveRect(PresentationUIRoot.Refs.CharSlotLeftFocus_Root));
        AddFocusProvider("center", _presentationRoot.ResolveRect(PresentationUIRoot.Refs.CharSlotCenterFocus_Root));
        AddFocusProvider("right", _presentationRoot.ResolveRect(PresentationUIRoot.Refs.CharSlotRightFocus_Root));

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif

        Debug.Log("[PresentationResponseRig] Auto-wired bindings/focus providers from PresentationUIRoot.");
    }

    [ContextMenu("PRS/Capture Base Pose")]
    public void CaptureBasePose()
    {
        TryResolveSpaceRoot();

        for (int i = 0; i < _bindings.Count; i++)
            _bindings[i]?.CaptureBasePose(this);

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif

        Debug.Log($"[PresentationResponseRig] Captured base pose for {_bindings.Count} bindings.");
    }

    [ContextMenu("PRS/Reset To Default")]
    public void ResetToDefault()
    {
        ApplyImmediate(PresentationIntentState.Default);
    }

    private void ApplyToAllBindings(in PresentationIntentState state)
    {
        for (int i = 0; i < _bindings.Count; i++)
            _bindings[i]?.Apply(in state);
    }

    private void TryResolvePresentationRoot()
    {
        if (_presentationRoot != null)
            return;

        _presentationRoot = GetComponentInParent<PresentationUIRoot>(true);
    }

    private void TryResolveSpaceRoot()
    {
        if (_spaceRoot != null)
            return;

        TryResolvePresentationRoot();
        if (_presentationRoot != null)
            _spaceRoot = _presentationRoot.ResolveRect(PresentationUIRoot.Refs.Stage_Root);

        if (_spaceRoot == null)
            _spaceRoot = transform as RectTransform;
    }

    private void AddBinding(string key, RectTransform rect, PresentationResponseProfile preset)
    {
        if (rect == null)
            return;

        RectTransformResponseTarget target = rect.GetComponent<RectTransformResponseTarget>();
        if (target == null)
            target = rect.gameObject.AddComponent<RectTransformResponseTarget>();

        PresentationResponseBinding binding = new PresentationResponseBinding
        {
            key = key,
            profile = CloneProfile(preset),
        };
        binding.SetTarget(target);
        _bindings.Add(binding);
    }

    private void AddFocusProvider(string key, RectTransform rect)
    {
        if (rect == null)
            return;

        RectTransformFocusProvider provider = rect.GetComponent<RectTransformFocusProvider>();
        if (provider == null)
            provider = rect.gameObject.AddComponent<RectTransformFocusProvider>();

        _focusProviders.Add(new NamedFocusProvider
        {
            key = key,
            provider = provider,
        });
    }

    private static PresentationResponseProfile CloneProfile(PresentationResponseProfile src)
    {
        return new PresentationResponseProfile
        {
            basePositionInRigSpace = src.basePositionInRigSpace,
            baseScale = src.baseScale,
            baseAlpha = src.baseAlpha,
            maxZoomScaleDelta = src.maxZoomScaleDelta,
            maxZoomSpreadPixels = src.maxZoomSpreadPixels,
            panResponse = src.panResponse,
        };
    }

    [System.Serializable]
    public sealed class NamedFocusProvider
    {
        public string key;
        public MonoBehaviour provider;
    }
}
