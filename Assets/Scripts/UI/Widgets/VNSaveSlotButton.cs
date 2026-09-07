using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UIRefValidation;

// 수동 저장 슬롯 하나.
// 저장 데이터의 의미는 모르고, 전달받은 표시 정보만 그린다.
public sealed class VNSaveSlotButton : UIBase<VNSaveSlotButton.Refs>
{
    public event Action<int> Clicked;

    #region Refs

    public enum Refs
    {
        SlotButton_Button,

        SlotLabel_Text,
        ChapterLabel_Text,
        PreviewLabel_Text,
        SavedAtLabel_Text,
        PlaytimeLabel_Text,
    }

    private Button _slotButton;

    private TMP_Text _slotLabel;
    private TMP_Text _chapterLabel;
    private TMP_Text _previewLabel;
    private TMP_Text _savedAtLabel;
    private TMP_Text _playtimeLabel;

    #endregion

    private bool _valid;
    private int _slotIndex;

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
    }

    private void CacheRefs()
    {
        _slotButton = View.Button(Refs.SlotButton_Button);

        _slotLabel     = View.Text(Refs.SlotLabel_Text);
        _chapterLabel  = View.Text(Refs.ChapterLabel_Text);
        _previewLabel  = View.Text(Refs.PreviewLabel_Text);
        _savedAtLabel  = View.Text(Refs.SavedAtLabel_Text);
        _playtimeLabel = View.Text(Refs.PlaytimeLabel_Text);
    }

    private void BindHandlers()
    {
        BindEvent(_slotButton, PressSlotButton);
    }

    #region Present

    public void Present(
        int slotIndex,
        VNSaveSlotMeta meta,
        bool isSaveMode)
    {
        if (!_valid)
            return;

        _slotIndex = slotIndex;

        bool isEmpty = meta == null || meta.IsEmpty;

        PresentSlotLabel(slotIndex, meta, isEmpty);

        _chapterLabel.text =
            isEmpty ? "Empty" : meta.ChapterId;

        _previewLabel.text =
            isEmpty ? "" : meta.Preview;

        _savedAtLabel.text =
            isEmpty ? "" : FormatSavedAt(meta.SavedAtUtc);

        _playtimeLabel.text =
            isEmpty ? "" : FormatPlaytime(meta.PlaySeconds);

        // Save:
        // - 기존 슬롯 덮어쓰기 가능
        // - 빈 슬롯 신규 저장 가능
        //
        // Load:
        // - 실제 저장이 있는 슬롯만 선택 가능
        _slotButton.interactable =
            isSaveMode || !isEmpty;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    private void PresentSlotLabel(
        int slotIndex,
        VNSaveSlotMeta meta,
        bool isEmpty)
    {
        if (isEmpty ||
            string.IsNullOrWhiteSpace(meta.Label))
        {
            _slotLabel.text = $"Slot {slotIndex:D2}";
            return;
        }

        _slotLabel.text = meta.Label;
    }

    #endregion

    #region Handlers

    private void PressSlotButton(PointerEventData _)
    {
        Clicked?.Invoke(_slotIndex);
    }

    #endregion

    #region Formatting

    private static string FormatSavedAt(string savedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(savedAtUtc))
            return "";

        if (!DateTimeOffset.TryParse(
                savedAtUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTimeOffset savedAt))
        {
            return savedAtUtc;
        }

        return savedAt
            .ToLocalTime()
            .ToString("yyyy.MM.dd HH:mm");
    }

    private static string FormatPlaytime(int playSeconds)
    {
        TimeSpan time =
            TimeSpan.FromSeconds(Math.Max(0, playSeconds));

        int hours = (int)time.TotalHours;

        return $"{hours:00}:{time.Minutes:00}:{time.Seconds:00}";
    }

    #endregion

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(
            ref missing,
            _slotButton,
            Refs.SlotButton_Button);

        AppendMissing(
            ref missing,
            _slotLabel,
            Refs.SlotLabel_Text);

        AppendMissing(
            ref missing,
            _chapterLabel,
            Refs.ChapterLabel_Text);

        AppendMissing(
            ref missing,
            _previewLabel,
            Refs.PreviewLabel_Text);

        AppendMissing(
            ref missing,
            _savedAtLabel,
            Refs.SavedAtLabel_Text);

        AppendMissing(
            ref missing,
            _playtimeLabel,
            Refs.PlaytimeLabel_Text);

        if (missing.Length > 0)
        {
            Debug.LogWarning(
                $"[VNSaveSlotButton] Missing refs:\n{missing}",
                this);

            return false;
        }

        return true;
    }
}