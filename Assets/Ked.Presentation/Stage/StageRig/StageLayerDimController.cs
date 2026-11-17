using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// 층의 어둡기 판 — 층 d의 Content 다음 형제로 서서, 먼 층부터 d까지 그려진 것을 모두 어둡게 한다(docs/PLAN_screen-cover.md §3.2).
// 캐릭터를 건드리지 않는다 — 리그는 전처럼 Content 밑에 서고, 판은 그 위를 검게 가릴 뿐이다.
// 선형 색 공간이라 투명도 α의 검은 판은 그 아래 밝기를 ×(1 − α)로 만든다 — 색을 곱한 것과 똑같이 보인다.
// 의도형 — 커맨드(stage_dim)가 세기를 쓰고 판이 IntentTransition으로 따라간다. 전환은 판이 가져 스텝 경계에 묶이지 않는다(줄을 넘는다).
// 판은 무대보다 넉넉히 크다(빌더가 정한다) — 층은 카메라를 타므로 넓히기 · 기울이기 · 옮기기에도 가장자리가 드러나지 않게. 무대 창 밖은 마스크가 자른다.
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class StageLayerDimController : MonoBehaviour
{
    [SerializeField] private Image targetImage;

    private float _amount;
    private IntentTransition<float> _transition;

    /// <summary>지금 보이는 세기(판의 투명도). 0 = 그대로, 1 = 검정.</summary>
    public float Amount => _amount;

    public float Intent => Transition.Intent;
    public bool IsTransitioning => Transition.IsTransitioning;

    private IntentTransition<float> Transition
        => _transition ??= new IntentTransition<float>(_amount, () => _amount, Apply, Mathf.Lerp, this);

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        Apply(_amount);
    }

    private void OnDestroy()
    {
        _transition?.Stop();
    }

    public void Bind(Image image)
    {
        if (image != null)
            targetImage = image;

        Apply(_amount);
    }

    public void Follow(float amount, float duration, Ease ease) => Transition.Follow(Mathf.Clamp01(amount), duration, ease);

    public void Snap(float amount) => Transition.Snap(Mathf.Clamp01(amount));

    // 기준 상태 — 전환을 멈추고 걷는다(정지 · 장면 경계).
    public void ClearImmediate() => Snap(0f);

    private void Apply(float amount)
    {
        _amount = amount;

        if (targetImage == null)
            return;

        targetImage.raycastTarget = false;
        targetImage.color = new Color(0f, 0f, 0f, amount);

        // 걷힌 판은 그리지 않는다 — 평소에는 비용이 없다.
        targetImage.enabled = amount > 0f;
    }
}
