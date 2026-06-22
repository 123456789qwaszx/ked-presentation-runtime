using System.Threading;
using TMPro;
using Yarn.Markup;
using Yarn.Unity;

// ─────────────────────────────────────────────────────────────────────────────
// InlineAdvanceMarkupHandler
//
// 기존 InlineEventMarkupHandler(pause/signal/move/sfx/emoji)를 대체한다.
// inline point tag는 이제 [advance/] 하나만 인정한다.
//
// 책임:
//  - OnPrepareForLine: 라인의 [advance/] manifest를 굳힌다.
//  - OnCharacterWillAppear(normal): 위치 도달 시 ordinal 순서대로 host에 위임.
//  - OnTypewriterWillRevealAll(hurry/fast-forward): 남은 advance를 reveal-all 직전 flush.
//
// 하지 않는 것:
//  - sub runner / hub / lane 직접 조작.
//  - 핸들러 ct(hurry/next-line)를 디스패치 취소에 사용.
//  - host 누락을 조용히 무시하며 ordinal만 소비.
//
// 인스펙터:
//  - CustomLinePresenter.eventHandlers(List<ActionMarkupHandler>)에 등록.
//  - CustomLinePresenter.Initialize에서 Initialize(host)로 host를 주입한다.
// ─────────────────────────────────────────────────────────────────────────────
public sealed class InlineAdvanceMarkupHandler
    : ActionMarkupHandler, ITypewriterRevealAllFlushHandler
{
    private IInlinePresentationAdvanceHost _host;
    private InlineAdvanceManifest _manifest = InlineAdvanceManifest.Empty;

    public void Initialize(IInlinePresentationAdvanceHost host)
    {
        _host = host;
    }

    public override void OnPrepareForLine(MarkupParseResult line, TMP_Text text)
    {
        // line은 TextWithoutCharacterName 기준이다.
        // 따라서 manifest position은 OnCharacterWillAppear의 index 공간과 일치한다.
        _manifest = InlineAdvanceManifest.Build(line);
    }

    public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text)
    {
    }

    public override async YarnTask OnCharacterWillAppear(
        int currentCharacterIndex,
        MarkupParseResult line,
        CancellationToken ct)
    {
        // ct는 hurry/next-line 신호다.
        // inline advance의 실제 cancel은 host가 보유한 line hard-cancel run token으로 처리한다.
        while (_manifest.HasPendingAt(currentCharacterIndex))
        {
            if (!_manifest.TryConsumeNext(out int ordinal))
                break;

            await _host.AdvanceOneAsync(ordinal);
        }
    }

    public override void OnLineDisplayComplete()
    {
    }

    public override void OnLineWillDismiss()
    {
    }

    // 타입라이터가 hurry/fast-forward로 reveal-all 하기 직전에 호출한다.
    // 남은 advance를 ordinal 순서대로 flush해야 fast-forward에서도 presentation cursor가 보존된다.
    public async YarnTask OnTypewriterWillRevealAll(
        MarkupParseResult line,
        CancellationToken hardCancel)
    {
        while (!_manifest.IsExhausted)
        {
            if (hardCancel.IsCancellationRequested)
                return;

            if (!_manifest.TryConsumeNext(out int ordinal))
                break;

            await _host.AdvanceOneAsync(ordinal);
        }
    }
}