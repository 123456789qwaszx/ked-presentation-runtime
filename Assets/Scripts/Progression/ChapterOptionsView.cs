using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ked.Progression;
using UnityEngine;

// 에피소드 선택지 화면. Yarn 옵션 프레젠터와 프리팹은 공유. 프레젠터는 분리.
// IPresentationOptionsBoxView(박스)와 VNOptionItem(항목)을 공유.
//
// Yarn 옵션은 잠기는 개념이 없기에 구분.
// 고르면 스탯이 커밋되고 에피소드가 넘어감. Yarn 옵션은 VM 안에서 처리되는 방식.
// LineCancellationToken 미사용.
public sealed class ChapterOptionsView : IChapterOptionsView
{
    private const float FadeInDuration = 0.12f;

    private readonly IPresentationOptionsBoxView _box;
    private readonly VNOptionItem _itemPrefab;

    private readonly List<VNOptionItem> _activeItems = new();

    private TaskCompletionSource<int> _pending;

    public ChapterOptionsView(IPresentationOptionsBoxView box, VNOptionItem itemPrefab)
    {
        _box = box;
        _itemPrefab = itemPrefab;
    }

    public async Task<int> ShowAsync(IReadOnlyList<ResolvedOption> options, int hiddenCount)
    {
        if (hiddenCount > 0)
            Debug.Log($"[진행] 선택지 {options.Count}개 · 숨김 {hiddenCount}개");

        _pending = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        BuildItems(options);

        _box.ResetPresentationTransform();
        _box.PrepareHidden();
        _box.SetInputEnabled(false);

        await _box.FadeInAsync(FadeInDuration, CancellationToken.None);

        _box.SetInputEnabled(true);
        SelectFirstSelectable();

        try
        {
            return await _pending.Task;
        }
        finally
        {
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

            // 잠긴 것은 회색으로 보이되 눌리지 않는다.
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

        // 잠긴 항목은 VNOptionItem 이 interactable=false 라 여기까지 오지 않는다.
        // 그래도 코어의 Choose 는 잠긴 것을 던진다
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