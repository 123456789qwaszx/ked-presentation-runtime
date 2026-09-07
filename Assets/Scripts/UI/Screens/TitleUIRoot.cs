using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UIRefValidation;

public sealed class TitleUIRoot : UIRoot<TitleUIRoot.Refs>
{
    public event Action ContinueClicked;
    public event Action StartClicked;
    public event Action LoadClicked;
    public event Action AlbumClicked;
    public event Action SettingsClicked;
    public event Action QuitClicked;

    #region Refs

    public enum Refs
    {
        TitleBG_Image,
        TitleLogo_Image,

        ContinueButton_Button,
        StartButton_Button,
        LoadButton_Button,
        AlbumButton_Button,
        SettingsButton_Button,
        QuitButton_Button,
    }

    private Image _titleBg;
    private Image _titleLogo;

    private Button _continueButton;
    private Button _startButton;
    private Button _loadButton;
    private Button _albumButton;
    private Button _settingsButton;
    private Button _quitButton;

    #endregion

    protected override void OnInitialize()
    {
        CacheRefs();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ValidateRefs();
#endif

        BindHandlers();
    }

    private void CacheRefs()
    {
        _titleBg   = View.Image(Refs.TitleBG_Image);
        _titleLogo = View.Image(Refs.TitleLogo_Image);

        _continueButton = View.Button(Refs.ContinueButton_Button);
        _startButton    = View.Button(Refs.StartButton_Button);
        _loadButton     = View.Button(Refs.LoadButton_Button);
        _albumButton    = View.Button(Refs.AlbumButton_Button);
        _settingsButton = View.Button(Refs.SettingsButton_Button);
        _quitButton     = View.Button(Refs.QuitButton_Button);
    }

    private void BindHandlers()
    {
        BindEvent(_continueButton, PressContinueButton);
        BindEvent(_startButton, PressStartButton);
        BindEvent(_loadButton, PressLoadButton);
        BindEvent(_albumButton, PressAlbumButton);
        BindEvent(_settingsButton, PressSettingsButton);
        BindEvent(_quitButton, PressQuitButton);
    }

    #region Handlers

    private void PressContinueButton(PointerEventData _)
    {
        ContinueClicked?.Invoke();
    }

    private void PressStartButton(PointerEventData _)
    {
        StartClicked?.Invoke();
    }

    private void PressLoadButton(PointerEventData _)
    {
        LoadClicked?.Invoke();
    }

    private void PressAlbumButton(PointerEventData _)
    {
        AlbumClicked?.Invoke();
    }

    private void PressSettingsButton(PointerEventData _)
    {
        SettingsClicked?.Invoke();
    }

    private void PressQuitButton(PointerEventData _)
    {
        QuitClicked?.Invoke();
    }

    #endregion

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _titleBg,   Refs.TitleBG_Image);
        AppendMissing(ref missing, _titleLogo, Refs.TitleLogo_Image);

        AppendMissing(ref missing, _continueButton, Refs.ContinueButton_Button);
        AppendMissing(ref missing, _startButton,    Refs.StartButton_Button);
        AppendMissing(ref missing, _loadButton,     Refs.LoadButton_Button);
        AppendMissing(ref missing, _albumButton,    Refs.AlbumButton_Button);
        AppendMissing(ref missing, _settingsButton, Refs.SettingsButton_Button);
        AppendMissing(ref missing, _quitButton,     Refs.QuitButton_Button);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[TitleUIRoot] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}