using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

/// <summary>
/// Choice UI 패널
/// - 선택지 목록을 받아 버튼 리스트로 렌더
/// - 선택 시 index 반환
/// </summary>
public sealed class ChoicePanel : UIPanel<ChoicePanel.Refs>, IManagedUI
{
    public event Action<int> OnChoiceSelected;
    public event Action OnCloseRequested;

    #region Refs
    public enum Refs
    {
        ChoiceBG_Root,
        ChoiceBG_Image,

        Title_Root,
        Title_Text,

        ScrollView_Root,
        ScrollRect,   // ScrollRect 컴포넌트가 붙어있는 오브젝트
        Viewport,
        Content,

        ChoicePrefab,        // ChoiceBox 템플릿(비활성 권장)
    }

    private Image _bgImage;
    private TMP_Text _titleText;

    [SerializeField]private ScrollRect _scrollRect;
    private RectTransform _content;

    [SerializeField]private ChoiceBoxView _choicePrefab;
    private ButtonWidget _close;

    private readonly List<ChoiceBoxView> _spawned = new();
    private bool _valid;
    #endregion

    protected override void Initialize()
    {
        _bgImage   = View.Image(Refs.ChoiceBG_Image);
        _titleText = View.Text(Refs.Title_Text);

        // 전부 Refs 기반으로 통일
        //_scrollRect = View.Get<ScrollRect>(Refs.ScrollRect);
        _content    = View.Rect(Refs.Content);

        //_choicePrefab = View.Get<ChoiceBox>(Refs.ChoicePrefab);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif

        if (_titleText != null)
            _titleText.text = "Choice";

        if (_close != null)
            _close.OnClicked += HandleCloseClicked;

        // 템플릿은 꺼두고 Instantiate해서 사용
        if (_choicePrefab != null)
            _choicePrefab.gameObject.SetActive(false);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_close != null)
            _close.OnClicked -= HandleCloseClicked;

        ClearChoices();
    }

    // -----------------------
    // Present
    // -----------------------
    public void Present(IReadOnlyList<string> choices)
    {
        if (!_valid) return;

        ClearChoices();

        if (choices == null || choices.Count <= 0)
            return;

        if (_choicePrefab == null || _content == null)
            return;

        for (int i = 0; i < choices.Count; i++)
        {
            var view = UnityEngine.Object.Instantiate(_choicePrefab, _content);
            view.gameObject.SetActive(true);
            view.Present(index: i, label: choices[i]);

            view.OnClicked -= HandleChoiceClicked;
            view.OnClicked += HandleChoiceClicked;

            _spawned.Add(view);
        }

        ScrollToTop();
    }

    private void HandleChoiceClicked(int index)
    {
        OnChoiceSelected?.Invoke(index);
    }

    private void ClearChoices()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            var v = _spawned[i];
            if (v == null) continue;

            v.OnClicked -= HandleChoiceClicked;
            UnityEngine.Object.Destroy(v.gameObject);
        }
        _spawned.Clear();
    }

    private void ScrollToTop()
    {
        if (_scrollRect == null) return;

        Canvas.ForceUpdateCanvases();
        _scrollRect.verticalNormalizedPosition = 1f;
    }

    private void HandleCloseClicked()
    {
        OnCloseRequested?.Invoke();
    }

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _bgImage, Refs.ChoiceBG_Image);
        AppendMissing(ref missing, _titleText, Refs.Title_Text);

        AppendMissing(ref missing, _scrollRect, Refs.ScrollRect);
        AppendMissing(ref missing, _content, Refs.Content);

        AppendMissing(ref missing, _choicePrefab, Refs.ChoicePrefab);

        // Close 버튼을 "필수"로 볼지 "옵션"으로 볼지에 따라 선택
        // 필수면 아래 체크를 켜고,
        // 옵션이면 그대로 두면 됨.
        // AppendMissing(ref missing, _close, Refs.CloseButton_BWidget);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[ChoicePanel] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}
