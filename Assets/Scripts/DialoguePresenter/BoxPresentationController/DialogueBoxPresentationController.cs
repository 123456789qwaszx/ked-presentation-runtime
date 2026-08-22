using TMPro;
using Yarn.Unity;

public partial class DialogueBoxPresentationController
{
    private const DialogueBoxKind DefaultLineBoxKind = DialogueBoxKind.Surface;

    private const float FadeUpDuration = 0.25f;
    private const float FadeDownDuration = 0.1f;

    private readonly IPresentationDialogueBoxView _box;
    private readonly DialogueBoxTagResolver _tagResolver;
    private readonly DialogueBoxCurrentState _boxState;
    private readonly DialogueSurfaceState _surfaceState;
    private readonly DialogueSurfaceLayoutPresetDBSO _surfaceLayoutDb;
    private readonly DialogueSpeakerPresentationPolicyDBSO _speakerPolicyDb;

    public DialogueBoxPresentationController(
        DialogueBoxCurrentState dialogueBoxState,
        IPresentationDialogueBoxView box,
        DialogueBoxTagResolver tagResolver,
        DialogueSurfaceState surfaceState,
        DialogueSurfaceLayoutPresetDBSO surfaceLayoutDb,
        DialogueSpeakerPresentationPolicyDBSO speakerPolicyDb)
    {
        _boxState = dialogueBoxState;
        _box = box;
        _tagResolver = tagResolver;
        _surfaceState = surfaceState;
        _surfaceLayoutDb = surfaceLayoutDb;
        _speakerPolicyDb = speakerPolicyDb;
    }

    // 타이프라이터가 글자를 채워 넣을 대상.
    // ShowLineAsync가 레이아웃을 얹은 뒤에 읽어야 함.
    public TMP_Text LineTextTarget => _box.GetLineText();

    public async YarnTask ShowLineAsync(
        DialogueBoxPresentationContext ctx)
    {
        InvalidateVisibilityTransition();

        DialogueSpeakerPresentationPolicyDBSO.Entry speakerPolicy = default;
        bool hasSpeakerPolicy = false;

        if (ctx.HasCharacterName) {
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
        if (_tagResolver.TryResolveBoxKind(
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

        if (_tagResolver.TryResolveTransitionKind(ctx.Metadata, out DialogueBoxTransitionKind metadataTransition))
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

                if (immediate || run.IsValid)
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
                    await _box.FadeInAsync(FadeUpDuration, run);
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

                await _box.FadeOutAsync(FadeDownDuration, run);

                if (!run.IsValid)
                    break;

                _box.SetVisibleImmediate(false);

                ApplyContent(nextBoxKind, displayCharacterName, ctx);

                _box.PrepareHidden();
                await _box.FadeInAsync(FadeUpDuration, run);

                break;

            // 감추는 것이 목적이지만 내용은 얹는다 —
            // 타이프라이터가 이 박스의 TMP_Text에 계속 쓰기 때문이다.
            case DialogueBoxTransitionKind.Hide:
                ApplyContent(nextBoxKind, displayCharacterName, ctx);

                if (immediate)
                {
                    _box.SetVisibleImmediate(false);
                    break;
                }

                await _box.FadeOutAsync(FadeDownDuration, run);

                if (run.IsValid)
                    _box.SetVisibleImmediate(false);

                break;
        }
    }

    public void CloseAll()
    {
        InvalidateVisibilityTransition();

        _box.SetVisibleImmediate(false);
        _boxState.Reset();
    }

    // 뷰가 하나이므로 "중단된 전환이 건드린 박스"와 "현재 박스"가 같은 객체
    public void CleanupStale()
    {
        InvalidateVisibilityTransition();

        bool committedVisible = _boxState.IsVisible && _boxState.Box != null;

        _box.SetVisibleImmediate(committedVisible);
    }
}