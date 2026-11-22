using Ked.Presentation.Core;

public sealed partial class StageCommandParser
{
    private const float DefaultPauseSeconds = 0.18f;

    // <<pause 시간>> — 다른 커맨드와 같은 시간 토큰(12fr · 0.5s · 0.5). 전경이다 — 그 앵커의 뒤 명령을 붙잡는다(docs/PLAN_line-anchor.md A7).
    private void EnqueueWaitSpec(string durationToken)
        => Emit(new WaitCommandSpec
        {
            duration = YarnDurationParser.Parse(durationToken, DefaultPauseSeconds),
            wait = true,
        });

    private void EnqueuePresentationActorAliasSpec(string aliasSymbol, string roleKey)
        => Emit(new SetPresentationActorAliasCommandSpec
        {
            aliasSymbol = aliasSymbol,
            roleKey = roleKey
        });
}
