using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

public partial class PresentationUIRoot
{
    private bool _stageViewportRefsValid;
    private bool _screenEffectRefsValid;
    private bool _dialogueUIRefsValid;
    private bool _buttonLayerRefsValid;

    #region StageViewport Layer Cache

    private RectTransform _stageViewportLayer;

    private RectTransform _stageShotRoot;
    private RectTransform _stagePanRoot;
    private RectTransform _stageZoomRoot;

    private RectTransform _stage00Root;

    private RectTransform _stage00DepthSystemRoot;
    private RectTransform _stage00DepthFarRoot;
    private RectTransform _stage00DepthFarFramingTransform;
    private RectTransform _stage00DepthFarFramingScale;
    private RectTransform _stage00DepthFarContent;

    private RectTransform _stage00DepthBackRoot;
    private RectTransform _stage00DepthBackFramingTransform;
    private RectTransform _stage00DepthBackFramingScale;
    private RectTransform _stage00DepthBackContent;

    private RectTransform _stage00DepthMidRoot;
    private RectTransform _stage00DepthMidFramingTransform;
    private RectTransform _stage00DepthMidFramingScale;
    private RectTransform _stage00DepthMidContent;

    private RectTransform _stage00DepthFrontRoot;
    private RectTransform _stage00DepthFrontFramingTransform;
    private RectTransform _stage00DepthFrontFramingScale;
    private RectTransform _stage00DepthFrontContent;

    private RectTransform _stage00DepthCloseRoot;
    private RectTransform _stage00DepthCloseFramingTransform;
    private RectTransform _stage00DepthCloseFramingScale;
    private RectTransform _stage00DepthCloseContent;

    private RectTransform _stage01Root;

    private RectTransform _stage01DepthSystemRoot;
    private RectTransform _stage01DepthFarRoot;
    private RectTransform _stage01DepthFarFramingTransform;
    private RectTransform _stage01DepthFarFramingScale;
    private RectTransform _stage01DepthFarContent;

    private RectTransform _stage01DepthBackRoot;
    private RectTransform _stage01DepthBackFramingTransform;
    private RectTransform _stage01DepthBackFramingScale;
    private RectTransform _stage01DepthBackContent;

    private RectTransform _stage01DepthMidRoot;
    private RectTransform _stage01DepthMidFramingTransform;
    private RectTransform _stage01DepthMidFramingScale;
    private RectTransform _stage01DepthMidContent;

    private RectTransform _stage01DepthFrontRoot;
    private RectTransform _stage01DepthFrontFramingTransform;
    private RectTransform _stage01DepthFrontFramingScale;
    private RectTransform _stage01DepthFrontContent;

    private RectTransform _stage01DepthCloseRoot;
    private RectTransform _stage01DepthCloseFramingTransform;
    private RectTransform _stage01DepthCloseFramingScale;
    private RectTransform _stage01DepthCloseContent;

    private RectTransform _stage02Root;

    private RectTransform _stage02DepthSystemRoot;
    private RectTransform _stage02DepthFarRoot;
    private RectTransform _stage02DepthFarFramingTransform;
    private RectTransform _stage02DepthFarFramingScale;
    private RectTransform _stage02DepthFarContent;

    private RectTransform _stage02DepthBackRoot;
    private RectTransform _stage02DepthBackFramingTransform;
    private RectTransform _stage02DepthBackFramingScale;
    private RectTransform _stage02DepthBackContent;

    private RectTransform _stage02DepthMidRoot;
    private RectTransform _stage02DepthMidFramingTransform;
    private RectTransform _stage02DepthMidFramingScale;
    private RectTransform _stage02DepthMidContent;

    private RectTransform _stage02DepthFrontRoot;
    private RectTransform _stage02DepthFrontFramingTransform;
    private RectTransform _stage02DepthFrontFramingScale;
    private RectTransform _stage02DepthFrontContent;

    private RectTransform _stage02DepthCloseRoot;
    private RectTransform _stage02DepthCloseFramingTransform;
    private RectTransform _stage02DepthCloseFramingScale;
    private RectTransform _stage02DepthCloseContent;

    #endregion

    #region ScreenEffect Layer Cache

    private RectTransform _screenEffectLayer;
    private RectTransform _verticalStripWipe;
    private RectTransform _focusBlurCurtain;

    #endregion

    #region DialogueUI Layer Cache

    private RectTransform _dialogueUILayer;
    private RectTransform _dialogueBoxRoot;
    private RectTransform _optionsBoxRoot;

    #endregion

    #region Button Layer Cache

    private RectTransform _buttonLayer;

    private Button _stepNextButton;

    private CanvasGroup _buttonsBottomRightGroup;

    private RectTransform _stepNextRoot;
    private TMP_Text _stepNextText;
    private RectTransform _stepNextHotKeyRoot;
    private Image _stepNextHotKeyImage;
    private TMP_Text _stepNextHotKeyText;

    private CanvasGroup _buttonsTopRightGroup;

    private RectTransform _quickMenuToggleRoot;
    private Image _quickMenuToggleImage;
    private TMP_Text _quickMenuToggleKeyText;
    private Button _quickMenuToggleButton;

    private CanvasGroup _quickMenuRootGroup;
    private Image _quickMenuBgImage;

    private RectTransform _expandToggleRoot;
    private Image _expandToggleImage;
    private TMP_Text _expandToggleText;
    private TMP_Text _expandToggleHotkeyText;
    private Button _expandButton;

    private RectTransform _backLogRoot;
    private Image _backLogImage;
    private TMP_Text _backLogText;
    private TMP_Text _backLogHotkeyText;
    private Button _backLogButton;

    private RectTransform _playbackSpeedToggleRoot;
    private Image _playbackSpeedToggleImage;
    private TMP_Text _playbackSpeedToggleText;
    private TMP_Text _playbackSpeedToggleDegreeText;
    private TMP_Text _playbackSpeedToggleHotKeyText;
    private Button _playbackSpeedButton;

    private RectTransform _autoToggleRoot;
    private Image _autoToggleIconImage;
    private RectTransform _autoToggleHotKeyRoot;
    private Image _autoToggleHotKeyImage;
    private TMP_Text _autoToggleHotKeyText;
    private Button _autoButton;

    private RectTransform _rapidSkipRoot;
    private Image _rapidSkipIconImage;
    private TMP_Text _rapidSkipIconText;
    private RectTransform _rapidSkipHotKeyRoot;
    private Image _rapidSkipHotKeyImage;
    private TMP_Text _rapidSkipHotKeyText;
    private Button _rapidSkipButton;

    private RectTransform _rollbackRoot;
    private Image _rollbackIconImage;
    private TMP_Text _rollbackIconText;
    private RectTransform _rollbackHotKeyRoot;
    private Image _rollbackHotKeyBg;
    private TMP_Text _rollbackHotKeyText;
    private Button _rollbackButton;

    private CanvasGroup _buttonsTopLeftGroup;

    private RectTransform _openSkipPanelRoot;
    private TMP_Text _openSkipPanelText;
    private RectTransform _openSkipPanelIconRoot;
    private Image _openSkipPanelIconImage;
    private RectTransform _openSkipPanelHotKeyRoot;
    private TMP_Text _openSkipPanelHotKeyText;
    private Button _openSkipPanelButton;

    private TMP_Text _saveMenuText;
    private Button _saveMenuButton;

    private TMP_Text _loadMenuText;
    private Button _loadMenuButton;

    #endregion

    private void CacheRefs()
    {
        CacheStageViewportRefs();
        CacheScreenEffectRefs();
        CacheDialogueUIRefs();
        CacheButtonLayerRefs();
    }

    private void ValidateRefs()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _stageViewportRefsValid = ValidateStageViewportRefs();
        _screenEffectRefsValid = ValidateScreenEffectRefs();
        _dialogueUIRefsValid = ValidateDialogueUIRefs();
        _buttonLayerRefsValid = ValidateButtonLayerRefs();
#else
        _stageViewportRefsValid = true;
        _screenEffectRefsValid = true;
        _dialogueUIRefsValid = true;
        _buttonLayerRefsValid = true;
#endif
    }

    #region StageViewport Layer

    private void CacheStageViewportRefs()
    {
        _stageViewportLayer = View.Rect(Refs.StageViewportLayer);

        _stageShotRoot = View.Rect(Refs.StageShot_Root);
        _stagePanRoot = View.Rect(Refs.StagePan_Root);
        _stageZoomRoot = View.Rect(Refs.StageZoom_Root);

        _stage00Root = View.Rect(Refs.Stage00_Root);

        _stage00DepthSystemRoot = View.Rect(Refs.Stage00DepthSystem_Root);
        _stage00DepthFarRoot = View.Rect(Refs.Stage00Depth_Far_Root);
        _stage00DepthFarFramingTransform = View.Rect(Refs.Stage00Depth_Far_FramingTransform);
        _stage00DepthFarFramingScale = View.Rect(Refs.Stage00Depth_Far_FramingScale);
        _stage00DepthFarContent = View.Rect(Refs.Stage00Depth_Far_Content);

        _stage00DepthBackRoot = View.Rect(Refs.Stage00Depth_Back_Root);
        _stage00DepthBackFramingTransform = View.Rect(Refs.Stage00Depth_Back_FramingTransform);
        _stage00DepthBackFramingScale = View.Rect(Refs.Stage00Depth_Back_FramingScale);
        _stage00DepthBackContent = View.Rect(Refs.Stage00Depth_Back_Content);

        _stage00DepthMidRoot = View.Rect(Refs.Stage00Depth_Mid_Root);
        _stage00DepthMidFramingTransform = View.Rect(Refs.Stage00Depth_Mid_FramingTransform);
        _stage00DepthMidFramingScale = View.Rect(Refs.Stage00Depth_Mid_FramingScale);
        _stage00DepthMidContent = View.Rect(Refs.Stage00Depth_Mid_Content);

        _stage00DepthFrontRoot = View.Rect(Refs.Stage00Depth_Front_Root);
        _stage00DepthFrontFramingTransform = View.Rect(Refs.Stage00Depth_Front_FramingTransform);
        _stage00DepthFrontFramingScale = View.Rect(Refs.Stage00Depth_Front_FramingScale);
        _stage00DepthFrontContent = View.Rect(Refs.Stage00Depth_Front_Content);

        _stage00DepthCloseRoot = View.Rect(Refs.Stage00Depth_Close_Root);
        _stage00DepthCloseFramingTransform = View.Rect(Refs.Stage00Depth_Close_FramingTransform);
        _stage00DepthCloseFramingScale = View.Rect(Refs.Stage00Depth_Close_FramingScale);
        _stage00DepthCloseContent = View.Rect(Refs.Stage00Depth_Close_Content);

        _stage01Root = View.Rect(Refs.Stage01_Root);

        _stage01DepthSystemRoot = View.Rect(Refs.Stage01DepthSystem_Root);
        _stage01DepthFarRoot = View.Rect(Refs.Stage01Depth_Far_Root);
        _stage01DepthFarFramingTransform = View.Rect(Refs.Stage01Depth_Far_FramingTransform);
        _stage01DepthFarFramingScale = View.Rect(Refs.Stage01Depth_Far_FramingScale);
        _stage01DepthFarContent = View.Rect(Refs.Stage01Depth_Far_Content);

        _stage01DepthBackRoot = View.Rect(Refs.Stage01Depth_Back_Root);
        _stage01DepthBackFramingTransform = View.Rect(Refs.Stage01Depth_Back_FramingTransform);
        _stage01DepthBackFramingScale = View.Rect(Refs.Stage01Depth_Back_FramingScale);
        _stage01DepthBackContent = View.Rect(Refs.Stage01Depth_Back_Content);

        _stage01DepthMidRoot = View.Rect(Refs.Stage01Depth_Mid_Root);
        _stage01DepthMidFramingTransform = View.Rect(Refs.Stage01Depth_Mid_FramingTransform);
        _stage01DepthMidFramingScale = View.Rect(Refs.Stage01Depth_Mid_FramingScale);
        _stage01DepthMidContent = View.Rect(Refs.Stage01Depth_Mid_Content);

        _stage01DepthFrontRoot = View.Rect(Refs.Stage01Depth_Front_Root);
        _stage01DepthFrontFramingTransform = View.Rect(Refs.Stage01Depth_Front_FramingTransform);
        _stage01DepthFrontFramingScale = View.Rect(Refs.Stage01Depth_Front_FramingScale);
        _stage01DepthFrontContent = View.Rect(Refs.Stage01Depth_Front_Content);

        _stage01DepthCloseRoot = View.Rect(Refs.Stage01Depth_Close_Root);
        _stage01DepthCloseFramingTransform = View.Rect(Refs.Stage01Depth_Close_FramingTransform);
        _stage01DepthCloseFramingScale = View.Rect(Refs.Stage01Depth_Close_FramingScale);
        _stage01DepthCloseContent = View.Rect(Refs.Stage01Depth_Close_Content);

        _stage02Root = View.Rect(Refs.Stage02_Root);

        _stage02DepthSystemRoot = View.Rect(Refs.Stage02DepthSystem_Root);
        _stage02DepthFarRoot = View.Rect(Refs.Stage02Depth_Far_Root);
        _stage02DepthFarFramingTransform = View.Rect(Refs.Stage02Depth_Far_FramingTransform);
        _stage02DepthFarFramingScale = View.Rect(Refs.Stage02Depth_Far_FramingScale);
        _stage02DepthFarContent = View.Rect(Refs.Stage02Depth_Far_Content);

        _stage02DepthBackRoot = View.Rect(Refs.Stage02Depth_Back_Root);
        _stage02DepthBackFramingTransform = View.Rect(Refs.Stage02Depth_Back_FramingTransform);
        _stage02DepthBackFramingScale = View.Rect(Refs.Stage02Depth_Back_FramingScale);
        _stage02DepthBackContent = View.Rect(Refs.Stage02Depth_Back_Content);

        _stage02DepthMidRoot = View.Rect(Refs.Stage02Depth_Mid_Root);
        _stage02DepthMidFramingTransform = View.Rect(Refs.Stage02Depth_Mid_FramingTransform);
        _stage02DepthMidFramingScale = View.Rect(Refs.Stage02Depth_Mid_FramingScale);
        _stage02DepthMidContent = View.Rect(Refs.Stage02Depth_Mid_Content);

        _stage02DepthFrontRoot = View.Rect(Refs.Stage02Depth_Front_Root);
        _stage02DepthFrontFramingTransform = View.Rect(Refs.Stage02Depth_Front_FramingTransform);
        _stage02DepthFrontFramingScale = View.Rect(Refs.Stage02Depth_Front_FramingScale);
        _stage02DepthFrontContent = View.Rect(Refs.Stage02Depth_Front_Content);

        _stage02DepthCloseRoot = View.Rect(Refs.Stage02Depth_Close_Root);
        _stage02DepthCloseFramingTransform = View.Rect(Refs.Stage02Depth_Close_FramingTransform);
        _stage02DepthCloseFramingScale = View.Rect(Refs.Stage02Depth_Close_FramingScale);
        _stage02DepthCloseContent = View.Rect(Refs.Stage02Depth_Close_Content);
    }

    private bool ValidateStageViewportRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _stageViewportLayer, Refs.StageViewportLayer);

        AppendMissing(ref missing, _stageShotRoot, Refs.StageShot_Root);
        AppendMissing(ref missing, _stagePanRoot, Refs.StagePan_Root);
        AppendMissing(ref missing, _stageZoomRoot, Refs.StageZoom_Root);

        AppendMissing(ref missing, _stage00Root, Refs.Stage00_Root);
        AppendMissing(ref missing, _stage00DepthSystemRoot, Refs.Stage00DepthSystem_Root);
        AppendMissing(ref missing, _stage00DepthFarRoot, Refs.Stage00Depth_Far_Root);
        AppendMissing(ref missing, _stage00DepthFarFramingTransform, Refs.Stage00Depth_Far_FramingTransform);
        AppendMissing(ref missing, _stage00DepthFarFramingScale, Refs.Stage00Depth_Far_FramingScale);
        AppendMissing(ref missing, _stage00DepthFarContent, Refs.Stage00Depth_Far_Content);

        AppendMissing(ref missing, _stage00DepthBackRoot, Refs.Stage00Depth_Back_Root);
        AppendMissing(ref missing, _stage00DepthBackFramingTransform, Refs.Stage00Depth_Back_FramingTransform);
        AppendMissing(ref missing, _stage00DepthBackFramingScale, Refs.Stage00Depth_Back_FramingScale);
        AppendMissing(ref missing, _stage00DepthBackContent, Refs.Stage00Depth_Back_Content);

        AppendMissing(ref missing, _stage00DepthMidRoot, Refs.Stage00Depth_Mid_Root);
        AppendMissing(ref missing, _stage00DepthMidFramingTransform, Refs.Stage00Depth_Mid_FramingTransform);
        AppendMissing(ref missing, _stage00DepthMidFramingScale, Refs.Stage00Depth_Mid_FramingScale);
        AppendMissing(ref missing, _stage00DepthMidContent, Refs.Stage00Depth_Mid_Content);

        AppendMissing(ref missing, _stage00DepthFrontRoot, Refs.Stage00Depth_Front_Root);
        AppendMissing(ref missing, _stage00DepthFrontFramingTransform, Refs.Stage00Depth_Front_FramingTransform);
        AppendMissing(ref missing, _stage00DepthFrontFramingScale, Refs.Stage00Depth_Front_FramingScale);
        AppendMissing(ref missing, _stage00DepthFrontContent, Refs.Stage00Depth_Front_Content);

        AppendMissing(ref missing, _stage00DepthCloseRoot, Refs.Stage00Depth_Close_Root);
        AppendMissing(ref missing, _stage00DepthCloseFramingTransform, Refs.Stage00Depth_Close_FramingTransform);
        AppendMissing(ref missing, _stage00DepthCloseFramingScale, Refs.Stage00Depth_Close_FramingScale);
        AppendMissing(ref missing, _stage00DepthCloseContent, Refs.Stage00Depth_Close_Content);

        AppendMissing(ref missing, _stage01Root, Refs.Stage01_Root);
        AppendMissing(ref missing, _stage01DepthSystemRoot, Refs.Stage01DepthSystem_Root);
        AppendMissing(ref missing, _stage01DepthFarRoot, Refs.Stage01Depth_Far_Root);
        AppendMissing(ref missing, _stage01DepthFarFramingTransform, Refs.Stage01Depth_Far_FramingTransform);
        AppendMissing(ref missing, _stage01DepthFarFramingScale, Refs.Stage01Depth_Far_FramingScale);
        AppendMissing(ref missing, _stage01DepthFarContent, Refs.Stage01Depth_Far_Content);

        AppendMissing(ref missing, _stage01DepthBackRoot, Refs.Stage01Depth_Back_Root);
        AppendMissing(ref missing, _stage01DepthBackFramingTransform, Refs.Stage01Depth_Back_FramingTransform);
        AppendMissing(ref missing, _stage01DepthBackFramingScale, Refs.Stage01Depth_Back_FramingScale);
        AppendMissing(ref missing, _stage01DepthBackContent, Refs.Stage01Depth_Back_Content);

        AppendMissing(ref missing, _stage01DepthMidRoot, Refs.Stage01Depth_Mid_Root);
        AppendMissing(ref missing, _stage01DepthMidFramingTransform, Refs.Stage01Depth_Mid_FramingTransform);
        AppendMissing(ref missing, _stage01DepthMidFramingScale, Refs.Stage01Depth_Mid_FramingScale);
        AppendMissing(ref missing, _stage01DepthMidContent, Refs.Stage01Depth_Mid_Content);

        AppendMissing(ref missing, _stage01DepthFrontRoot, Refs.Stage01Depth_Front_Root);
        AppendMissing(ref missing, _stage01DepthFrontFramingTransform, Refs.Stage01Depth_Front_FramingTransform);
        AppendMissing(ref missing, _stage01DepthFrontFramingScale, Refs.Stage01Depth_Front_FramingScale);
        AppendMissing(ref missing, _stage01DepthFrontContent, Refs.Stage01Depth_Front_Content);

        AppendMissing(ref missing, _stage01DepthCloseRoot, Refs.Stage01Depth_Close_Root);
        AppendMissing(ref missing, _stage01DepthCloseFramingTransform, Refs.Stage01Depth_Close_FramingTransform);
        AppendMissing(ref missing, _stage01DepthCloseFramingScale, Refs.Stage01Depth_Close_FramingScale);
        AppendMissing(ref missing, _stage01DepthCloseContent, Refs.Stage01Depth_Close_Content);

        AppendMissing(ref missing, _stage02Root, Refs.Stage02_Root);
        AppendMissing(ref missing, _stage02DepthSystemRoot, Refs.Stage02DepthSystem_Root);
        AppendMissing(ref missing, _stage02DepthFarRoot, Refs.Stage02Depth_Far_Root);
        AppendMissing(ref missing, _stage02DepthFarFramingTransform, Refs.Stage02Depth_Far_FramingTransform);
        AppendMissing(ref missing, _stage02DepthFarFramingScale, Refs.Stage02Depth_Far_FramingScale);
        AppendMissing(ref missing, _stage02DepthFarContent, Refs.Stage02Depth_Far_Content);

        AppendMissing(ref missing, _stage02DepthBackRoot, Refs.Stage02Depth_Back_Root);
        AppendMissing(ref missing, _stage02DepthBackFramingTransform, Refs.Stage02Depth_Back_FramingTransform);
        AppendMissing(ref missing, _stage02DepthBackFramingScale, Refs.Stage02Depth_Back_FramingScale);
        AppendMissing(ref missing, _stage02DepthBackContent, Refs.Stage02Depth_Back_Content);

        AppendMissing(ref missing, _stage02DepthMidRoot, Refs.Stage02Depth_Mid_Root);
        AppendMissing(ref missing, _stage02DepthMidFramingTransform, Refs.Stage02Depth_Mid_FramingTransform);
        AppendMissing(ref missing, _stage02DepthMidFramingScale, Refs.Stage02Depth_Mid_FramingScale);
        AppendMissing(ref missing, _stage02DepthMidContent, Refs.Stage02Depth_Mid_Content);

        AppendMissing(ref missing, _stage02DepthFrontRoot, Refs.Stage02Depth_Front_Root);
        AppendMissing(ref missing, _stage02DepthFrontFramingTransform, Refs.Stage02Depth_Front_FramingTransform);
        AppendMissing(ref missing, _stage02DepthFrontFramingScale, Refs.Stage02Depth_Front_FramingScale);
        AppendMissing(ref missing, _stage02DepthFrontContent, Refs.Stage02Depth_Front_Content);

        AppendMissing(ref missing, _stage02DepthCloseRoot, Refs.Stage02Depth_Close_Root);
        AppendMissing(ref missing, _stage02DepthCloseFramingTransform, Refs.Stage02Depth_Close_FramingTransform);
        AppendMissing(ref missing, _stage02DepthCloseFramingScale, Refs.Stage02Depth_Close_FramingScale);
        AppendMissing(ref missing, _stage02DepthCloseContent, Refs.Stage02Depth_Close_Content);

        return LogMissingRefs("StageViewport Layer", missing);
    }

    #endregion

    #region ScreenEffect Layer

    private void CacheScreenEffectRefs()
    {
        _screenEffectLayer = View.Rect(Refs.ScreenEffectLayer);
    }

    private bool ValidateScreenEffectRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _screenEffectLayer, Refs.ScreenEffectLayer);

        return LogMissingRefs("ScreenEffect Layer", missing);
    }

    #endregion

    #region DialogueUI Layer

    private void CacheDialogueUIRefs()
    {
        _dialogueUILayer = View.Rect(Refs.DialogueUILayer);
        _dialogueBoxRoot = View.Rect(Refs.DialogueBox_Root);
        _optionsBoxRoot = View.Rect(Refs.OptionsBox_Root);
    }

    private bool ValidateDialogueUIRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _dialogueUILayer, Refs.DialogueUILayer);
        AppendMissing(ref missing, _dialogueBoxRoot, Refs.DialogueBox_Root);
        AppendMissing(ref missing, _optionsBoxRoot, Refs.OptionsBox_Root);

        return LogMissingRefs("DialogueUI Layer", missing);
    }

    #endregion

    #region Button Layer

    private void CacheButtonLayerRefs()
    {
        _buttonLayer = View.Rect(Refs.ButtonLayer);

        _stepNextButton = View.Button(Refs.StepNext_Button);

        _buttonsBottomRightGroup = View.CanvasGroup(Refs.Buttons_BottomRight);

        _stepNextRoot = View.Rect(Refs.StepNext_Root);
        _stepNextText = View.Text(Refs.StepNext_Text);
        _stepNextHotKeyRoot = View.Rect(Refs.StepNextHotKey_Root);
        _stepNextHotKeyImage = View.Image(Refs.StepNextHotKey_Image);
        _stepNextHotKeyText = View.Text(Refs.StepNextHotKey_Text);

        _buttonsTopRightGroup = View.CanvasGroup(Refs.Buttons_TopRight);

        _quickMenuToggleRoot = View.Rect(Refs.QuickMenuToggle_Root);
        _quickMenuToggleImage = View.Image(Refs.QuickMenuToggle_Image);
        _quickMenuToggleKeyText = View.Text(Refs.QuickMenuToggleKey_Text);
        _quickMenuToggleButton = View.Button(Refs.QuickMenuToggle_Button);

        _quickMenuRootGroup = View.CanvasGroup(Refs.QuickMenu_Root);
        _quickMenuBgImage = View.Image(Refs.QuickMenuBG_Image);

        _expandToggleRoot = View.Rect(Refs.ExpandToggle_Root);
        _expandToggleImage = View.Image(Refs.ExpandToggle_Image);
        _expandToggleText = View.Text(Refs.ExpandToggle_Text);
        _expandToggleHotkeyText = View.Text(Refs.ExpandToggleHotkey_Text);
        _expandButton = View.Button(Refs.ExpandToggle_Button);

        _backLogRoot = View.Rect(Refs.BackLog_Root);
        _backLogImage = View.Image(Refs.BackLog_Image);
        _backLogText = View.Text(Refs.BackLog_Text);
        _backLogHotkeyText = View.Text(Refs.BackLogHotkey_Text);
        _backLogButton = View.Button(Refs.BackLog_Button);

        _playbackSpeedToggleRoot = View.Rect(Refs.PlaybackSpeedToggle_Root);
        _playbackSpeedToggleImage = View.Image(Refs.PlaybackSpeedToggle_Image);
        _playbackSpeedToggleText = View.Text(Refs.PlaybackSpeedToggle_Text);
        _playbackSpeedToggleDegreeText = View.Text(Refs.PlaybackSpeedToggleDegree_Text);
        _playbackSpeedToggleHotKeyText = View.Text(Refs.PlaybackSpeedToggleHotKey_Text);
        _playbackSpeedButton = View.Button(Refs.PlaybackSpeedToggle_Button);

        _autoToggleRoot = View.Rect(Refs.AutoToggle_Root);
        _autoToggleIconImage = View.Image(Refs.AutoToggleIcon_Image);
        _autoToggleHotKeyRoot = View.Rect(Refs.AutoToggleHotKey_Root);
        _autoToggleHotKeyImage = View.Image(Refs.AutoToggleHotKey_Image);
        _autoToggleHotKeyText = View.Text(Refs.AutoToggleHotKey_Text);
        _autoButton = View.Button(Refs.AutoToggle_Button);

        _rapidSkipRoot = View.Rect(Refs.RapidSkip_Root);
        _rapidSkipIconImage = View.Image(Refs.RapidSkipIcon_Image);
        _rapidSkipIconText = View.Text(Refs.RapidSkipIcon_Text);
        _rapidSkipHotKeyRoot = View.Rect(Refs.RapidSkipHotKey_Root);
        _rapidSkipHotKeyImage = View.Image(Refs.RapidSkipHotKey_Image);
        _rapidSkipHotKeyText = View.Text(Refs.RapidSkipHotKey_Text);
        _rapidSkipButton = View.Button(Refs.RapidSkip_Button);

        _rollbackRoot = View.Rect(Refs.Rollback_Root);
        _rollbackIconImage = View.Image(Refs.RollbackIcon_Image);
        _rollbackIconText = View.Text(Refs.RollbackIcon_Text);
        _rollbackHotKeyRoot = View.Rect(Refs.RollbackHotKey_Root);
        _rollbackHotKeyBg = View.Image(Refs.RollbackHotKeyBG);
        _rollbackHotKeyText = View.Text(Refs.RollbackHotKey_Text);
        _rollbackButton = View.Button(Refs.Rollback_Button);

        _buttonsTopLeftGroup = View.CanvasGroup(Refs.Buttons_TopLeft);

        _openSkipPanelRoot = View.Rect(Refs.OpenSkipPanel_Root);
        _openSkipPanelText = View.Text(Refs.OpenSkipPanel_Text);
        _openSkipPanelIconRoot = View.Rect(Refs.OpenSkipPanelIcon_Root);
        _openSkipPanelIconImage = View.Image(Refs.OpenSkipPanelIcon_Image);
        _openSkipPanelHotKeyRoot = View.Rect(Refs.OpenSkipPanelHotKey_Root);
        _openSkipPanelHotKeyText = View.Text(Refs.OpenSkipPanelHotKey_Text);
        _openSkipPanelButton = View.Button(Refs.OpenSkipPanel_Button);

        _saveMenuText = View.Text(Refs.SaveMenu_Text);
        _saveMenuButton = View.Button(Refs.SaveMenu_Button);

        _loadMenuText = View.Text(Refs.LoadMenu_Text);
        _loadMenuButton = View.Button(Refs.LoadMenu_Button);
    }

    private bool ValidateButtonLayerRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _buttonLayer, Refs.ButtonLayer);

        AppendMissing(ref missing, _stepNextButton, Refs.StepNext_Button);

        AppendMissing(ref missing, _buttonsBottomRightGroup, Refs.Buttons_BottomRight);

        AppendMissing(ref missing, _stepNextRoot, Refs.StepNext_Root);
        AppendMissing(ref missing, _stepNextText, Refs.StepNext_Text);
        AppendMissing(ref missing, _stepNextHotKeyRoot, Refs.StepNextHotKey_Root);
        AppendMissing(ref missing, _stepNextHotKeyImage, Refs.StepNextHotKey_Image);
        AppendMissing(ref missing, _stepNextHotKeyText, Refs.StepNextHotKey_Text);

        AppendMissing(ref missing, _buttonsTopRightGroup, Refs.Buttons_TopRight);

        AppendMissing(ref missing, _quickMenuToggleRoot, Refs.QuickMenuToggle_Root);
        AppendMissing(ref missing, _quickMenuToggleImage, Refs.QuickMenuToggle_Image);
        AppendMissing(ref missing, _quickMenuToggleKeyText, Refs.QuickMenuToggleKey_Text);
        AppendMissing(ref missing, _quickMenuToggleButton, Refs.QuickMenuToggle_Button);

        AppendMissing(ref missing, _quickMenuRootGroup, Refs.QuickMenu_Root);
        AppendMissing(ref missing, _quickMenuBgImage, Refs.QuickMenuBG_Image);

        AppendMissing(ref missing, _expandToggleRoot, Refs.ExpandToggle_Root);
        AppendMissing(ref missing, _expandToggleImage, Refs.ExpandToggle_Image);
        AppendMissing(ref missing, _expandToggleText, Refs.ExpandToggle_Text);
        AppendMissing(ref missing, _expandToggleHotkeyText, Refs.ExpandToggleHotkey_Text);
        AppendMissing(ref missing, _expandButton, Refs.ExpandToggle_Button);

        AppendMissing(ref missing, _backLogRoot, Refs.BackLog_Root);
        AppendMissing(ref missing, _backLogImage, Refs.BackLog_Image);
        AppendMissing(ref missing, _backLogText, Refs.BackLog_Text);
        AppendMissing(ref missing, _backLogHotkeyText, Refs.BackLogHotkey_Text);
        AppendMissing(ref missing, _backLogButton, Refs.BackLog_Button);

        AppendMissing(ref missing, _playbackSpeedToggleRoot, Refs.PlaybackSpeedToggle_Root);
        AppendMissing(ref missing, _playbackSpeedToggleImage, Refs.PlaybackSpeedToggle_Image);
        AppendMissing(ref missing, _playbackSpeedToggleText, Refs.PlaybackSpeedToggle_Text);
        AppendMissing(ref missing, _playbackSpeedToggleDegreeText, Refs.PlaybackSpeedToggleDegree_Text);
        AppendMissing(ref missing, _playbackSpeedToggleHotKeyText, Refs.PlaybackSpeedToggleHotKey_Text);
        AppendMissing(ref missing, _playbackSpeedButton, Refs.PlaybackSpeedToggle_Button);

        AppendMissing(ref missing, _autoToggleRoot, Refs.AutoToggle_Root);
        AppendMissing(ref missing, _autoToggleIconImage, Refs.AutoToggleIcon_Image);
        AppendMissing(ref missing, _autoToggleHotKeyRoot, Refs.AutoToggleHotKey_Root);
        AppendMissing(ref missing, _autoToggleHotKeyImage, Refs.AutoToggleHotKey_Image);
        AppendMissing(ref missing, _autoToggleHotKeyText, Refs.AutoToggleHotKey_Text);
        AppendMissing(ref missing, _autoButton, Refs.AutoToggle_Button);

        AppendMissing(ref missing, _rapidSkipRoot, Refs.RapidSkip_Root);
        AppendMissing(ref missing, _rapidSkipIconImage, Refs.RapidSkipIcon_Image);
        AppendMissing(ref missing, _rapidSkipIconText, Refs.RapidSkipIcon_Text);
        AppendMissing(ref missing, _rapidSkipHotKeyRoot, Refs.RapidSkipHotKey_Root);
        AppendMissing(ref missing, _rapidSkipHotKeyImage, Refs.RapidSkipHotKey_Image);
        AppendMissing(ref missing, _rapidSkipHotKeyText, Refs.RapidSkipHotKey_Text);
        AppendMissing(ref missing, _rapidSkipButton, Refs.RapidSkip_Button);

        AppendMissing(ref missing, _rollbackRoot, Refs.Rollback_Root);
        AppendMissing(ref missing, _rollbackIconImage, Refs.RollbackIcon_Image);
        AppendMissing(ref missing, _rollbackIconText, Refs.RollbackIcon_Text);
        AppendMissing(ref missing, _rollbackHotKeyRoot, Refs.RollbackHotKey_Root);
        AppendMissing(ref missing, _rollbackHotKeyBg, Refs.RollbackHotKeyBG);
        AppendMissing(ref missing, _rollbackHotKeyText, Refs.RollbackHotKey_Text);
        AppendMissing(ref missing, _rollbackButton, Refs.Rollback_Button);

        AppendMissing(ref missing, _buttonsTopLeftGroup, Refs.Buttons_TopLeft);

        AppendMissing(ref missing, _openSkipPanelRoot, Refs.OpenSkipPanel_Root);
        AppendMissing(ref missing, _openSkipPanelText, Refs.OpenSkipPanel_Text);
        AppendMissing(ref missing, _openSkipPanelIconRoot, Refs.OpenSkipPanelIcon_Root);
        AppendMissing(ref missing, _openSkipPanelIconImage, Refs.OpenSkipPanelIcon_Image);
        AppendMissing(ref missing, _openSkipPanelHotKeyRoot, Refs.OpenSkipPanelHotKey_Root);
        AppendMissing(ref missing, _openSkipPanelHotKeyText, Refs.OpenSkipPanelHotKey_Text);
        AppendMissing(ref missing, _openSkipPanelButton, Refs.OpenSkipPanel_Button);

        AppendMissing(ref missing, _saveMenuText, Refs.SaveMenu_Text);
        AppendMissing(ref missing, _saveMenuButton, Refs.SaveMenu_Button);

        AppendMissing(ref missing, _loadMenuText, Refs.LoadMenu_Text);
        AppendMissing(ref missing, _loadMenuButton, Refs.LoadMenu_Button);

        return LogMissingRefs("Button Layer", missing);
    }

    #endregion

    private bool LogMissingRefs(string regionName, string missing)
    {
        if (missing.Length <= 0)
            return true;

        Debug.LogWarning($"[PresentationUIRoot] Missing refs in {regionName}:\n{missing}", this);
        return false;
    }
}