using TMPro;
using UnityEngine;
using Yarn.Unity;

public partial class DialogueBoxPresentationController
{
    private const DialogueBoxKind DefaultLineBoxKind = DialogueBoxKind.Surface;

    private const float FadeUpDuration = 0.25f;
    private const float FadeDownDuration = 0.1f;

    private readonly IPresentationDialogueBoxView _box;
    // 메신저 패널. 없어도 된다 — 없으면 메신저 구간의 줄도 대사창으로 보인다.
    private readonly IPresentationMessengerView _messenger;
    private readonly MessengerLog _messengerLog;
    private readonly DialogueBoxCurrentState _boxState;
    private readonly DialogueSurfaceState _surfaceState;
    private readonly DialogueSurfaceLayoutPresetDBSO _surfaceLayoutDb;
    private readonly DialogueSpeakerPresentationPolicyDBSO _speakerPolicyDb;

    private bool _warnedMissingMessengerView;

    public DialogueBoxPresentationController(
        DialogueBoxCurrentState dialogueBoxState,
        IPresentationDialogueBoxView box,
        DialogueSurfaceState surfaceState,
        DialogueSurfaceLayoutPresetDBSO surfaceLayoutDb,
        DialogueSpeakerPresentationPolicyDBSO speakerPolicyDb,
        MessengerLog messengerLog,
        IPresentationMessengerView messenger = null)
    {
        _messengerLog = messengerLog;
        _messenger = messenger;
        _boxState = dialogueBoxState;
        _box = box;
        _surfaceState = surfaceState;
        _surfaceLayoutDb = surfaceLayoutDb;
        _speakerPolicyDb = speakerPolicyDb;
    }

    // 타이프라이터가 글자를 채워 넣을 대상.
    // ShowLineAsync가 레이아웃을 얹은 뒤에 읽어야 함.
    public TMP_Text LineTextTarget => _box.GetLineText();

    // 마지막 ShowLineAsync가 고른 표면이 타자기를 쓰는가.
    // 메신저 줄은 말풍선이 한 번에 선다 — 줄 흐름이 타자기 단계를 건너뛴다.
    public bool UsesTypewriter { get; private set; } = true;

    public async YarnTask ShowLineAsync(
        DialogueBoxPresentationContext ctx)
    {
        DialogueSpeakerPresentationPolicyDBSO.Entry speakerPolicy = default;
        bool hasSpeakerPolicy = false;

        if (ctx.HasCharacterName) 
        {
            hasSpeakerPolicy = _speakerPolicyDb.TryFind(ctx.CharacterName,
                out speakerPolicy);
        }

        DialogueBoxKind nextBoxKind = ResolveNextBoxKind(ctx, hasSpeakerPolicy, speakerPolicy);
        DialogueBoxTransitionKind transitionKind = ResolveTransitionKind(ctx, _boxState.BoxKind, nextBoxKind);

        // 들어오는 뷰와 나가는 뷰. 종류가 대사창 ↔ 메신저로 바뀔 때만 서로 다르다.
        IPresentationLineSurfaceView incoming = ViewFor(nextBoxKind);
        IPresentationLineSurfaceView outgoing = _boxState.IsVisible ? _boxState.Box : null;

        UsesTypewriter = !IsMessenger(incoming);

        string displayCharacterName = ctx.CharacterName;

        if (ctx.HasCharacterName && hasSpeakerPolicy && !string.IsNullOrWhiteSpace(speakerPolicy.fallbackDisplayName))
            displayCharacterName = speakerPolicy.fallbackDisplayName;

        await ApplyTransitionAsync(
            transitionKind,
            nextBoxKind,
            incoming,
            outgoing,
            displayCharacterName,
            ctx);

        // Only the still-valid run is allowed to commit the current box state.
        if (ctx.Run.IsValid)
            _boxState.Commit(nextBoxKind, incoming, transitionKind);
    }

    private IPresentationLineSurfaceView ViewFor(DialogueBoxKind kind)
    {
        if (kind == DialogueBoxKind.Messenger)
            return _messenger;

        return _box;
    }

    private bool IsMessenger(IPresentationLineSurfaceView view)
        => _messenger != null && ReferenceEquals(view, _messenger);

    private DialogueBoxKind ResolveNextBoxKind(
        DialogueBoxPresentationContext ctx,
        bool hasSpeakerPolicy,
        DialogueSpeakerPresentationPolicyDBSO.Entry speakerPolicy)
    {
        // 메신저 구간 안의 줄은 모두 메신저다 — 줄 태그 · 화자 정책보다 앞선다.
        if (_messengerLog.IsOpen)
        {
            if (_messenger != null)
                return DialogueBoxKind.Messenger;

            if (!_warnedMissingMessengerView)
            {
                _warnedMissingMessengerView = true;
                Debug.LogWarning("[DialogueBox] 메신저 구간이 열렸지만 메신저 뷰가 없다 — 대사창으로 보인다.");
            }
        }

        DialogueBoxKind kind = ResolveAuthoredBoxKind(ctx, hasSpeakerPolicy, speakerPolicy);

        // 메신저는 구간(messenger_open)이 고른다. 구간 밖에서 태그 · 정책으로 지정하면 기록이 없어 그릴 것이 없다.
        if (kind == DialogueBoxKind.Messenger)
            return DefaultLineBoxKind;

        return kind;
    }

    private static DialogueBoxKind ResolveAuthoredBoxKind(
        DialogueBoxPresentationContext ctx,
        bool hasSpeakerPolicy,
        DialogueSpeakerPresentationPolicyDBSO.Entry speakerPolicy)
    {
        if (DialogueBoxTagResolver.TryResolveBoxKind(
                ctx.Metadata,
                out DialogueBoxKind metadataBoxKind))
            return metadataBoxKind;

        if (hasSpeakerPolicy && speakerPolicy.useBoxKindOverride)
            return speakerPolicy.boxKind;

        return DefaultLineBoxKind;
    }

    private DialogueBoxTransitionKind ResolveTransitionKind(
        DialogueBoxPresentationContext ctx,
        DialogueBoxKind? currentBoxKind,
        DialogueBoxKind nextBoxKind)
    {
        if (ctx.UseImmediateTransition)
            return DialogueBoxTransitionKind.Cut;

        if (DialogueBoxTagResolver.TryResolveTransitionKind(ctx.Metadata, 
                out DialogueBoxTransitionKind metadataTransition))
            return metadataTransition;

        if (!_boxState.IsVisible || currentBoxKind.HasValue == false)
            return DialogueBoxTransitionKind.FadeIn;

        if (currentBoxKind.Value == nextBoxKind)
            return DialogueBoxTransitionKind.Keep;

        return DialogueBoxTransitionKind.FadeOutIn;
    }

    // 들어오는 뷰에 이 줄의 내용을 얹는다.
    // 대사창: ApplySurfaceLayout이 이름 표시 여부를 정하고, PrimeText가 그걸 읽는 순서.
    // 메신저: 기록 전체를 그린다 — 이 줄은 줄 진입에서 이미 기록의 마지막에 적혀 있다.
    private void ApplyContent(
        DialogueBoxKind kind,
        IPresentationLineSurfaceView incoming,
        string displayCharacterName,
        DialogueBoxPresentationContext ctx)
    {
        if (IsMessenger(incoming))
        {
            _messenger.Render(
                _messengerLog.Entries,
                animateLast: !ctx.UseImmediateTransition);

            return;
        }

        _box.ResetPresentationTransform();

        ApplySurfaceLayoutFor(_box, kind);

        _box.PrimeText(
            ctx.Text,
            displayCharacterName,
            ctx.HasCharacterName);
    }

    // 나가는 뷰가 들어오는 뷰와 다르면 그 자리에서 감춘다(대사창 ↔ 메신저의 즉시 전환).
    private static void HideIfOther(
        IPresentationLineSurfaceView outgoing,
        IPresentationLineSurfaceView incoming)
    {
        if (outgoing != null && !ReferenceEquals(outgoing, incoming))
            outgoing.SetVisibleImmediate(false);
    }

    private void ApplySurfaceLayoutFor(IPresentationDialogueBoxView box, DialogueBoxKind kind)
    {
        DialogueSurfaceLayoutPresetDBSO.Entry entry = _surfaceState.HasOverride
            ? _surfaceLayoutDb.FindOrDefault(_surfaceState.OverrideLayoutKey)
            : _surfaceLayoutDb.FindByKind(kind);

        box.ApplySurfaceLayout(entry);
    }

    private async YarnTask ApplyTransitionAsync(
        DialogueBoxTransitionKind transitionKind,
        DialogueBoxKind nextBoxKind,
        IPresentationLineSurfaceView incoming,
        IPresentationLineSurfaceView outgoing,
        string displayCharacterName,
        DialogueBoxPresentationContext ctx)
    {
        bool immediate = ctx.UseImmediateTransition;
        LinePresentationRun run = ctx.Run;

        // 페이드 아웃할 뷰. 보이는 뷰가 없으면(태그로 전환을 강제한 경우) 지금까지처럼 들어오는 뷰를 내린다.
        IPresentationLineSurfaceView leaving = outgoing ?? incoming;

        switch (transitionKind)
        {
            // 같은 종류 - 레이아웃이 그대로라 보이는 채로 교체.
            case DialogueBoxTransitionKind.Keep:
            case DialogueBoxTransitionKind.Cut:
                HideIfOther(outgoing, incoming);
                ApplyContent(nextBoxKind, incoming, displayCharacterName, ctx);
                incoming.SetVisibleImmediate(true);
                
                break;

            // 지금 안 보이는 상태다 - 교체가 화면에 드러나지 않음.
            case DialogueBoxTransitionKind.FadeIn:
                HideIfOther(outgoing, incoming);
                ApplyContent(nextBoxKind, incoming, displayCharacterName, ctx);

                if (immediate)
                    incoming.SetVisibleImmediate(true);
                else 
                {
                    incoming.PrepareHidden();
                    await incoming.FadeInAsync(FadeUpDuration, run.VisualToken);
                }
                
                break;

            // 보이는 중에 종류가 바뀐다 - 교체를 페이드 뒤로 미뤄야 함.
            // 여기서 먼저 교체하면 레이아웃과 화자 이름이 툭 바뀐 뒤에 페이드아웃됨.
            case DialogueBoxTransitionKind.FadeOutIn:
                if (immediate) 
                {
                    HideIfOther(outgoing, incoming);
                    ApplyContent(nextBoxKind, incoming, displayCharacterName, ctx);
                    incoming.SetVisibleImmediate(true);
                    break;
                }

                await leaving.FadeOutAsync(FadeDownDuration, run.VisualToken);

                if (!run.IsValid)
                    break;

                leaving.SetVisibleImmediate(false);

                ApplyContent(nextBoxKind, incoming, displayCharacterName, ctx);

                incoming.PrepareHidden();
                await incoming.FadeInAsync(FadeUpDuration, run.VisualToken);

                break;

            // 감추는 것이 목적이지만 내용은 얹는다 —
            // 타이프라이터가 이 박스의 TMP_Text에 계속 쓰기 때문이다.
            // 보이는 중이면 FadeOutIn과 같은 까닭으로 사라진 뒤에 얹는다 —
            // 먼저 얹으면 앞 줄 글자가 툭 지워지고 이름표·레이아웃이 바뀐 채 페이드아웃된다.
            // 타이프라이터는 ShowLineAsync가 끝난 뒤에 글자 칸을 받으므로 늦지 않다.
            case DialogueBoxTransitionKind.Hide:
                if (immediate || !_boxState.IsVisible)
                {
                    HideIfOther(outgoing, incoming);
                    ApplyContent(nextBoxKind, incoming, displayCharacterName, ctx);
                    incoming.SetVisibleImmediate(false);
                    break;
                }

                await leaving.FadeOutAsync(FadeDownDuration, run.VisualToken);

                if (!run.IsValid)
                    break;

                leaving.SetVisibleImmediate(false);
                ApplyContent(nextBoxKind, incoming, displayCharacterName, ctx);
                incoming.SetVisibleImmediate(false);

                break;
        }
    }

    // 두 뷰를 감춘다. 메신저 기록은 건드리지 않는다 — 시크 통과가 줄마다 부른다.
    public void CloseAll()
    {
        _box.SetVisibleImmediate(false);
        _messenger?.SetVisibleImmediate(false);
        _boxState.Reset();
    }

    // 중단된 전환이 건드린 뷰를 커밋된 상태로 되돌린다 — 커밋된 뷰만 보이고, 다른 뷰는 감춘다.
    public void CleanupStale()
    {
        IPresentationLineSurfaceView committed = _boxState.IsVisible ? _boxState.Box : null;

        _box.SetVisibleImmediate(ReferenceEquals(committed, _box));
        _messenger?.SetVisibleImmediate(ReferenceEquals(committed, _messenger));
    }
}