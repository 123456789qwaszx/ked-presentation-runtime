using Ked.Presentation.Core;

public sealed partial class StageCommandParser
{
    private const float DefaultPauseSeconds = 0.18f;

    // <<pause 시간>> — 다른 커맨드와 같은 시간 토큰(12fr · 0.5s · 0.5). 전경이다 — 무대 스텝에서만 기다린다.
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
