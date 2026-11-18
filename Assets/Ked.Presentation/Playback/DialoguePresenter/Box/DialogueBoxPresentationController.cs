using TMPro;
using Yarn.Unity;

public partial class DialogueBoxPresentationController
{
    private const DialogueBoxKind DefaultLineBoxKind = DialogueBoxKind.Surface;

    private const float FadeUpDuration = 0.25f;
    private const float FadeDownDuration = 0.1f;

    private readonly IPresentationDialogueBoxView _box;
    private readonly DialogueBoxCurrentState _boxState;
    private readonly DialogueSurfaceState _surfaceState;
    private readonly DialogueSurfaceLayoutPresetDBSO _surfaceLayoutDb;
    private readonly DialogueSpeakerPresentationPolicyDBSO _speakerPolicyDb;

    // 초상 줄(DialogueBoxKind.Portrait)의 그림을 내는 촬영소. 없으면(null) 초상 칸은 늘 꺼져 있다.
    private readonly PortraitStudio _portraitStudio;

    public DialogueBoxPresentationController(
        DialogueBoxCurrentState dialogueBoxState,
        IPresentationDialogueBoxView box,
        DialogueSurfaceState surfaceState,
        DialogueSurfaceLayoutPresetDBSO surfaceLayoutDb,
        DialogueSpeakerPresentationPolicyDBSO speakerPolicyDb,
        PortraitStudio portraitStudio)
    {
        _boxState = dialogueBoxState;
        _box = box;
        _surfaceState = surfaceState;
        _surfaceLayoutDb = surfaceLayoutDb;
        _speakerPolicyDb = speakerPolicyDb;
        _portraitStudio = portraitStudio;
    }

    // 타이프라이터가 글자를 채워 넣을 대상.
    // ShowLineAsync가 레이아웃을 얹은 뒤에 읽어야 함.
    public TMP_Text LineTextTarget => _box.GetLineText();

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

        string displayCharacterName = ctx.CharacterName;

        if (ctx.HasCharacterName && hasSpeakerPolicy && !string.IsNullOrWhiteSpace(speakerPolicy.fallbackDisplayName))
            displayCharacterName = speakerPolicy.fallbackDisplayName;

        await ApplyTransitionAsync(
            transitionKind,
            nextBoxKind,
            displayCharacterName,
            ctx);

        // Only the still-valid run is allowed to commit the current box state.
        if (ctx.Run.IsValid)
            _boxState.Commit(nextBoxKind, _box, transitionKind);
    }

    private DialogueBoxKind ResolveNextBoxKind(
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

    // ApplySurfaceLayout이 이름 표시 여부를 정하고,
    // PrimeText가 그걸 읽는 순서.
    private void ApplyContent(
        DialogueBoxKind kind,
        string displayCharacterName,
        DialogueBoxPresentationContext ctx)
    {
        _box.ResetPresentationTransform();

        ApplySurfaceLayoutFor(_box, kind);
        ApplyPortraitFor(kind);

        _box.PrimeText(
            ctx.Text,
            displayCharacterName,
            ctx.HasCharacterName);
    }

    private void ApplySurfaceLayoutFor(IPresentationDialogueBoxView box, DialogueBoxKind kind)
    {
        DialogueSurfaceLayoutPresetDBSO.Entry entry = _surfaceState.HasOverride
            ? _surfaceLayoutDb.FindOrDefault(_surfaceState.OverrideLayoutKey)
            : _surfaceLayoutDb.FindByKind(kind);

        box.ApplySurfaceLayout(entry);
    }

    // 초상 칸은 줄의 종류가 켜고 끈다 — 무대 커맨드가 아니다. 내용을 얹는 자리(ApplyContent)에서 함께 바뀌므로
    // 종류가 바뀌는 줄에서는 대사창이 사라진 틈에 켜지고 꺼진다.
    private void ApplyPortraitFor(DialogueBoxKind kind)
    {
        if (kind != DialogueBoxKind.Portrait || _portraitStudio == null)
        {
            _box.SetPortrait(null);
            return;
        }

        // 촬영소의 인물은 줄 머리의 명령으로 이미 서 있다. 아직 한 프레임도 찍히지 않았어도 칸은 켠다 —
        // 그 프레임 끝에 찍힌다. 인물이 없다는 판정(HasSubject)은 촬영소가 다음 LateUpdate에야 알므로 여기서 묻지 않는다.
        _box.SetPortrait(_portraitStudio.Texture);
    }

    private async YarnTask ApplyTransitionAsync(
        DialogueBoxTransitionKind transitionKind,
        DialogueBoxKind nextBoxKind,
        string displayCharacterName,
        DialogueBoxPresentationContext ctx)
    {
        bool immediate = ctx.UseImmediateTransition;
        LinePresentationRun run = ctx.Run;

        switch (transitionKind)
        {
            // 같은 종류 - 레이아웃이 그대로라 보이는 채로 교체.
            case DialogueBoxTransitionKind.Keep:
            case DialogueBoxTransitionKind.Cut:
                ApplyContent(nextBoxKind, displayCharacterName, ctx);
                _box.SetVisibleImmediate(true);
                
                break;

            // 지금 안 보이는 상태다 - 교체가 화면에 드러나지 않음.
            case DialogueBoxTransitionKind.FadeIn:
                ApplyContent(nextBoxKind, displayCharacterName, ctx);

                if (immediate)
                    _box.SetVisibleImmediate(true);
                else 
                {
                    _box.PrepareHidden();
                    await _box.FadeInAsync(FadeUpDuration, run.VisualToken);
                }
                
                break;

            // 보이는 중에 종류가 바뀐다 - 교체를 페이드 뒤로 미뤄야 함.
            // 여기서 먼저 교체하면 레이아웃과 화자 이름이 툭 바뀐 뒤에 페이드아웃됨.
            case DialogueBoxTransitionKind.FadeOutIn:
                if (immediate) 
                {
                    ApplyContent(nextBoxKind, displayCharacterName, ctx);
                    _box.SetVisibleImmediate(true);
                    break;
                }

                await _box.FadeOutAsync(FadeDownDuration, run.VisualToken);

                if (!run.IsValid)
                    break;

                _box.SetVisibleImmediate(false);

                ApplyContent(nextBoxKind, displayCharacterName, ctx);

                _box.PrepareHidden();
                await _box.FadeInAsync(FadeUpDuration, run.VisualToken);

                break;

            // 감추는 것이 목적이지만 내용은 얹는다 —
            // 타이프라이터가 이 박스의 TMP_Text에 계속 쓰기 때문이다.
            // 보이는 중이면 FadeOutIn과 같은 까닭으로 사라진 뒤에 얹는다 —
            // 먼저 얹으면 앞 줄 글자가 툭 지워지고 이름표·레이아웃이 바뀐 채 페이드아웃된다.
            // 타이프라이터는 ShowLineAsync가 끝난 뒤에 글자 칸을 받으므로 늦지 않다.
            case DialogueBoxTransitionKind.Hide:
                if (immediate || !_boxState.IsVisible)
                {
                    ApplyContent(nextBoxKind, displayCharacterName, ctx);
                    _box.SetVisibleImmediate(false);
                    break;
                }

                await _box.FadeOutAsync(FadeDownDuration, run.VisualToken);

                if (!run.IsValid)
                    break;

                _box.SetVisibleImmediate(false);
                ApplyContent(nextBoxKind, displayCharacterName, ctx);

                break;
        }
    }

    // 무대 스텝 — 대사창을 닫는다. 내용은 건드리지 않는다(얹을 줄이 없다).
    // 중단되면(run 무효) 상태를 적지 않는다 — 부른 쪽이 CleanupStale로 정리한다.
    public async YarnTask HideForStageStepAsync(LinePresentationRun run, bool immediate)
    {
        if (!_boxState.IsVisible)
            return;

        if (!immediate)
        {
            await _box.FadeOutAsync(FadeDownDuration, run.VisualToken);

            if (!run.IsValid)
                return;
        }

        _box.SetVisibleImmediate(false);
        _boxState.MarkHidden();
    }

    public void CloseAll()
    {
        _box.SetVisibleImmediate(false);
        _box.SetPortrait(null);
        _boxState.Reset();
    }

    // 뷰가 하나이므로 "중단된 전환이 건드린 박스"와 "현재 박스"가 같은 객체
    public void CleanupStale()
    {
        bool committedVisible = _boxState.IsVisible && _boxState.Box != null;

        _box.SetVisibleImmediate(committedVisible);
    }
}