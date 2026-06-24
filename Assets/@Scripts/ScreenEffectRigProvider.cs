using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class ScreenEffectRigProvider : MonoBehaviour
{
    [SerializeField] private RectTransform rigPrefab;
    [SerializeField] private string rigRootName = "ScreenEffectRig";

    private readonly ScreenEffectRigBuilder _builder = new();

    private ScreenEffectRig _rig;

    public IScreenEffectHost Host => EnsureRig();

    private void Awake()
    {
        EnsureRig();
    }

    private void OnEnable()
    {
        EnsureRig();
        ScreenEffectRuntime.Bind(this);
    }

    private void OnDisable()
    {
        ScreenEffectRuntime.Unbind(this);
    }

    public ScreenEffectRig EnsureRig()
    {
        if (_rig != null)
            return _rig;

        _rig = GetComponentInChildren<ScreenEffectRig>(true);

        if (_rig == null)
        {
            RectTransform rigRoot = _builder.BuildRigRoot(
                rigPrefab,
                rigRootName);

            rigRoot.SetParent(transform, false);
            StretchFull(rigRoot);

            _rig = rigRoot.GetComponent<ScreenEffectRig>();

            if (_rig == null)
                _rig = rigRoot.gameObject.AddComponent<ScreenEffectRig>();
        }

        _rig.Initialize();

        return _rig;
    }

    private static void StretchFull(RectTransform rt)
    {
        if (rt == null)
            return;

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }
}