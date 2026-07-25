using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 목록 한 줄의 표시 내용.
/// VNOptionViewModel 을 만들 때 필요한 최소값만 담는다.
/// </summary>
public readonly struct GuesthouseOptionEntry
{
    public readonly string Label;
    public readonly bool IsAvailable;

    public GuesthouseOptionEntry(string label, bool isAvailable = true)
    {
        Label = label ?? string.Empty;
        IsAvailable = isAvailable;
    }
}

/// <summary>
/// VNOptionItem 인스턴스 묶음을 관리한다.
///
/// 게스트하우스 화면은 전부 '라벨 목록에서 하나 고르기' 형태라 생성/정리/포커스 로직이 같다.
/// 다섯 곳에 같은 코드를 두지 않도록 여기로 모으고, 각 화면은 라벨 조립만 담당한다.
///
/// VNOptionItem 은 Selectable 이라 마우스 hover 와 패드 입력이 그대로 동작한다.
/// 대신 Submitted 가 항목 자신을 돌려주므로, 인덱스 환원은 이쪽 책임이다.
///
/// MonoBehaviour 가 아니다. 패널이 필드로 들고 수명을 직접 관리한다.
/// </summary>
public sealed class GuesthouseOptionItemList
{
    /// <summary>선택된 항목의 인덱스. Rebuild 에 넘긴 목록 기준이다.</summary>
    public event Action<int> OnSubmitted;

    private readonly List<VNOptionItem> _items = new();

    private VNOptionItem _prefab;
    private RectTransform _container;
    private bool _locked;

    public int Count => _items.Count;

    /// <summary>
    /// 한 번 고르면 목록을 잠글지 여부.
    /// 결정을 확정하는 목록은 true, 업무수첩처럼 열람을 반복하는 목록은 false 로 둔다.
    /// </summary>
    public bool LockOnSubmit { get; set; } = true;

    /// <summary>패널 OnInitialize 에서 한 번 호출한다. 템플릿은 여기서 꺼 둔다.</summary>
    public void Configure(VNOptionItem prefab, RectTransform container)
    {
        _prefab = prefab;
        _container = container;

        if (_prefab != null)
            _prefab.gameObject.SetActive(false);
    }

    public bool IsReady => _prefab != null && _container != null;

    /// <summary>목록을 전부 다시 만든다. 이전 인스턴스는 파기된다.</summary>
    public void Rebuild(IReadOnlyList<GuesthouseOptionEntry> entries)
    {
        Clear();

        if (!IsReady || entries == null)
            return;

        _locked = false;

        for (int i = 0; i < entries.Count; i++)
        {
            VNOptionItem item = UnityEngine.Object.Instantiate(_prefab, _container);

            // Awake 가 ResetView 를 돌리므로, 활성화한 뒤에 바인딩해야 값이 남는다.
            item.gameObject.SetActive(true);

            item.Submitted -= HandleSubmitted;
            item.Submitted += HandleSubmitted;

            item.Bind(BuildViewModel(entries[i], i));
            item.SetRevealAlpha(1f);

            _items.Add(item);
        }

        SelectFirstAvailable();
    }

    /// <summary>
    /// 통제 상실처럼 목록은 남기되 입력만 막아야 할 때 쓴다.
    /// 표시를 지우면 무엇을 잃었는지 보이지 않으므로 항목 자체는 남긴다.
    /// </summary>
    public void SetLocked(bool locked)
    {
        _locked = locked;

        for (int i = 0; i < _items.Count; i++)
        {
            VNOptionItem item = _items[i];

            if (item == null)
                continue;

            item.interactable = !locked && item.HasViewModel && item.ViewModel.IsAvailable;
        }
    }

    public void Clear()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            VNOptionItem item = _items[i];

            if (item == null)
                continue;

            item.Submitted -= HandleSubmitted;
            item.ResetView();

            UnityEngine.Object.Destroy(item.gameObject);
        }

        _items.Clear();
    }

    /// <summary>
    /// SourceOption 은 Yarn 선택지에만 쓰이고 VNOptionItem 은 참조하지 않는다.
    /// 게스트하우스 목록은 Yarn 선택지가 아니므로 null 로 둔다.
    /// </summary>
    private static VNOptionViewModel BuildViewModel(in GuesthouseOptionEntry entry, int index)
    {
        return new VNOptionViewModel(
            sourceOption: null,
            sourceOptionIndex: index,
            choiceIndexInNode: index,
            label: entry.Label,
            isAvailable: entry.IsAvailable,
            effects: null);
    }

    private void HandleSubmitted(VNOptionItem item)
    {
        if (_locked || item == null)
            return;

        int index = _items.IndexOf(item);

        if (index < 0)
            return;

        // 중복 제출 방지. VNOptionItem 자체도 막지만, 목록 단위로도 한 번 더 잠근다.
        if (LockOnSubmit)
            _locked = true;

        OnSubmitted?.Invoke(index);
    }

    /// <summary>패드/키보드로도 바로 진행할 수 있도록 첫 후보에 포커스를 준다.</summary>
    private void SelectFirstAvailable()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            VNOptionItem item = _items[i];

            if (item == null || !item.isActiveAndEnabled || !item.IsInteractable())
                continue;

            item.Select();
            return;
        }
    }
}
