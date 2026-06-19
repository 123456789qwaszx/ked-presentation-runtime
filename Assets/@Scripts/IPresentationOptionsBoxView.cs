using System.Threading;
using UnityEngine;
using Yarn.Unity;

public interface IPresentationOptionsBoxView
{
    RectTransform ItemContainer { get; }

    void ResetPresentationTransform();

    void SetVisibleImmediate(bool visible);
    void SetInputEnabled(bool enabled);

    void PrepareHidden();

    YarnTask FadeInAsync(float duration, CancellationToken token);
    YarnTask FadeOutAsync(float duration, CancellationToken token);
}