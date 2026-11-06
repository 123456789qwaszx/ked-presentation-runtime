using System;
using System.Collections.Generic;
using System.Text;

public sealed class YarnPlaybackDriver
{
    private readonly CommandExecutor _executor;
    private readonly ICommandRunScopeProvider _scopeProvider;

    private readonly List<CommandSpecBase> _collectedSpecs = new();

    private CommandRunScope CurrentScope => _scopeProvider?.CurrentScope;

    // 줄 머리 커맨드를 실행한 직후(첫 진입이 다 돈 뒤). 인자는 압축 중인가 — 장면 전역 의도가 리그를 다시 맞춘다.
    public event Action<bool> LineHeadPlayed;

    public YarnPlaybackDriver(CommandExecutor executor, ICommandRunScopeProvider scopeProvider)
    {
        _executor = executor;
        _scopeProvider = scopeProvider;
    }
    
    public void Enqueue(CommandSpecBase spec) => _collectedSpecs.Add(spec);

    // 다음 PlayCollected가 실행할 것 — 그 줄 머리의 커맨드.
    public IReadOnlyList<CommandSpecBase> Collected => _collectedSpecs;

    public void PlayCollected()
    {
        var specs = new List<CommandSpecBase>(_collectedSpecs);
        _collectedSpecs.Clear();

        // 커맨드가 0개여도 PlaySpecs가 스텝 경계(CleanupStep)를 연다 — 여기서 따로 부르지 않는다.
        CommandRunScope scope = CurrentScope;
        _executor.PlaySpecs(specs, scope);

        LineHeadPlayed?.Invoke(scope != null && scope.ShouldCompressCommandExecution);
    }

    // 쌓인 명령을 실행하지 않고 버린다. 버린 것이 있으면 개수와 요약("이름×개수, …")을 돌려준다(로그용).
    public bool TryDiscardPending(out int count, out string summary)
    {
        count = _collectedSpecs.Count;

        if (count == 0)
        {
            summary = string.Empty;
            return false;
        }

        summary = Describe(_collectedSpecs);
        _collectedSpecs.Clear();
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