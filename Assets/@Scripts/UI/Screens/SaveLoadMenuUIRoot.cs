using System;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

public enum SaveLoadMenuMode
{
    Save = 0,
    Load = 1
}

public sealed class SaveLoadMenuUIPanel : UIPanel<SaveLoadMenuUIPanel.Refs>
{
    public event Action<int> SlotClicked;
    public event Action CloseClicked;

    #region Refs

    public enum Refs
    {
        SaveLoadBG_Image,

        ContentRoot_Rect,

        CloseButton_BWidget,
    }

    [Header("Prefab")]
    [SerializeField] private VNSaveSlotButton _slotButtonPrefab;

    private Image _saveLoadBg;

    private RectTransform _contentRoot;

    private ButtonWidget _close;

    #endregion

    private bool _valid;
    private SaveLoadMenuMode _mode;

    protected override void OnInitialize()
    {
        _saveLoadBg = View.Image(Refs.SaveLoadBG_Image);

        _contentRoot = View.Rect(Refs.ContentRoot_Rect);

        _close = View.Widget<ButtonWidget>(Refs.CloseButton_BWidget);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif

        _close.SetLabel("돌아가기");
        _close.OnClicked += HandleClose;

        ClearSlots();
    }

    #region Event Handlers

    private void HandleClose()
    {
        CloseClicked?.Invoke();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (!_valid) return;

        _close.OnClicked -= HandleClose;
    }

    #endregion

    public void Rebuild(SaveLoadMenuMode mode, VNSaveSlotMeta[] metas)
    {
        if (!_valid) return;

        _mode = mode;
        ClearSlots();

        bool isSaveMode = _mode == SaveLoadMenuMode.Save;

        for (int i = 0; i < metas.Length; i++)
        {
            int slotIndex = i + 1;
            VNSaveSlotMeta meta = metas[i];

            VNSaveSlotButton button = Instantiate(_slotButtonPrefab, _contentRoot);
            button.Bind(slotIndex, meta, isSaveMode, HandleSlotClicked);
        }
    }

    private void HandleSlotClicked(int slotIndex)
    {
        SlotClicked?.Invoke(slotIndex);
    }

    private void ClearSlots()
    {
        if (_contentRoot == null)
            return;

        for (int i = _contentRoot.childCount - 1; i >= 0; i--)
            Destroy(_contentRoot.GetChild(i).gameObject);
    }

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _saveLoadBg, Refs.SaveLoadBG_Image);

        AppendMissing(ref missing, _contentRoot, Refs.ContentRoot_Rect);

        AppendMissing(ref missing, _close, Refs.CloseButton_BWidget);

        if (_slotButtonPrefab == null)
            missing += "- _slotButtonPrefab\n";

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[SaveLoadMenuUIRoot] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}