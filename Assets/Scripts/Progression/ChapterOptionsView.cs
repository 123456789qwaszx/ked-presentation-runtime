using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ked.Progression;
using UnityEngine;

// Progression 계층의 에피소드 선택지 화면.
// Yarn option과 prefab은 공유할 수 있지만 presentation contract는 별도다.
public sealed class ChapterOptionsView : IChapterOptionsView
{
    private const float FadeInDuration = 0.12f;

    private readonly IPresentationOptionsBoxView _box;
    private readonly VNOptionItem _itemPrefab;

    private readonly List<VNOptionItem> _activeItems = new();

    // 현재 ShowAsync transaction에 외부 Cancel/Select가 접근하기 위한 handle.
    private TaskCompletionSource<int> _pending;

    public ChapterOptionsView(
        IPresentationOptionsBoxView box,
        VNOptionItem itemPrefab)
    {
        _box = box;
        _itemPrefab = itemPrefab;
    }

    public async Task<int> ShowAsync(
        IReadOnlyList<ResolvedOption> options,
        int hiddenCount)
    {
        if (_pending != null)
            throw new InvalidOperationException("진행 선택지를 이미 기다리고 있다.");

        if (hiddenCount > 0)
            Debug.Log($"[진행] 선택지 {options.Count}개 · 숨김 {hiddenCount}개");

        var pending = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        _pending = pending;

        try
        {
            BuildItems(options);

            _box.ResetPresentationTransform();
            _box.PrepareHidden();
            _box.SetInputEnabled(false);

            await _box.FadeInAsync(FadeInDuration, CancellationToken.None);

            // Fade 도중 Cancel되었다면 UI를 다시 활성화하지 않는다.
            if (pending.Task.IsCompleted)
                return await pending.Task;

            _box.SetInputEnabled(true);
            SelectFirstSelectable();

            return await pending.Task;
        }
        finally
        {
            if (ReferenceEquals(_pending, pending))
                _pending = null;

            Close();
        }
    }

    public void Cancel()
    {
        TaskCompletionSource<int> pending = _pending;

        if (pending == null)
            return;

        _pending = null;
        pending.TrySetCanceled();
    }

    private void BuildItems(IReadOnlyList<ResolvedOption> options)
    {
        DestroyItems();

        for (int i = 0; i < options.Count; i++)
        {
            ResolvedOption resolved = options[i];
            VNOptionItem item = UnityEngine.Object.Instantiate(_itemPrefab, _box.ItemContainer);

            if (!item.gameObject.activeSelf)
                item.gameObject.SetActive(true);

            int index = i;
            item.Submitted += _ => Select(index);

            item.Bind(
                LabelOf(resolved),
                resolved.IsSelectable,
                EffectTextOf(resolved.Option));

            item.SetRevealAlpha(1f);

            _activeItems.Add(item);
        }
    }

    private static string LabelOf(ResolvedOption resolved)
    {
        string label = resolved.Option.ChoiceLabel;

        if (resolved.IsSelectable || string.IsNullOrEmpty(resolved.LockedReason))
            return label;

        return $"{label}  ({resolved.LockedReason})";
    }

    private static string EffectTextOf(EpisodeOption option)
    {
        if (option.StatChanges.Count == 0)
            return string.Empty;

        var text = new StringBuilder();

        for (int i = 0; i < option.StatChanges.Count; i++)
        {
            if (i > 0)
                text.Append("  ");

            text.Append(option.StatChanges[i]);
        }

        return text.ToString();
    }

    private void Select(int index)
    {
        TaskCompletionSource<int> pending = _pending;

        if (pending == null)
            return;

        _pending = null;
        pending.TrySetResult(index);
    }

    private void SelectFirstSelectable()
    {
        for (int i = 0; i < _activeItems.Count; i++)
        {
            VNOptionItem item = _activeItems[i];

            if (item != null && item.isActiveAndEnabled && item.IsInteractable())
            {
                item.Select();
                return;
            }
        }
    }

    private void Close()
    {
        _box.SetInputEnabled(false);
        _box.SetVisibleImmediate(false);

        DestroyItems();
    }

    private void DestroyItems()
    {
        for (int i = 0; i < _activeItems.Count; i++)
        {
            VNOptionItem item = _activeItems[i];

            if (!item)
                continue;

            item.ResetView();
            UnityEngine.Object.Destroy(item.gameObject);
        }

        _activeItems.Clear();
    }
}