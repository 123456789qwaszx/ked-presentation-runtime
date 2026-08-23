using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Ked.Progression;
using UnityEngine;

// 테스트용 임시 선택지.
public sealed class DebugKeyChapterOptionsView : IChapterOptionsView
{
    private TaskCompletionSource<int> _pending;
    private IReadOnlyList<ResolvedOption> _shown;

    public bool IsWaiting => _pending != null;

    public Task<int> ShowAsync(IReadOnlyList<ResolvedOption> options, int hiddenCount)
    {
        _shown = options;

        // 완료를 비동기로.
        // 그렇지 않으면 TrySelect를 부른 Update 스택 위에서
        // 드라이버의 다음 요청(대사 재생)이 그대로 이어져 실행됨.
        _pending = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        Debug.Log(Describe(options, hiddenCount));

        return _pending.Task;
    }

    public void TrySelect(int index)
    {
        if (_pending == null || _shown == null)
            return;

        if (index < 0 || index >= _shown.Count)
            return;

        if (!_shown[index].IsSelectable)
        {
            Debug.LogWarning(
                $"[진행] {index + 1}번은 잠겨 있다 — {_shown[index].BlockingCondition}");

            return;
        }

        TaskCompletionSource<int> pending = _pending;

        _pending = null;
        _shown = null;

        pending.TrySetResult(index);
    }

    /// <summary>드라이버가 멈출 때. 기다리던 쪽을 풀어 준다.</summary>
    public void Cancel()
    {
        TaskCompletionSource<int> pending = _pending;

        _pending = null;
        _shown = null;

        pending?.TrySetCanceled();
    }

    private static string Describe(IReadOnlyList<ResolvedOption> options, int hiddenCount)
    {
        var text = new StringBuilder();

        text.AppendLine($"[진행] 선택지 {options.Count}개 — 숫자 키로 고른다");

        for (int i = 0; i < options.Count; i++)
        {
            ResolvedOption resolved = options[i];
            EpisodeOption option = resolved.Option;

            text.Append("  ").Append(i + 1).Append(") ").Append(option.ChoiceLabel);

            if (!resolved.IsSelectable)
            {
                // BlockingCondition은 조건식이라 디버그에만. 플레이어에게는
                // LockedReason(작가가 쓴 문구)만 보임.
                text.Append("  [잠김");

                if (!string.IsNullOrEmpty(resolved.LockedReason))
                    text.Append(": ").Append(resolved.LockedReason);

                text.Append(" / 조건 ").Append(resolved.BlockingCondition).Append(']');
            }

            if (option.StatChanges.Count > 0)
            {
                text.Append("  →");

                for (int c = 0; c < option.StatChanges.Count; c++)
                    text.Append(' ').Append(option.StatChanges[c]);
            }

            if (option.HasVia)
                text.Append("  (연출 ").Append(option.ViaNodeId).Append(')');

            text.AppendLine();
        }

        if (hiddenCount > 0)
            text.Append("  (숨김 ").Append(hiddenCount).Append("개 — 로그에만 적는다)");

        return text.ToString();
    }
}