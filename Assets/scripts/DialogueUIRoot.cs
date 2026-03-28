using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UIRefValidation;


public class DialogueUIRoot : UIRoot<DialogueUIRoot.Refs>
{
    public event Action OnSkipPressed;
    public event Action OnAutoPressed;
    public event Action OnQuickMenuPressed;
    public event Action OnExpandPressed;
    public event Action OnShowPreviousLogPressed;
    public event Action OnSetSpeedupPressed;
    public event Action OnStepNextPressed;

    public enum DialogueBoxKind
    {
        WithPortrait = 0,
        NoPortrait = 1,
        LetterBox = 2
    }

    #region Refs

    public enum Refs
    {
        DialogueBox_Layer,
        ToggleBottomRight,
        ToggleTopRight,
        ToggleTopLeft,
        
        Background00_Root,
        Background00_Image,

        Background01_Root,
        Background01_Image,

        StepNextToggle_Root,
        StepNextToggle_Text,
        StepNextToggleHotKey_Root,
        StepNextToggleHotKey_Image,
        StepNextToggleHotKey_Text,
        StepNextToggle_Button,

        QuickMenuToggle_Root,
        QuickMenuToggle_Image,
        QuickMenuToggleKey_Text,
        QuickMenuToggle_Button,

        QuickMenu_Root,
        QuickMenuBG_Image,

        QuickExpandToggle_Root,
        QuickExpandToggle_Image,
        QuickExpandToggleHotkey_Text,
        QuickExpandToggle_Button,

        QuickDialogueLog_Root,
        QuickDialogueLog_Image,
        QuickDialogueLogHotkey_Text,
        QuickDialogueLog_Button,

        QuickSpeedToggle_Root,
        QuickSpeedToggle_Image,
        QuickSpeedToggle_Text,
        QuickSpeedToggle_Button,

        AutoToggle_Root,
        AutoToggleIcon_Image,
        AutoToggleHotKey_Root,
        AutoToggleHotKey_Text,
        AutoToggleHotKey_Button,

        SkipToggleHotKey_Root,
        SkipToggleHotKey_Text,
        SkipToggle_Text,
        SkipToggleIcon_Root,
        SkipToggleIcon_Image,
        SkipToggle_Button,

        DialogueBoxSlot00_Root,
        DialogueBoxSlot01_Root,
        DialogueBoxSlot02_Root,
        
        CharacterStageSlot00,
        CharacterStageSlot01,
        CharacterStageSlot02
    }

    // ---- Layer ----
    private CanvasGroup _dialogueBoxLayerCg;
    private CanvasGroup _toggleBottomRightCg;
    private CanvasGroup _toggleTopRightCg;
    private CanvasGroup _toggleTopLeftCg;
    
    // ---- Background ----
    private RectTransform _bg00Root;
    private Image _bg00Image;

    private RectTransform _bg01Root;
    private Image _bg01Image;

    // ---- FastForward ----
    private RectTransform _stepNextRoot;
    private TMP_Text _stepNextText;
    private RectTransform _stepNextHotKeyRoot;
    private Image _stepNextHotKeyImage;
    private TMP_Text _stepNextHotKeyText;
    private Button _stepNextButton;

    // ---- QuickMenu Toggle + Panel ----
    private RectTransform _quickMenuToggleRoot;
    private Image _quickMenuToggleImage;
    private TMP_Text _quickMenuToggleKeyText;
    private Button _quickMenuToggleButton;

    private RectTransform _quickMenuRoot;
    private CanvasGroup _quickMenuCg;
    private Image _quickMenuBgImage;

    // ---- Quick buttons ----
    private RectTransform _quickExpandRoot;
    private Image _quickExpandImage;
    private TMP_Text _quickExpandHotkeyText;
    private Button _quickExpandButton;

    private RectTransform _quickLogRoot;
    private Image _quickLogImage;
    private TMP_Text _quickLogHotkeyText;
    private Button _quickLogButton;

    private RectTransform _quickSpeedRoot;
    private Image _quickSpeedImage;
    private TMP_Text _quickSpeedText;
    private Button _quickSpeedButton;

    // ---- Auto ----
    private RectTransform _autoRoot;
    private Image _autoIconImage;
    private RectTransform _autoHotKeyRoot;
    private TMP_Text _autoHotKeyText;
    private Button _autoHotKeyButton;

    // ---- Skip ----
    private RectTransform _skipHotKeyRoot;
    private TMP_Text _skipHotKeyText;
    private TMP_Text _skipText;
    private RectTransform _skipIconRoot;
    private Image _skipIconImage;
    private Button _skipButton;

    // ---- Slots & views ----
    private CanvasGroup[] _slots;
    private IDialogueBoxView[] _boxBySlot;

    // ---- Valid flag ----
    private bool _valid;
    
    // ---- HUD 숨김 상태 ----
    private bool _isExpanded; 
    public bool IsExpanded => _isExpanded;

    #endregion

    public RectTransform CharRigSlot => View.Rect(Refs.CharacterStageSlot00);
    public RectTransform CharRigSlot1 => View.Rect(Refs.CharacterStageSlot01);
    public RectTransform CharRigSlot2 => View.Rect(Refs.CharacterStageSlot02);

    protected override void Initialize()
    {
        CacheRefs();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif

        BindHandlers();

        CloseCanvasGroup(_quickMenuCg);
    }
    

    private void CacheRefs()
    {
        // Layer
        _dialogueBoxLayerCg = View.CanvasGroup(Refs.DialogueBox_Layer);
        _toggleBottomRightCg = View.CanvasGroup(Refs.ToggleBottomRight);
        _toggleTopRightCg    = View.CanvasGroup(Refs.ToggleTopRight);
        _toggleTopLeftCg     = View.CanvasGroup(Refs.ToggleTopLeft);
        
        // Background
        _bg00Root = View.Rect(Refs.Background00_Root);
        _bg00Image = View.Image(Refs.Background00_Image);

        _bg01Root = View.Rect(Refs.Background01_Root);
        _bg01Image = View.Image(Refs.Background01_Image);

        // FastForward
        _stepNextRoot = View.Rect(Refs.StepNextToggle_Root);
        _stepNextText = View.Text(Refs.StepNextToggle_Text);
        _stepNextHotKeyRoot = View.Rect(Refs.StepNextToggleHotKey_Root);
        _stepNextHotKeyImage = View.Image(Refs.StepNextToggleHotKey_Image);
        _stepNextHotKeyText = View.Text(Refs.StepNextToggleHotKey_Text);
        _stepNextButton = View.Button(Refs.StepNextToggle_Button);

        // QuickMenu toggle
        _quickMenuToggleRoot = View.Rect(Refs.QuickMenuToggle_Root);
        _quickMenuToggleImage = View.Image(Refs.QuickMenuToggle_Image);
        _quickMenuToggleKeyText = View.Text(Refs.QuickMenuToggleKey_Text);
        _quickMenuToggleButton = View.Button(Refs.QuickMenuToggle_Button);

        // QuickMenu panel
        _quickMenuRoot = View.Rect(Refs.QuickMenu_Root);
        _quickMenuCg = View.CanvasGroup(Refs.QuickMenu_Root);
        _quickMenuBgImage = View.Image(Refs.QuickMenuBG_Image);

        // QuickExpand (HUD Toggle)
        _quickExpandRoot = View.Rect(Refs.QuickExpandToggle_Root);
        _quickExpandImage = View.Image(Refs.QuickExpandToggle_Image);
        _quickExpandHotkeyText = View.Text(Refs.QuickExpandToggleHotkey_Text);
        _quickExpandButton = View.Button(Refs.QuickExpandToggle_Button);

        // Dialogue Log
        _quickLogRoot = View.Rect(Refs.QuickDialogueLog_Root);
        _quickLogImage = View.Image(Refs.QuickDialogueLog_Image);
        _quickLogHotkeyText = View.Text(Refs.QuickDialogueLogHotkey_Text);
        _quickLogButton = View.Button(Refs.QuickDialogueLog_Button);

        // Speed
        _quickSpeedRoot = View.Rect(Refs.QuickSpeedToggle_Root);
        _quickSpeedImage = View.Image(Refs.QuickSpeedToggle_Image);
        _quickSpeedText = View.Text(Refs.QuickSpeedToggle_Text);
        _quickSpeedButton = View.Button(Refs.QuickSpeedToggle_Button);

        // Auto
        _autoRoot = View.Rect(Refs.AutoToggle_Root);
        _autoIconImage = View.Image(Refs.AutoToggleIcon_Image);
        _autoHotKeyRoot = View.Rect(Refs.AutoToggleHotKey_Root);
        _autoHotKeyText = View.Text(Refs.AutoToggleHotKey_Text);
        _autoHotKeyButton = View.Button(Refs.AutoToggleHotKey_Button);

        // Skip
        _skipHotKeyRoot = View.Rect(Refs.SkipToggleHotKey_Root);
        _skipHotKeyText = View.Text(Refs.SkipToggleHotKey_Text);
        _skipText = View.Text(Refs.SkipToggle_Text);
        _skipIconRoot = View.Rect(Refs.SkipToggleIcon_Root);
        _skipIconImage = View.Image(Refs.SkipToggleIcon_Image);
        _skipButton = View.Button(Refs.SkipToggle_Button);

        // Slots
        _slots = new[]
        {
            View.CanvasGroup(Refs.DialogueBoxSlot00_Root),
            View.CanvasGroup(Refs.DialogueBoxSlot01_Root),
            View.CanvasGroup(Refs.DialogueBoxSlot02_Root),
        };

        _boxBySlot = new IDialogueBoxView[_slots.Length];
        for (int i = 0; i < _slots.Length; i++)
        {
            _boxBySlot[i] = _slots[i] != null
                ? _slots[i].GetComponentInChildren<IDialogueBoxView>(includeInactive: true)
                : null;
        }
    }

    private void BindHandlers()
    {
        BindEvent(_autoHotKeyButton, PressAutoButton);
        BindEvent(_stepNextButton, PressStepNextButton);
        BindEvent(_quickMenuToggleButton, PressQuickMenuToggleButton);
        BindEvent(_quickExpandButton, PressExpandButton);
        BindEvent(_quickLogButton, PressLogButton);
        BindEvent(_quickSpeedButton, PressSpeedButton);
        BindEvent(_skipButton, PressSkipButton);
    }

    #region Handler
    
    private void PressAutoButton(PointerEventData _)
    {
        OnAutoPressed?.Invoke();
    }
    
    private void PressStepNextButton(PointerEventData _)
    {
        if (_isExpanded)
        {
            SetExpanded(false);
            return;
        }

        OnStepNextPressed?.Invoke();
    }
    
    private void PressQuickMenuToggleButton(PointerEventData _)
    {
        OnQuickMenuPressed?.Invoke();

        bool isOpen = _quickMenuCg.alpha > 0.5f;
        
        if (isOpen) CloseCanvasGroup(_quickMenuCg);
        else OpenCanvasGroup(_quickMenuCg);
    }
    
    private void PressExpandButton(PointerEventData _)
    {
        ToggleExpand();
        OnExpandPressed?.Invoke();
    }
    private void PressLogButton(PointerEventData _)
    {
        OnShowPreviousLogPressed?.Invoke();
    }
    
    private void PressSpeedButton(PointerEventData _)
    {
        OnSetSpeedupPressed?.Invoke();
    }

    private void PressSkipButton(PointerEventData _)
    {
        OnSkipPressed?.Invoke();
    }
    
    #endregion
    
    private bool ValidateRefs()
    {
        string missing = "";
        
        // Layer
        AppendMissing(ref missing, _dialogueBoxLayerCg, Refs.DialogueBox_Layer);
        AppendMissing(ref missing, _toggleBottomRightCg, Refs.ToggleBottomRight);
        AppendMissing(ref missing, _toggleTopRightCg,    Refs.ToggleTopRight);
        AppendMissing(ref missing, _toggleTopLeftCg,     Refs.ToggleTopLeft);
        
        // Background
        AppendMissing(ref missing, _bg00Root, Refs.Background00_Root);
        AppendMissing(ref missing, _bg00Image, Refs.Background00_Image);
        AppendMissing(ref missing, _bg01Root, Refs.Background01_Root);
        AppendMissing(ref missing, _bg01Image, Refs.Background01_Image);

        // FastForward
        AppendMissing(ref missing, _stepNextRoot, Refs.StepNextToggle_Root);
        AppendMissing(ref missing, _stepNextText, Refs.StepNextToggle_Text);
        AppendMissing(ref missing, _stepNextHotKeyRoot, Refs.StepNextToggleHotKey_Root);
        AppendMissing(ref missing, _stepNextHotKeyImage, Refs.StepNextToggleHotKey_Image);
        AppendMissing(ref missing, _stepNextHotKeyText, Refs.StepNextToggleHotKey_Text);
        AppendMissing(ref missing, _stepNextButton, Refs.StepNextToggle_Button);

        // QuickMenu toggle
        AppendMissing(ref missing, _quickMenuToggleRoot, Refs.QuickMenuToggle_Root);
        AppendMissing(ref missing, _quickMenuToggleImage, Refs.QuickMenuToggle_Image);
        AppendMissing(ref missing, _quickMenuToggleKeyText, Refs.QuickMenuToggleKey_Text);
        AppendMissing(ref missing, _quickMenuToggleButton, Refs.QuickMenuToggle_Button);

        // QuickMenu panel
        AppendMissing(ref missing, _quickMenuRoot, Refs.QuickMenu_Root);
        // CanvasGroup은 View.CanvasGroup이 자동 추가하므로 엄격 체크가 필요 없으면 빼도 됨.
        AppendMissing(ref missing, _quickMenuCg, Refs.QuickMenu_Root);
        AppendMissing(ref missing, _quickMenuBgImage, Refs.QuickMenuBG_Image);

        // QuickExpand
        AppendMissing(ref missing, _quickExpandRoot, Refs.QuickExpandToggle_Root);
        AppendMissing(ref missing, _quickExpandImage, Refs.QuickExpandToggle_Image);
        AppendMissing(ref missing, _quickExpandHotkeyText, Refs.QuickExpandToggleHotkey_Text);
        AppendMissing(ref missing, _quickExpandButton, Refs.QuickExpandToggle_Button);

        // Log
        AppendMissing(ref missing, _quickLogRoot, Refs.QuickDialogueLog_Root);
        AppendMissing(ref missing, _quickLogImage, Refs.QuickDialogueLog_Image);
        AppendMissing(ref missing, _quickLogHotkeyText, Refs.QuickDialogueLogHotkey_Text);
        AppendMissing(ref missing, _quickLogButton, Refs.QuickDialogueLog_Button);

        // Speed
        AppendMissing(ref missing, _quickSpeedRoot, Refs.QuickSpeedToggle_Root);
        AppendMissing(ref missing, _quickSpeedImage, Refs.QuickSpeedToggle_Image);
        AppendMissing(ref missing, _quickSpeedText, Refs.QuickSpeedToggle_Text);
        AppendMissing(ref missing, _quickSpeedButton, Refs.QuickSpeedToggle_Button);

        // Auto
        AppendMissing(ref missing, _autoRoot, Refs.AutoToggle_Root);
        AppendMissing(ref missing, _autoIconImage, Refs.AutoToggleIcon_Image);
        AppendMissing(ref missing, _autoHotKeyRoot, Refs.AutoToggleHotKey_Root);
        AppendMissing(ref missing, _autoHotKeyText, Refs.AutoToggleHotKey_Text);
        AppendMissing(ref missing, _autoHotKeyButton, Refs.AutoToggleHotKey_Button);

        // Skip
        AppendMissing(ref missing, _skipHotKeyRoot, Refs.SkipToggleHotKey_Root);
        AppendMissing(ref missing, _skipHotKeyText, Refs.SkipToggleHotKey_Text);
        AppendMissing(ref missing, _skipText, Refs.SkipToggle_Text);
        AppendMissing(ref missing, _skipIconRoot, Refs.SkipToggleIcon_Root);
        AppendMissing(ref missing, _skipIconImage, Refs.SkipToggleIcon_Image);
        AppendMissing(ref missing, _skipButton, Refs.SkipToggle_Button);

        // Slots
        var s0 = _slots != null && _slots.Length > 0 ? _slots[0] : null;
        var s1 = _slots != null && _slots.Length > 1 ? _slots[1] : null;
        var s2 = _slots != null && _slots.Length > 2 ? _slots[2] : null;

        AppendMissing(ref missing, s0, Refs.DialogueBoxSlot00_Root);
        AppendMissing(ref missing, s1, Refs.DialogueBoxSlot01_Root);
        AppendMissing(ref missing, s2, Refs.DialogueBoxSlot02_Root);

        // IDialogueBoxView (슬롯 밑에 반드시 있어야 함)
        if (_boxBySlot == null || _boxBySlot.Length < 3)
        {
            if (missing.Length > 0) missing += "\n";
            missing += "- IDialogueBoxView array not built";
        }
        else
        {
            if (_boxBySlot[0] == null) AddMissingLine(ref missing, "- IDialogueBoxView under DialogueBoxSlot00_Root");
            if (_boxBySlot[1] == null) AddMissingLine(ref missing, "- IDialogueBoxView under DialogueBoxSlot01_Root");
            if (_boxBySlot[2] == null) AddMissingLine(ref missing, "- IDialogueBoxView under DialogueBoxSlot02_Root");
        }

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[DialogueUIRoot] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }

    private static void AddMissingLine(ref string acc, string line)
    {
        if (acc.Length > 0) acc += "\n";
        acc += line;
    }
    
    
    // Presenter(Yarn)가 사용할: kind에 해당하는 박스 뷰를 얻는다.
    public IDialogueBoxView GetBox(DialogueBoxKind kind)
    {
        int slotIndex = (int)kind;
        if (slotIndex < 0 || slotIndex >= _boxBySlot.Length) return null;
        return _boxBySlot[slotIndex];
    }

    // kind에 해당하는 슬롯만 보이게 한다.
    public void ShowBox(DialogueBoxKind kind)
    {
        HideAllBoxes();

        int slotIndex = (int)kind;
        if (slotIndex < 0 || slotIndex >= _slots.Length) return;

        OpenCanvasGroup(_slots[slotIndex]);

        var box = _boxBySlot[slotIndex];
        if (box != null)
            box.SetVisible(true);
    }

    public void HideAllBoxes()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            CloseCanvasGroup(_slots[i]);

            var box = _boxBySlot[i];
            if (box != null)
                box.SetVisible(false);
        }
    }

    private static void OpenCanvasGroup(CanvasGroup cg)
    {
        if (!cg) return;
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    private static void CloseCanvasGroup(CanvasGroup cg)
    {
        if (!cg) return;
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }
    
    public void SetAutoModeActive(bool active)
    {
        if (_autoIconImage) _autoIconImage.enabled = true;
        if (_autoRoot) _autoRoot.gameObject.SetActive(true);

        if (_autoHotKeyText) _autoHotKeyText.text = active ? "AUTO ON" : "AUTO";
    }

    public void SetSkipModeActive(bool active)
    {
        if (_skipText) _skipText.text = active ? "SKIP ON" : "SKIP";
    }

    public void SetBacklogOpen(bool open)
    {
        if (_quickLogHotkeyText) _quickLogHotkeyText.text = open ? "LOG (OPEN)" : "LOG";
    }
    
    public void SetInputBlocked(bool blocked)
    {
        if (_stepNextButton) _stepNextButton.interactable = !blocked;
        if (_skipButton) _skipButton.interactable = !blocked;
        if (_autoHotKeyButton) _autoHotKeyButton.interactable = !blocked;
    }
    
    public void ToggleExpand()
    {
        SetExpanded(!_isExpanded);
    }

    public void SetExpanded(bool expanded)
    {
        _isExpanded = expanded;

        SetLayerVisible(_dialogueBoxLayerCg,  visible: !expanded);
        SetLayerVisible(_toggleBottomRightCg, visible: !expanded);
        SetLayerVisible(_toggleTopRightCg,    visible: !expanded);
        SetLayerVisible(_toggleTopLeftCg,     visible: !expanded);

        //CloseCanvasGroup(_quickMenuCg);
    }

    private static void SetLayerVisible(CanvasGroup cg, bool visible)
    {
        if (!cg) return;
        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }
}