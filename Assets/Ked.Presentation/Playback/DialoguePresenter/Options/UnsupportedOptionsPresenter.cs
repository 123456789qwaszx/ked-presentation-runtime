using UnityEngine;
using Yarn.Unity;

/// <summary>
/// 대사 안 선택지(Yarn <c>-&gt;</c>)는 지원하지 않는다 — 선택지는 에피소드 끝의 진행 선택지(챕터 JSON의 <c>NextOptions</c>)
/// 하나뿐이다(2026-11-04 소유자 결정 · 두 툴은 <c>-&gt;</c>를 만들지 않는다).
///
/// 그래도 손으로 쓴 Yarn에 <c>-&gt;</c>가 들어올 수 있다. Yarn Spinner는 옵션을 고른 프리젠터가 없으면
/// <c>allowOptionFallthrough</c>로 <b>말없이</b> 다음 줄로 간다 — 그 침묵을 막는 것이 이 프리젠터의 전부다:
/// 오류 로그를 남기고 <see cref="DialogueRunner.NoOptionSelected"/>를 돌려준다(게임은 멈추지 않고 지나간다).
///
/// ⛔ 전에는 <c>CustomOptionsPresenter</c>가 옵션 상자를 띄우고 선택을 기록해 되감기 시크 때 자동 응답했다
/// (<c>ChoiceHistory</c> · <c>ChoiceBoundary</c> · <c>OptionsPresentationFlow</c>, 2026-11-05에 걷힘).
/// 대사 안에 플레이어 입력이 없으니 단위를 루트부터 다시 달리는 것은 구조로 결정적이다 — 기록할 선택이 없다.
/// </summary>
public sealed class UnsupportedOptionsPresenter : DialoguePresenterBase
{
    public override YarnTask OnDialogueStartedAsync() => YarnTask.CompletedTask;

    public override YarnTask OnDialogueCompleteAsync() => YarnTask.CompletedTask;

    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token) => YarnTask.CompletedTask;

    public override YarnTask<DialogueOption> RunOptionsAsync(
        DialogueOption[] dialogueOptions,
        LineCancellationToken cancellationToken)
    {
        Debug.LogError(
            $"[연출] 대사 안 선택지(->)는 지원하지 않는다 — 선택지는 에피소드 끝의 진행 선택지에서만 선다. " +
            $"이 묶음({dialogueOptions?.Length ?? 0}개)은 고르지 않고 지나간다. 대본에서 -> 블록을 걷어 내고 챕터 간선으로 옮겨라.");

        return DialogueRunner.NoOptionSelected;
    }
}
