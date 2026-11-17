using System.Collections.Generic;
using System.Text;
using Ked.Presentation.Core;

public sealed class YarnPlaybackDriver
{
    private readonly CommandExecutor _executor;
    private readonly ICommandRunScopeProvider _scopeProvider;

    private readonly List<CommandSpecBase> _collectedSpecs = new();

    // <<at 표지 명령>> — 다음 대사 줄의 것. 머리 배치(_collectedSpecs)에 섞지 않는다(docs/PLAN_line-beat.md I1).
    // 줄의 머리가 그 줄의 목록으로 가져간다(InlineCommandTrack.Begin). 무대 스텝이면 머리 뒤에(PlayStrayAt).
    private readonly List<AtCommand> _collectedAt = new();

    private CommandRunScope CurrentScope => _scopeProvider?.CurrentScope;

    public YarnPlaybackDriver(CommandExecutor executor, ICommandRunScopeProvider scopeProvider)
    {
        _executor = executor;
        _scopeProvider = scopeProvider;
    }
    
    public void Enqueue(CommandSpecBase spec) => _collectedSpecs.Add(spec);

    public IReadOnlyList<CommandSpecBase> Collected => _collectedSpecs;

    public void EnqueueAt(in AtCommand at) => _collectedAt.Add(at);

    public IReadOnlyList<AtCommand> CollectedAt => _collectedAt;

    // 쌓인 <<at>>을 가져간다 — 쓴 순서.
    public List<AtCommand> TakeCollectedAt()
    {
        var ats = new List<AtCommand>(_collectedAt);
        _collectedAt.Clear();
        return ats;
    }

    public void PlayCollected()
    {
        var specs = new List<CommandSpecBase>(_collectedSpecs);
        _collectedSpecs.Clear();

        CommandRunScope scope = CurrentScope;
        _executor.PlaySpecs(specs, scope);
    }

    // 지금 스텝에 덧붙여 실행한다 — 스텝 경계를 치지 않는다(인라인 명령). 쌓인 명령은 건드리지 않는다.
    public void PlayInStep(IReadOnlyList<CommandSpecBase> specs)
    {
        _executor.PlaySpecsInStep(new List<CommandSpecBase>(specs), CurrentScope);
    }

    // 쌓인 명령을 실행하지 않고 버린다. 버린 것이 있으면 개수와 요약("이름×개수, …")을 돌려준다(로그용).
    public bool TryDiscardPending(out int count, out string summary)
    {
        count = _collectedSpecs.Count + _collectedAt.Count;

        if (count == 0)
        {
            summary = string.Empty;
            return false;
        }

        summary = Describe(_collectedSpecs);

        if (_collectedAt.Count > 0)
            summary += (summary.Length > 0 ? ", " : "") + $"<<at>>×{_collectedAt.Count}";

        _collectedSpecs.Clear();
        _collectedAt.Clear();
        return true;
    }

    // 이어진 같은 종류는 하나로 센다: FadeCommandSpecCharR×3, StopBgmSpec, …
    private static string Describe(List<CommandSpecBase> specs)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < specs.Count; )
        {
            string name = specs[i] != null ? specs[i].GetType().Name : "null";
            int run = 1;

            while (i + run < specs.Count &&
                   (specs[i + run] != null ? specs[i + run].GetType().Name : "null") == name)
                run++;

            if (sb.Length > 0)
                sb.Append(", ");

            sb.Append(name);

            if (run > 1)
                sb.Append('×').Append(run);

            i += run;
        }

        return sb.ToString();
    }
}