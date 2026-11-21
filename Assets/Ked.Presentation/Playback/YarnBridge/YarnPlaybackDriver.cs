using System.Collections.Generic;
using System.Text;
using Ked.Presentation.Core;

public sealed class YarnPlaybackDriver
{
    private readonly CommandExecutor _executor;
    private readonly ICommandRunScopeProvider _scopeProvider;

    // 지금 열린 구간 — 다음 대사 줄의 머리(at 없는 명령). <<step>>을 만나면 앞 앵커 하나로 닫힌다(CloseStep).
    private readonly List<CommandSpecBase> _collectedSpecs = new();

    // <<step>>이 닫은 구간들 — 다음 대사 줄의 앞 앵커(옛 표기, docs/PLAN_line-anchor.md A6 · §9). 쓴 순서.
    // 줄이 도착한 뒤에 그 줄이 차례로 실행한다. 붙을 줄이 없으면 노드 끝의 암묵 스텝이 실행한다.
    private readonly List<List<CommandSpecBase>> _preBatches = new();

    // 위 둘을 쓴 순서로 — 아직 실행하지 않은 선언(slot · cast · actor)을 대상 판정이 본다(CommandRoleGate).
    // 줄이 도착하기 전에 그 줄의 명령이 모두 풀리므로, 앞 앵커가 세운 슬롯을 머리 명령이 가리킬 수 있어야 한다.
    private readonly List<CommandSpecBase> _pendingAll = new();

    // <<at 표지 명령>> — 다음 대사 줄의 것. 머리 배치(_collectedSpecs)에 섞지 않는다(docs/PLAN_line-beat.md I1).
    // 줄에 들어갈 때 그 줄의 앵커로 가져간다(InlineCommandTrack.Begin). 붙을 줄이 없으면 노드 끝의 스텝이 머리 뒤에(PlayStrayAt).
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

    public void Enqueue(CommandSpecBase spec)
    {
        _collectedSpecs.Add(spec);
        _pendingAll.Add(spec);
    }

    public IReadOnlyList<CommandSpecBase> Collected => _collectedSpecs;

    // 다음 대사 줄을 기다리는 명령 모두(앞 앵커 + 머리) — 쓴 순서. 대상 판정의 문맥이다.
    public IReadOnlyList<CommandSpecBase> PendingAll => _pendingAll;

    public int PreBatchCount => _preBatches.Count;

    public bool HasPending => _collectedSpecs.Count > 0 || _preBatches.Count > 0 || _collectedAt.Count > 0;

    // <<step>>(옛 표기) — 여기까지 쌓인 명령이 다음 대사 줄의 앞 앵커 하나다. 실행하지 않는다.
    // 빈 구간은 앵커가 아니다(쌓인 명령이 없는 무대 스텝은 스텝이 아니었다).
    public void CloseStep()
    {
        if (_collectedSpecs.Count == 0)
            return;

        _preBatches.Add(new List<CommandSpecBase>(_collectedSpecs));
        _collectedSpecs.Clear();
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

    // 다음 앞 앵커(<<step>>이 닫은 구간)를 실행한다 — 쓴 순서로 하나씩. 더 없으면 false.
    // 하나씩 꺼내므로, 도중에 끊긴 줄의 남은 것은 정지가 버린다(TryDiscardPending).
    public bool TryPlayNextPreBatch()
    {
        if (_preBatches.Count == 0)
            return false;

        List<CommandSpecBase> specs = _preBatches[0];
        _preBatches.RemoveAt(0);

        Play(specs);
        return true;
    }

    // 머리 — 지금 열린 구간을 실행한다.
    public void PlayCollected()
    {
        var specs = new List<CommandSpecBase>(_collectedSpecs);
        _collectedSpecs.Clear();

        // 이 줄의 것을 모두 가져갔다 — 앞 앵커는 머리보다 먼저 실행된다.
        if (_preBatches.Count == 0)
            _pendingAll.Clear();

        Play(specs);
    }

    // 이 단위에 덧붙여 실행한다 — <<at>>의 앵커. 쌓인 명령은 건드리지 않는다.
    // 이 단위의 첫 묶음이면(머리보다 먼저 열리는 앞 앵커) 실행기의 스텝을 연다.
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
        var specs = new List<CommandSpecBase>();

        foreach (List<CommandSpecBase> batch in _preBatches)
            specs.AddRange(batch);

        specs.AddRange(_collectedSpecs);

        count = specs.Count + _collectedAt.Count;

        _pendingAll.Clear();
        _collectedGaps.Clear();

        if (count == 0)
        {
            summary = string.Empty;
            return false;
        }

        summary = Describe(specs);

        if (_collectedAt.Count > 0)
            summary += (summary.Length > 0 ? ", " : "") + $"<<at>>×{_collectedAt.Count}";

        _preBatches.Clear();
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
