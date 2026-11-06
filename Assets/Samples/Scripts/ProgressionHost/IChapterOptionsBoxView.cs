using System.Threading;
using UnityEngine;
using Yarn.Unity;

/// <summary>
/// 진행 예제의 선택지 상자 — 에피소드 끝 선택지(ChapterOptionsView)가 항목을 세울 자리와 나타나는 방식.
/// 항목은 CustomOptionItem이고, 무엇을 고를 수 있는지는 ChapterOptionsView가 정한다.
/// 예제에서는 UI(KedOptionsBoxView)가 구현한다. 본체 Playback은 선택지를 모른다 — 대사 안 선택지(->)를 지원하지 않는다.
/// </summary>
public interface IChapterOptionsBoxView
{
    RectTransform ItemContainer { get; }

    void ResetPresentationTransform();

    void SetVisibleImmediate(bool visible);
    void SetInputEnabled(bool enabled);

    void PrepareHidden();

    YarnTask FadeInAsync(float duration, CancellationToken token);
}