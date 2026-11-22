using System.Collections.Generic;
using System.Text;
using Ked.Presentation.Core;

public sealed class YarnPlaybackDriver
{
    private readonly CommandExecutor _executor;
    private readonly ICommandRunScopeProvider _scopeProvider;

    // at 없는 명령 — 노드 끝에만 쓴다. 붙을 줄이 없이 남으면 노드 끝의 암묵 스텝이 쓴 순서로 실행한다(docs/PLAN_line-anchor.md A6).
    // 대사 줄이 오면 원문의 오류다 — 실행하지 않고 버린다(§9 N5, DropUnanchored). 아직 실행하지 않은 선언(slot · cast · actor)을
    // 대상 판정이 본다(CommandRoleGate). 대본에 쓴 꼴(_collectedCommands)은 오류를 적을 때 쓴다 — 명령 하나가 스펙 여럿을 낳는다.
    private readonly List<CommandSpecBase> _collectedSpecs = new();
    private readonly List<StageCommand> _collectedCommands = new();

    // <<at 표지 명령>> — 다음 대사 줄의 것. 줄에 들어갈 때 그 줄의 앵커로 가져간다(InlineCommandTrack.Begin).
    // 붙을 줄이 없으면 노드 끝의 스텝이 남은 명령 뒤에(PlayStrayAt).
    private readonly List<AtCommand> _collectedAt = new();

    // <<gap 이름 길이>> — 다음 대사 줄의 앵커에 붙는 간격(docs/PLAN_line-anchor.md A8). <<at>>과 같이 줄에 들어갈 때 가져간다.
    private readonly List<GapCommand> _collectedGaps = new();

    // 이 단위(줄 · 노드 끝의 스텝)가 실행기의 스텝을 열었나 — 처음 실행하는 묶음이 연다.
    // 여는 것이 앞 단위의 남은 전환을 마무리하는 경계다. 그 뒤의 묶음은 덧붙는다(docs/PLAN_line-anchor.md A13).
    private bool _unitOpened;

    // 이 단위에서 기다리는 명령(pause · screen_cover)을 실행했나 — 다음 앵커의 관문(AR3 ①)이 본다.
    // 기다림이 없는 줄은 명령이 그 자리에서 다 시작되므로 관문이 볼 것이 없다.
    private bool _unitHasWaits;

    private CommandRunScope CurrentScope => _scopeProvider?.CurrentScope;

    public YarnPlaybackDriver(CommandExecutor executor, ICommandRunScopeProvider scopeProvider)
    {
        _executor = executor;
        _scopeProvider = scopeProvider;
    }

    // at 없는 명령 하나 — 그 명령이 풀린 스펙들과 대본에 쓴 꼴.
    public void Enqueue(in StageCommand authored, IReadOnlyList<CommandSpecBase> specs)
    {
        _collectedCommands.Add(authored);

        for (int i = 0; i < specs.Count; i++)
            _collectedSpecs.Add(specs[i]);
    }

    // 쌓인 at 없는 명령의 스펙 — 쓴 순서. 대상 판정의 문맥이다.
    public IReadOnlyList<CommandSpecBase> Collected => _collectedSpecs;

    public bool HasPending => _collectedSpecs.Count > 0 || _collectedAt.Count > 0;

    // 대사 줄이 왔다 — 쌓인 at 없는 명령은 그 줄의 것이 아니다. 실행하지 않고 버리고, 대본에 쓴 꼴을 돌려준다(오류로 적는다).
    public bool TryDropUnanchored(out List<StageCommand> dropped)
    {
        if (_collectedCommands.Count == 0 && _collectedSpecs.Count == 0)
        {
            dropped = null;
            return false;
        }

        dropped = new List<StageCommand>(_collectedCommands);
        _collectedCommands.Clear();
        _collectedSpecs.Clear();
        return true;
    }

    public void EnqueueAt(in AtCommand at) => _collectedAt.Add(at);

    public IReadOnlyList<AtCommand> CollectedAt => _collectedAt;

    // 쌓인 <<at>>을 가져간다 — 쓴 순서.
    public List<AtCommand> TakeCollectedAt()
    {
        var ats = new List<AtCommand>(_collectedAt);
        _collectedAt.Clear();
        return ats;
    }

    public void EnqueueGap(in GapCommand gap) => _collectedGaps.Add(gap);

    // 쌓인 <<gap>>을 가져간다 — 쓴 순서.
    public List<GapCommand> TakeCollectedGaps()
    {
        var gaps = new List<GapCommand>(_collectedGaps);
        _collectedGaps.Clear();
        return gaps;
    }

    // 새 단위(줄 · 노드 끝의 스텝)에 들어선다 — 이 단위가 처음 실행하는 묶음이 실행기의 스텝을 연다.
    public void BeginUnit()
    {
        _unitOpened = false;
        _unitHasWaits = false;
    }

    public bool UnitHasWaits => _unitHasWaits;

    // 노드 끝의 암묵 스텝 — 남은 at 없는 명령을 쓴 순서로 실행한다.
    public void PlayCollected()
    {
        var specs = new List<CommandSpecBase>(_collectedSpecs);
        _collectedSpecs.Clear();
        _collectedCommands.Clear();

        Play(specs);
    }

    // 이 단위에 덧붙여 실행한다 — <<at>>의 앵커. 쌓인 명령은 건드리지 않는다.
    // 이 단위의 첫 묶음이면 실행기의 스텝을 연다.
    public void PlayInStep(IReadOnlyList<CommandSpecBase> specs)
    {
        Play(new List<CommandSpecBase>(specs));
    }

    private void Play(List<CommandSpecBase> specs)
    {
        for (int i = 0; i < specs.Count && !_unitHasWaits; i++)
            _unitHasWaits = specs[i] != null && specs[i].wait;

        CommandRunScope scope = CurrentScope;

        if (_unitOpened)
        {
            // 스텝 경계를 치지 않는다 — 앞 묶음의 아직 도는 전환을 마무리하지 않는다.
            _executor.PlaySpecsInStep(specs, scope);
            return;
        }

        _unitOpened = true;
        _executor.PlaySpecs(specs, scope);
    }

    // 쌓인 명령을 실행하지 않고 버린다. 버린 것이 있으면 개수와 요약("이름×개수, …")을 돌려준다(로그용).
    public bool TryDiscardPending(out int count, out string summary)
    {
        var specs = new List<CommandSpecBase>(_collectedSpecs);

        count = specs.Count + _collectedAt.Count;

        _collectedCommands.Clear();
        _collectedGaps.Clear();

        if (count == 0)
        {
            summary = string.Empty;
            return false;
        }

        summary = Describe(specs);

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
