using UnityEngine;
using Yarn.Unity;

public sealed class OptionsBoxPresentationController
{
    private const float FadeInDuration = 0.12f;

    private readonly IPresentationOptionsBoxView _box;

    public OptionsBoxPresentationController(IPresentationOptionsBoxView box)
    {
        _box = box;
    }

    public RectTransform ItemContainer => _box.ItemContainer;

    // 페이드인 도중 다음 콘텐츠 요청이 들어오면 false
    public async YarnTask<bool> ShowAsync(LineCancellationToken token)
    {
        _box.ResetPresentationTransform();
        _box.PrepareHidden();
        _box.SetInputEnabled(false);

        await _box
            .FadeInAsync(FadeInDuration, token.NextContentToken)
            .SuppressCancellationThrow();

        if (token.IsNextContentRequested)
            return false;

        // 입력은 항목이 붙은 뒤에 염.
        _box.SetInputEnabled(false);

        return true;
    }

    public void SetInputEnabled(bool enabled)
    {
        _box.SetInputEnabled(enabled);
    }

    public void CloseImmediate()
    {
        _box.SetInputEnabled(false);
        _box.SetVisibleImmediate(false);
    }
}