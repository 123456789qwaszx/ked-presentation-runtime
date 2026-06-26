using System;
using UnityEngine;
using UnityEngine.EventSystems;

public partial class PresentationUIRoot : UIRoot<PresentationUIRoot.Refs>
{
    public event Action StepNextClicked;
    
    public event Action AutoClicked;
    public event Action QuickMenuClicked;
    
    public event Action ExpandClicked;
    public event Action BackLogClicked;
    public event Action PlaybackSpeedClicked;
    
    public event Action RapidSkipDown;
    public event Action RapidSkipUp;
    public event Action RollbackClicked;
    public event Action OpenSkipPanelClicked;
    
    public event Action SaveMenuClicked;
    public event Action LoadMenuClicked;
    
    #region Refs
    
    public enum Refs
    {
        #region StageViewport Layer
        StageViewportLayer,
        
        StageShot_Root,
        StagePan_Root,
        StageZoom_Root,

        Stage00_Root,
        
        Stage00Mask_Root,
        
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
        
        
        Stage01_Root,
        
        Stage01Mask_Root,

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



        Stage02_Root,
        
        Stage02Mask_Root,

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
        
        #endregion
        
        #region StageOverlay Layer
        StageOverlayLayer,
        SpriteRig_Root,
        TextRig_Root,
        
        
        #endregion
        
        #region ScreenEffect Layer
        ScreenEffectLayer,
        // ScreenEffectRig
        
        #endregion

        #region DialogueUI Layer
        DialogueUILayer,
        
        DialogueBox_Root,
        //DialogueBox00
        //DialogueBox01
        //DialogueBox02
        //DialogueBox03
        //DialogueBox04
        //DialogueBox_Surface
        
        OptionsBox_Root,
        //VNDefaultOptionsPanel

        #endregion
        
        #region Button Layer
        ButtonLayer,
        
        StepNext_Button,
        
        Buttons_BottomRight,
        
        StepNext_Root,
        StepNext_Text,
        StepNextHotKey_Root,
        StepNextHotKey_Image,
        StepNextHotKey_Text,
        
        Buttons_TopRight,
        
        QuickMenuToggle_Root,
        QuickMenuToggle_Image,
        QuickMenuToggleKey_Text,
        QuickMenuToggle_Button,

        QuickMenu_Root,
        QuickMenuBG_Image,

        ExpandToggle_Root,
        ExpandToggle_Image,
        ExpandToggle_Text,
        ExpandToggleHotkey_Text,
        ExpandToggle_Button,

        BackLog_Root,
        BackLog_Image,
        BackLog_Text,
        BackLogHotkey_Text,
        BackLog_Button,

        PlaybackSpeedToggle_Root,
        PlaybackSpeedToggle_Image,
        PlaybackSpeedToggle_Text,
        PlaybackSpeedToggleDegree_Text,
        PlaybackSpeedToggleHotKey_Text,
        PlaybackSpeedToggle_Button,

        AutoToggle_Root,
        AutoToggleIcon_Image,
        AutoToggleHotKey_Root,
        AutoToggleHotKey_Image,
        AutoToggleHotKey_Text,
        AutoToggle_Button,

        RapidSkip_Root,
        RapidSkipIcon_Image,
        RapidSkipIcon_Text,
        RapidSkipHotKey_Root,
        RapidSkipHotKey_Image,
        RapidSkipHotKey_Text,
        RapidSkip_Button,

        Rollback_Root,
        RollbackIcon_Image,
        RollbackIcon_Text,
        RollbackHotKey_Root,
        RollbackHotKeyBG,
        RollbackHotKey_Text,
        Rollback_Button,
        
        Buttons_TopLeft,
        
        OpenSkipPanel_Root,
        OpenSkipPanel_Text,
        
        OpenSkipPanelIcon_Root,
        OpenSkipPanelIcon_Image,
        OpenSkipPanelHotKey_Root,
        OpenSkipPanelHotKey_Text,
        OpenSkipPanel_Button,
        
        // temp
        SaveMenu_Text,
        SaveMenu_Button,

        LoadMenu_Text,
        LoadMenu_Button,
        
        #endregion
    }
    
    #endregion

    private bool _isExpanded;
    private bool _isQuickMenuOpen;

    protected override void OnInitialize()
    {
        CacheRefs();
        ValidateRefs();
        
        CacheShotResponseStageProviderRefs();
        CacheDepthDefocusOverlayProviderRefs();
        CacheStageDepthContentSlotProviderRefs();
        CacheStageMaskProviderRefs();
        
        BindHandlers();
        
        SetQuickMenuOpen(false);
        SetExpanded(false);
    }
    
    private void BindHandlers()
    {
        BindEvent(View.Button(Refs.StepNext_Button), PressStepNextButton);
        BindEvent(View.Button(Refs.QuickMenuToggle_Button), PressQuickMenuToggleButton);
        BindEvent(View.Button(Refs.AutoToggle_Button), PressAutoButton);
        BindEvent(View.Button(Refs.ExpandToggle_Button), PressExpandButton);
        BindEvent(View.Button(Refs.BackLog_Button), PressBackLogButton);
        BindEvent(View.Button(Refs.PlaybackSpeedToggle_Button), PressPlaybackSpeedButton);
        BindEvent(View.Button(Refs.OpenSkipPanel_Button), PressOpenSkipPanelButton);
        BindEvent(View.Button(Refs.SaveMenu_Button), PressSaveMenuButton);
        BindEvent(View.Button(Refs.LoadMenu_Button), PressLoadMenuButton);
        
        BindEvent(View.Button(Refs.RapidSkip_Button), _ 
            => RapidSkipDown?.Invoke(), ETouchEvent.PointerDown);
        BindEvent(View.Button(Refs.RapidSkip_Button), _ 
            => RapidSkipUp?.Invoke(), ETouchEvent.PointerUp);
        
        BindEvent(View.Button(Refs.Rollback_Button), _ 
            => RollbackClicked?.Invoke(), ETouchEvent.Click);
    }
    
    #region Handlers
    
    private void PressStepNextButton(PointerEventData _)
    {
        SetExpanded(!_isExpanded);
        StepNextClicked?.Invoke();
    }
    
    private void PressAutoButton(PointerEventData _)
    {
        AutoClicked?.Invoke();
    }

    private void PressQuickMenuToggleButton(PointerEventData _)
    {
        QuickMenuClicked?.Invoke();

        SetQuickMenuOpen(!_isQuickMenuOpen);
    }
    
    private void PressExpandButton(PointerEventData _)
    {
        SetExpanded(!_isExpanded);
        ExpandClicked?.Invoke();
    }

    private void PressBackLogButton(PointerEventData _)
    {
        BackLogClicked?.Invoke();
    }

    private void PressPlaybackSpeedButton(PointerEventData _)
    {
        PlaybackSpeedClicked?.Invoke();
    }

    private void PressOpenSkipPanelButton(PointerEventData _)
    {
        OpenSkipPanelClicked?.Invoke();
    }

    private void PressSaveMenuButton(PointerEventData _)
    {
        SetExpanded(false);
        SaveMenuClicked?.Invoke();
    }

    private void PressLoadMenuButton(PointerEventData _)
    {
        SetExpanded(false);
        LoadMenuClicked?.Invoke();
    }
    
    #endregion
    
    
    private void SetQuickMenuOpen(bool open)
    {
        _isQuickMenuOpen = open;

        SetCanvasGroupVisible(View.CanvasGroup(Refs.QuickMenu_Root), open);
    }

    private void SetExpanded(bool expanded)
    {
        _isExpanded = expanded;

        SetCanvasGroupVisible(View.CanvasGroup(Refs.Buttons_BottomRight), !expanded);
        SetCanvasGroupVisible(View.CanvasGroup(Refs.Buttons_TopRight), !expanded);
        SetCanvasGroupVisible(View.CanvasGroup(Refs.Buttons_TopLeft), !expanded);
    }
    
    private static void SetCanvasGroupVisible(CanvasGroup cg, bool visible)
    {
        if (!cg)
            return;
        
        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }
}