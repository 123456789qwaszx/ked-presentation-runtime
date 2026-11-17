using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// 덮개는 장면 전역 의도다 — 커맨드가 "덮여 있어야 한다 / 걷혀 있어야 한다"를 쓰고, 화면은 이 컨트롤러가 따라간다(IntentTransition).
// 전환은 이 컨트롤러가 갖는다 — 줄이 바뀌어도 제 길이로 간다(docs/PLAN_screen-cover.md §3.1 · D7).
// 단색 그림 하나라 머티리얼이 없다. Image의 색만 쓴다 — RGB = 덮개의 색, 알파 = 덮인 정도. 다 걷히면 그림을 끈다.
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class ScreenCoverEffectController :
    MonoBehaviour,
    IScreenEffectController
{
    public readonly struct State
    {
        /// <summary>덮개의 색. 알파는 쓰지 않는다(1로 둔다) — 덮인 정도는 Amount다.</summary>
        public readonly Color Color;

        /// <summary>덮인 정도. 0 = 걷힘, 1 = 다 덮임.</summary>
        public readonly float Amount;

        public State(Color color, float amount)
        {
            Color = new Color(color.r, color.g, color.b, 1f);
            Amount = Mathf.Clamp01(amount);
        }

        public static State Lerp(State from, State to, float t)
        {
            t = Mathf.Clamp01(t);

            return new State(
                Color.Lerp(from.Color, to.Color, t),
                Mathf.Lerp(from.Amount, to.Amount, t));
        }
    }

    [Header("Target")]
    [SerializeField] private Image targetImage;

    private bool _stateInitialized;
    private Color _color;
    private float _amount;

    private IntentTransition<State> _transition;

    public Color Color => _color;
    public float Amount => _amount;

    public State Presented => new(_color, _amount);
    public State Intent => Transition.Intent;
    public bool IsTransitioning => Transition.IsTransitioning;

    // 덮개는 머티리얼이 없다 — 빌더가 Resources에서 찾지 않는다.
    public Material SourceMaterial => null;

    private IntentTransition<State> Transition
    {
        get
        {
            if (_transition != null)
                return _transition;

            InitializeStateIfNeeded();
            _transition = new IntentTransition<State>(Presented, () => Presented, ApplyState, State.Lerp, this);
            return _transition;
        }
    }

    private void Reset() => EnsureTarget();

    private void Awake()
    {
        EnsureTarget();
        InitializeStateIfNeeded();
        ApplyToImage();
    }

    private void OnDestroy()
    {
        KillTween(false);
    }

    public void Bind(Image image, Material sourceMaterial)
    {
        if (image != null)
            targetImage = image;

        EnsureTarget();
        InitializeStateIfNeeded();
        ApplyToImage();
    }

    public void Follow(in State intent, float duration, Ease ease) => Transition.Follow(intent, duration, ease);

    public void Snap(in State intent) => Transition.Snap(intent);

    // 기준 상태 — 전환을 멈추고 걷는다(색은 남긴다).
    public void ClearImmediate()
    {
        Snap(new State(_color, 0f));
    }

    public void KillTween(bool complete)
    {
        if (complete)
            Snap(Intent);
        else
            Transition.Stop();
    }

    private void ApplyState(State state)
    {
        InitializeStateIfNeeded();

        _color = state.Color;
        _amount = state.Amount;

        ApplyToImage();
    }

    private void ApplyToImage()
    {
        if (targetImage == null)
            return;

        targetImage.raycastTarget = false;
        targetImage.color = new Color(_color.r, _color.g, _color.b, _amount);

        // 다 걷힌 덮개는 그리지 않는다 — 평소에는 비용이 없다.
        targetImage.enabled = _amount > 0f;
    }

    private void InitializeStateIfNeeded()
    {
        if (_stateInitialized)
            return;

        _color = Color.black;
        _amount = 0f;
        _stateInitialized = true;
    }

    private void EnsureTarget()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }
}
