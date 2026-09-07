using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UIRefValidation;

public enum SaveLoadMenuMode
{
    Save = 0,
    Load = 1,
}

public sealed class SaveLoadMenuUIPanel : UIPanel<SaveLoadMenuUIPanel.Refs>
{
    public event Action<int> SlotClicked;
    public event Action<SaveLoadMenuMode> ModeChanged;
    public event Action CloseClicked;

    #region Refs

    public enum Refs
    {
        SaveLoadBG_Image,

        SaveModeButton_Button,
        LoadModeButton_Button,

        Content_Root,

        FirstPageButton_Button,
        PreviousPageButton_Button,
        PageLabel_Text,
        NextPageButton_Button,
        LastPageButton_Button,

        CloseButton_Button,
    }

    private Image _saveLoadBg;

    private Button _saveModeButton;
    private Button _loadModeButton;

    private RectTransform _content;

    private Button _firstPageButton;
    private Button _previousPageButton;
    private Button _nextPageButton;
    private Button _lastPageButton;

    private TMP_Text _pageLabel;

    private Button _closeButton;

    #endregion

    [Header("Slot")]
    [SerializeField]
    private VNSaveSlotButton slotButtonPrefab;

    [Header("Paging")]
    [SerializeField, Min(1)]
    private int slotsPerPage = 6;

    private readonly List<VNSaveSlotButton> _slotButtons = new();

    private bool _valid;

    private SaveLoadMenuMode _mode;
    private VNSaveSlotMeta[] _metas =
        Array.Empty<VNSaveSlotMeta>();

    // 0-based.
    private int _pageIndex;

    private int TotalPageCount =>
        _metas.Length <= 0
            ? 1
            : Mathf.Max(
                1,
                Mathf.CeilToInt(
                    _metas.Length / (float)slotsPerPage));

    protected override void OnInitialize()
    {
        CacheRefs();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid)
            return;
#else
        _valid = true;
#endif

        BindHandlers();

        CreateSlotButtonsIfNeeded();
        RefreshPage();
    }

    private void CacheRefs()
    {
        _saveLoadBg =
            View.Image(Refs.SaveLoadBG_Image);

        _saveModeButton =
            View.Button(Refs.SaveModeButton_Button);

        _loadModeButton =
            View.Button(Refs.LoadModeButton_Button);

        _content =
            View.Rect(Refs.Content_Root);

        _firstPageButton =
            View.Button(Refs.FirstPageButton_Button);

        _previousPageButton =
            View.Button(Refs.PreviousPageButton_Button);

        _pageLabel =
            View.Text(Refs.PageLabel_Text);

        _nextPageButton =
            View.Button(Refs.NextPageButton_Button);

        _lastPageButton =
            View.Button(Refs.LastPageButton_Button);

        _closeButton =
            View.Button(Refs.CloseButton_Button);
    }

    private void BindHandlers()
    {
        BindEvent(
            _saveModeButton,
            PressSaveModeButton);

        BindEvent(
            _loadModeButton,
            PressLoadModeButton);

        BindEvent(
            _firstPageButton,
            PressFirstPageButton);

        BindEvent(
            _previousPageButton,
            PressPreviousPageButton);

        BindEvent(
            _nextPageButton,
            PressNextPageButton);

        BindEvent(
            _lastPageButton,
            PressLastPageButton);

        BindEvent(
            _closeButton,
            PressCloseButton);
    }

    #region Present

    public void Rebuild(
        SaveLoadMenuMode mode,
        VNSaveSlotMeta[] metas)
    {
        if (!_valid)
            return;

        _mode = mode;
        _metas = metas ?? Array.Empty<VNSaveSlotMeta>();

        _pageIndex = Mathf.Clamp(
            _pageIndex,
            0,
            TotalPageCount - 1);

        CreateSlotButtonsIfNeeded();
        RefreshPage();
    }

    public void ResetPage()
    {
        if (!_valid)
            return;

        _pageIndex = 0;

        RefreshPage();
    }

    #endregion

    #region Handlers

    private void PressSaveModeButton(PointerEventData _)
    {
        if (_mode == SaveLoadMenuMode.Save)
            return;

        ModeChanged?.Invoke(
            SaveLoadMenuMode.Save);
    }

    private void PressLoadModeButton(PointerEventData _)
    {
        if (_mode == SaveLoadMenuMode.Load)
            return;

        ModeChanged?.Invoke(
            SaveLoadMenuMode.Load);
    }

    private void PressFirstPageButton(PointerEventData _)
    {
        if (_pageIndex <= 0)
            return;

        _pageIndex = 0;

        RefreshPage();
    }

    private void PressPreviousPageButton(PointerEventData _)
    {
        if (_pageIndex <= 0)
            return;

        _pageIndex--;

        RefreshPage();
    }

    private void PressNextPageButton(PointerEventData _)
    {
        if (_pageIndex >= TotalPageCount - 1)
            return;

        _pageIndex++;

        RefreshPage();
    }

    private void PressLastPageButton(PointerEventData _)
    {
        int lastPageIndex =
            TotalPageCount - 1;

        if (_pageIndex >= lastPageIndex)
            return;

        _pageIndex = lastPageIndex;

        RefreshPage();
    }

    private void PressCloseButton(PointerEventData _)
    {
        CloseClicked?.Invoke();
    }

    private void HandleSlotClicked(int slotIndex)
    {
        SlotClicked?.Invoke(slotIndex);
    }

    #endregion

    #region Refresh

    private void RefreshPage()
    {
        if (!_valid)
            return;

        int startMetaIndex =
            _pageIndex * slotsPerPage;

        bool isSaveMode =
            _mode == SaveLoadMenuMode.Save;

        for (int i = 0; i < _slotButtons.Count; i++)
        {
            VNSaveSlotButton slotButton =
                _slotButtons[i];

            if (slotButton == null)
                continue;

            int metaIndex =
                startMetaIndex + i;

            if (metaIndex >= _metas.Length)
            {
                slotButton.SetVisible(false);
                continue;
            }

            slotButton.SetVisible(true);

            int slotIndex =
                metaIndex + 1;

            slotButton.Present(
                slotIndex,
                _metas[metaIndex],
                isSaveMode);
        }

        RefreshModeButtons();
        RefreshPagingButtons();
        RefreshPageLabel();
    }

    private void RefreshModeButtons()
    {
        bool isSaveMode =
            _mode == SaveLoadMenuMode.Save;

        _saveModeButton.interactable =
            !isSaveMode;

        _loadModeButton.interactable =
            isSaveMode;
    }

    private void RefreshPagingButtons()
    {
        int lastPageIndex =
            TotalPageCount - 1;

        bool canGoPrevious =
            _pageIndex > 0;

        bool canGoNext =
            _pageIndex < lastPageIndex;

        _firstPageButton.interactable =
            canGoPrevious;

        _previousPageButton.interactable =
            canGoPrevious;

        _nextPageButton.interactable =
            canGoNext;

        _lastPageButton.interactable =
            canGoNext;
    }

    private void RefreshPageLabel()
    {
        _pageLabel.text =
            $"{_pageIndex + 1} / {TotalPageCount}";
    }

    #endregion

    #region Slots

    private void CreateSlotButtonsIfNeeded()
    {
        if (_content == null ||
            slotButtonPrefab == null)
        {
            return;
        }

        while (_slotButtons.Count < slotsPerPage)
        {
            VNSaveSlotButton slotButton =
                Instantiate(
                    slotButtonPrefab,
                    _content);

            // 비활성 prefab에서도 사용 전에 refs를 확보.
            slotButton.EnsureInitialized();

            slotButton.Clicked +=
                HandleSlotClicked;

            _slotButtons.Add(slotButton);
        }

        while (_slotButtons.Count > slotsPerPage)
        {
            int lastIndex =
                _slotButtons.Count - 1;

            VNSaveSlotButton slotButton =
                _slotButtons[lastIndex];

            _slotButtons.RemoveAt(lastIndex);

            if (slotButton == null)
                continue;

            slotButton.Clicked -=
                HandleSlotClicked;

            Destroy(slotButton.gameObject);
        }
    }

    #endregion

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(
            ref missing,
            _saveLoadBg,
            Refs.SaveLoadBG_Image);

        AppendMissing(
            ref missing,
            _saveModeButton,
            Refs.SaveModeButton_Button);

        AppendMissing(
            ref missing,
            _loadModeButton,
            Refs.LoadModeButton_Button);

        AppendMissing(
            ref missing,
            _content,
            Refs.Content_Root);

        AppendMissing(
            ref missing,
            _firstPageButton,
            Refs.FirstPageButton_Button);

        AppendMissing(
            ref missing,
            _previousPageButton,
            Refs.PreviousPageButton_Button);

        AppendMissing(
            ref missing,
            _pageLabel,
            Refs.PageLabel_Text);

        AppendMissing(
            ref missing,
            _nextPageButton,
            Refs.NextPageButton_Button);

        AppendMissing(
            ref missing,
            _lastPageButton,
            Refs.LastPageButton_Button);

        AppendMissing(
            ref missing,
            _closeButton,
            Refs.CloseButton_Button);

        if (slotButtonPrefab == null)
            missing +=
                "\n- slotButtonPrefab";

        if (missing.Length > 0)
        {
            Debug.LogWarning(
                $"[SaveLoadMenuUIPanel] Missing refs:\n{missing}",
                this);

            return false;
        }

        return true;
    }
}