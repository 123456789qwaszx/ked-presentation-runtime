using System;
using UnityEngine;
using UnityEngine.EventSystems;

public partial class PresentationUIRoot : UIRoot<PresentationUIRoot.Refs>
{
    public event Action FastForwardDown;
    public event Action FastForwardUp;
    public event Action RollbackClicked;
    public event Action SkipMenuClicked;
    public event Action AutoClicked;
    public event Action QuickMenuClicked;
    public event Action ExpandClicked;
    public event Action BackLogClicked;
    public event Action PlaybackSpeedClicked;
    public event Action SaveMenuClicked;
    public event Action LoadMenuClicked;
    
    public event Action HurryUpClicked;

    public enum Refs
    {
        StageShot_Root,
        StagePan_Root,
        StageZoom_Root,

        Stage00_Root,
        
        Stage00DepthSystem_Root,
        Stage00Depth_Far_Root,
        Stage00Depth_Far_FramingTransform,
        Stage00Depth_Far_FramingScale,
        Stage00Depth_Far_Content,
        Stage00FarFrostedGlassMask,
        Stage00FarFrostedGlassRawImage,
        
        Stage00Depth_Back_Root,
        Stage00Depth_Back_FramingTransform,
        Stage00Depth_Back_FramingScale,
        Stage00Depth_Back_Content,
        Stage00BackFrostedGlassMask,
        Stage00BackFrostedGlassRawImage,
        
        Stage00Depth_Mid_Root,
        Stage00Depth_Mid_FramingTransform,
        Stage00Depth_Mid_FramingScale,
        Stage00Depth_Mid_Content,
        Stage00MidFrostedGlassMask,
        Stage00MidFrostedGlassRawImage,
        
        Stage00Depth_Front_Root,
        Stage00Depth_Front_FramingTransform,
        Stage00Depth_Front_FramingScale,
        Stage00Depth_Front_Content,
        Stage00FrontFrostedGlassMask,
        Stage00FrontFrostedGlassRawImage,
        
        Stage00Depth_Close_Root,
        Stage00Depth_Close_FramingTransform,
        Stage00Depth_Close_FramingScale,
        Stage00Depth_Close_Content,
        Stage00CloseFrostedGlassMask,
        Stage00CloseFrostedGlassRawImage,
        
        Stage00Overlay_Root,
        

        Stage01_Root,

        Stage01DepthSystem_Root,
        Stage01Depth_Far_Root,
        Stage01Depth_Far_FramingTransform,
        Stage01Depth_Far_FramingScale,
        Stage01Depth_Far_Content,
        Stage01FarFrostedGlassMask,
        Stage01FarFrostedGlassRawImage,

        Stage01Depth_Back_Root,
        Stage01Depth_Back_FramingTransform,
        Stage01Depth_Back_FramingScale,
        Stage01Depth_Back_Content,
        Stage01BackFrostedGlassMask,
        Stage01BackFrostedGlassRawImage,

        Stage01Depth_Mid_Root,
        Stage01Depth_Mid_FramingTransform,
        Stage01Depth_Mid_FramingScale,
        Stage01Depth_Mid_Content,
        Stage01MidFrostedGlassMask,
        Stage01MidFrostedGlassRawImage,

        Stage01Depth_Front_Root,
        Stage01Depth_Front_FramingTransform,
        Stage01Depth_Front_FramingScale,
        Stage01Depth_Front_Content,
        Stage01FrontFrostedGlassMask,
        Stage01FrontFrostedGlassRawImage,

        Stage01Depth_Close_Root,
        Stage01Depth_Close_FramingTransform,
        Stage01Depth_Close_FramingScale,
        Stage01Depth_Close_Content,
        Stage01CloseFrostedGlassMask,
        Stage01CloseFrostedGlassRawImage,

        Stage01Overlay_Root,
        SlantedMaskEdgeGraphic,


        Stage02_Root,

        Stage02DepthSystem_Root,
        Stage02Depth_Far_Root,
        Stage02Depth_Far_FramingTransform,
        Stage02Depth_Far_FramingScale,
        Stage02Depth_Far_Content,
        Stage02FarFrostedGlassMask,
        Stage02FarFrostedGlassRawImage,

        Stage02Depth_Back_Root,
        Stage02Depth_Back_FramingTransform,
        Stage02Depth_Back_FramingScale,
        Stage02Depth_Back_Content,
        Stage02BackFrostedGlassMask,
        Stage02BackFrostedGlassRawImage,

        Stage02Depth_Mid_Root,
        Stage02Depth_Mid_FramingTransform,
        Stage02Depth_Mid_FramingScale,
        Stage02Depth_Mid_Content,
        Stage02MidFrostedGlassMask,
        Stage02MidFrostedGlassRawImage,

        Stage02Depth_Front_Root,
        Stage02Depth_Front_FramingTransform,
        Stage02Depth_Front_FramingScale,
        Stage02Depth_Front_Content,
        Stage02FrontFrostedGlassMask,
        Stage02FrontFrostedGlassRawImage,

        Stage02Depth_Close_Root,
        Stage02Depth_Close_FramingTransform,
        Stage02Depth_Close_FramingScale,
        Stage02Depth_Close_Content,
        Stage02CloseFrostedGlassMask,
        Stage02CloseFrostedGlassRawImage,

        Stage02Overlay_Root,

        DialogueUI_Root,

        Choice_Root,
        SystemUI_Root,
        
        VerticalStripWipe,
        FocusBlurCurtain,

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

        QuickSaveMenu_Root,
        QuickSaveMenu_Text,
        QuickSaveMenu_Button,

        QuickLoadMenu_Root,
        QuickLoadMenu_Text,
        QuickLoadMenu_Button,

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
        SkipToggle_Button,
        
        ScreenEffectLayer,
        FullscreenFade_Root,
        Letterbox_Root,
        Flash_Root,
        ScreenOverlay_Root,
        
        ScreenFlashOverlay_Image,
        ScreenVignetteOverlay_Image,
        ScreenNoiseOverlay_Image,
    }

    [SerializeField] private DialogueBoxHost dialogueBoxHost;

    private bool _isExpanded;

    protected override void OnInitialize()
    {
        BindHandlers();
        CloseCanvasGroup(View.CanvasGroup(Refs.QuickMenu_Root));
        ApplyToggleVisibility();
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
        BindEvent(View.Button(Refs.QuickSaveMenu_Button), PressSaveMenuButton);
        BindEvent(View.Button(Refs.QuickLoadMenu_Button), PressLoadMenuButton);
        BindEvent(View.Button(Refs.SpeedUpToggleHotKey_Button), _ => FastForwardDown?.Invoke(), ETouchEvent.PointerDown);
        BindEvent(View.Button(Refs.SpeedUpToggleHotKey_Button), _ => FastForwardUp?.Invoke(), ETouchEvent.PointerUp);
        BindEvent(View.Button(Refs.RollbackToggleHotKey_Button), _ => RollbackClicked?.Invoke(), ETouchEvent.Click);
    }

    private void PressAutoButton(PointerEventData _)
    {
        AutoClicked?.Invoke();
    }

    private void PressStepNextButton(PointerEventData _)
    {
        if (_isExpanded)
        {
            SetExpanded(false);
            return;
        }

        HurryUpClicked?.Invoke();
    }

    private void PressQuickMenuToggleButton(PointerEventData _)
    {
        QuickMenuClicked?.Invoke();

        SetQuickMenuOpen(!IsQuickMenuOpen());
    }

    private void PressExpandButton(PointerEventData _)
    {
        ToggleExpand();
        ExpandClicked?.Invoke();
    }

    private void PressLogButton(PointerEventData _)
    {
        BackLogClicked?.Invoke();
    }

    private void PressSpeedButton(PointerEventData _)
    {
        PlaybackSpeedClicked?.Invoke();
    }

    private void PressSkipButton(PointerEventData _)
    {
        SkipMenuClicked?.Invoke();
    }

    private void PressSaveMenuButton(PointerEventData _)
    {
        SetQuickMenuOpen(false);
        SaveMenuClicked?.Invoke();
    }

    private void PressLoadMenuButton(PointerEventData _)
    {
        SetQuickMenuOpen(false);
        LoadMenuClicked?.Invoke();
    }

    public void SetQuickMenuOpen(bool open)
    {
        CanvasGroup quickMenu = View.CanvasGroup(Refs.QuickMenu_Root);

        if (open)
            OpenCanvasGroup(quickMenu);
        else
            CloseCanvasGroup(quickMenu);
    }

    public bool IsQuickMenuOpen()
    {
        CanvasGroup quickMenu = View.CanvasGroup(Refs.QuickMenu_Root);
        return quickMenu != null && quickMenu.alpha > 0.5f;
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

        ApplyToggleVisibility();
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