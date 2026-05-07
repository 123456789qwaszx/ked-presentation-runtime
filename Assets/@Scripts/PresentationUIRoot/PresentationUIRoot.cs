using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PresentationUIRoot : UIRoot<PresentationUIRoot.Refs>
{
    public event Action OnSpeedUpHoldStarted;
    public event Action OnSpeedUpHoldEnded;

    public event Action OnRollbackOneStepPressed;

    public event Action OnSkipPressed;
    public event Action OnAutoPressed;
    public event Action OnQuickMenuPressed;
    public event Action OnExpandPressed;
    public event Action OnShowPreviousLogPressed;
    public event Action OnSetSpeedupPressed;
    public event Action OnStepNextPressed;

    public enum Refs
    {
        FullscreenFade_Root,
        Letterbox_Root,
        Flash_Root,
        ScreenOverlay_Root,

        StageShot_Root,
        StagePan_Root,
        StageZoom_Root,
        Stage_Root,
        BackgroundSystem_Root,
        BGShot_Root,
        BGContent_Root,

        BGOverlay_Root,

        CharacterSystem_Root,

        CharSlotLeft_Root,
        CharSlotLeftFocus_Root,
        CharSlotLeftRig_Root,

        CharSlotCenter_Root,
        CharSlotCenterFocus_Root,
        CharSlotCenterRig_Root,

        CharSlotRight_Root,
        CharSlotRightFocus_Root,
        CharSlotRightRig_Root,

        Foreground_Root,

        DialogueUI_Root,
        DialogueBox_Root,
        NameBox_Root,
        NarrationBox_Root,

        Choice_Root,
        SystemUI_Root,

        ToggleBottomRight,
        ToggleTopRight,
        ToggleTopLeft,

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

        SpeedUpToggle_Root,
        SpeedUpToggleIcon_Image,
        SpeedUpToggleHotKey_Button,

        RollbackToggleHotKey_Button,

        SkipToggleHotKey_Root,
        SkipToggleHotKey_Text,
        SkipToggle_Text,
        SkipToggleIcon_Root,
        SkipToggleIcon_Image,
        SkipToggle_Button
    }

    [SerializeField] private DialogueBoxHost dialogueBoxHost;

    private bool _isExpanded;
    private bool _isDialogueUiSuppressed;

    public bool IsExpanded => _isExpanded;
    public bool IsDialogueUiSuppressed => _isDialogueUiSuppressed;

    protected override void Initialize()
    {
        BindHandlers();

        CloseCanvasGroup(View.CanvasGroup(Refs.QuickMenu_Root));

        ApplyDialogueUiVisibility();
        ApplyToggleVisibility();
    }

    public RectTransform ResolveRect(Refs key) => View.Rect(key);
    public CanvasGroup ResolveCanvasGroup(Refs key) => View.CanvasGroup(key);
    public Image ResolveImage(Refs key) => View.Image(key);

    /// <summary>
    /// DialogueUI_Root를 출력 레이어에서 임시로 숨긴다.
    /// RollbackSeek / Skip처럼 대사 표시 파이프라인은 돌지만
    /// 사용자에게는 보여주면 안 되는 상태에서 사용한다.
    /// </summary>
    public void SetDialogueUiSuppressed(bool suppressed)
    {
        _isDialogueUiSuppressed = suppressed;
        ApplyDialogueUiVisibility();
    }

    /// <summary>
    /// PresentationSessionContext 기준으로 DialogueUI 표시 억제 상태를 갱신한다.
    /// 현재 정책: RollbackSeek 또는 Skip 중에는 DialogueUI_Root를 숨긴다.
    /// </summary>
    public void RefreshDialogueUiSuppression(PresentationSessionContext context)
    {
        if (context == null)
        {
            SetDialogueUiSuppressed(false);
            return;
        }

        SetDialogueUiSuppressed(context.IsRollbackSeeking || context.IsSkipping);
    }

    private void BindHandlers()
    {
        BindEvent(View.Button(Refs.AutoToggleHotKey_Button), PressAutoButton);
        BindEvent(View.Button(Refs.StepNextToggle_Button), PressStepNextButton);
        BindEvent(View.Button(Refs.QuickMenuToggle_Button), PressQuickMenuToggleButton);
        BindEvent(View.Button(Refs.QuickExpandToggle_Button), PressExpandButton);
        BindEvent(View.Button(Refs.QuickDialogueLog_Button), PressLogButton);
        BindEvent(View.Button(Refs.QuickSpeedToggle_Button), PressSpeedButton);
        BindEvent(View.Button(Refs.SkipToggle_Button), PressSkipButton);

        BindEvent(
            View.Button(Refs.SpeedUpToggleHotKey_Button),
            _ => OnSpeedUpHoldStarted?.Invoke(),
            ETouchEvent.PointerDown);

        BindEvent(
            View.Button(Refs.SpeedUpToggleHotKey_Button),
            _ => OnSpeedUpHoldEnded?.Invoke(),
            ETouchEvent.PointerUp);

        BindEvent(
            View.Button(Refs.RollbackToggleHotKey_Button),
            _ => OnRollbackOneStepPressed?.Invoke(),
            ETouchEvent.Click);
    }

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

        CanvasGroup quickMenu = View.CanvasGroup(Refs.QuickMenu_Root);
        bool isOpen = quickMenu.alpha > 0.5f;

        if (isOpen) CloseCanvasGroup(quickMenu);
        else OpenCanvasGroup(quickMenu);
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

    public void SetAutoModeActive(bool active)
    {
        View.Image(Refs.AutoToggleIcon_Image).enabled = true;
        View.Rect(Refs.AutoToggle_Root).gameObject.SetActive(true);
        View.Text(Refs.AutoToggleHotKey_Text).text = active ? "AUTO ON" : "AUTO";
    }

    public void SetSkipModeActive(bool active)
    {
        View.Text(Refs.SkipToggle_Text).text = active ? "SKIP ON" : "SKIP";
    }

    public void SetBacklogOpen(bool open)
    {
        View.Text(Refs.QuickDialogueLogHotkey_Text).text = open ? "LOG (OPEN)" : "LOG";
    }

    public void SetInputBlocked(bool blocked)
    {
        View.Button(Refs.StepNextToggle_Button).interactable = !blocked;
        View.Button(Refs.SkipToggle_Button).interactable = !blocked;
        View.Button(Refs.AutoToggleHotKey_Button).interactable = !blocked;
    }

    public void ToggleExpand()
    {
        SetExpanded(!_isExpanded);
    }

    public void SetExpanded(bool expanded)
    {
        if (_isExpanded == expanded)
            return;

        _isExpanded = expanded;

        ApplyDialogueUiVisibility();
        ApplyToggleVisibility();
    }

    private void ApplyDialogueUiVisibility()
    {
        bool visible = !_isExpanded && !_isDialogueUiSuppressed;
        Debug.Log($"{visible}, {_isExpanded}, {_isDialogueUiSuppressed}");
        SetLayerVisible(View.CanvasGroup(Refs.DialogueUI_Root), visible);
        
        SetLayerVisible(View.CanvasGroup(Refs.Stage_Root), visible);
    }

    private void ApplyToggleVisibility()
    {
        bool visible = !_isExpanded;

        SetLayerVisible(View.CanvasGroup(Refs.ToggleBottomRight), visible);
        SetLayerVisible(View.CanvasGroup(Refs.ToggleTopRight), visible);
        SetLayerVisible(View.CanvasGroup(Refs.ToggleTopLeft), visible);
    }

    private static void OpenCanvasGroup(CanvasGroup cg)
    {
        SetLayerVisible(cg, true);
    }

    private static void CloseCanvasGroup(CanvasGroup cg)
    {
        SetLayerVisible(cg, false);
    }

    private static void SetLayerVisible(CanvasGroup cg, bool visible)
    {
        if (cg == null)
            return;

        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }
}