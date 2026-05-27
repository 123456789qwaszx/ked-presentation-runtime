using System;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

public sealed class TitleUIRoot : UIRoot<TitleUIRoot.Refs>
{
    public event Action StartClicked;
    public event Action ContinueClicked;
    public event Action LoadClicked;
    public event Action AlbumClicked;
    public event Action SettingsClicked;
    public event Action QuitClicked;

    #region Refs

    public enum Refs
    {
        TitleBG_Image,
        TitleLogo_Image,

        StartButton_BWidget,
        ContinueButton_BWidget,
        LoadButton_BWidget,
        AlbumButton_BWidget,
        SettingsButton_BWidget,
        QuitButton_BWidget,
    }

    private Image _titleBg;
    private Image _titleLogo;

    private ButtonWidget _start;
    private ButtonWidget _continue;
    private ButtonWidget _load;
    private ButtonWidget _album;
    private ButtonWidget _settings;
    private ButtonWidget _quit;

    #endregion

    private bool _valid;

    protected override void OnInitialize()
    {
        _titleBg   = View.Image(Refs.TitleBG_Image);
        _titleLogo = View.Image(Refs.TitleLogo_Image);

        _start    = View.Widget<ButtonWidget>(Refs.StartButton_BWidget);
        _continue = View.Widget<ButtonWidget>(Refs.ContinueButton_BWidget);
        _load     = View.Widget<ButtonWidget>(Refs.LoadButton_BWidget);
        _album    = View.Widget<ButtonWidget>(Refs.AlbumButton_BWidget);
        _settings = View.Widget<ButtonWidget>(Refs.SettingsButton_BWidget);
        _quit     = View.Widget<ButtonWidget>(Refs.QuitButton_BWidget);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif

        _start.SetLabel("시작하기");
        _continue.SetLabel("이어하기");
        _load.SetLabel("불러오기");
        _album.SetLabel("앨범");
        _settings.SetLabel("설정");
        _quit.SetLabel("종료");

        _start.OnClicked += HandleStart;
        _continue.OnClicked += HandleContinue;
        _load.OnClicked += HandleLoad;
        _album.OnClicked += HandleAlbum;
        _settings.OnClicked += HandleSettings;
        _quit.OnClicked += HandleQuit;
    }

    #region Event Handlers

    private void HandleStart()    => StartClicked?.Invoke();
    private void HandleContinue() => ContinueClicked?.Invoke();
    private void HandleLoad()     => LoadClicked?.Invoke();
    private void HandleAlbum()    => AlbumClicked?.Invoke();
    private void HandleSettings() => SettingsClicked?.Invoke();
    private void HandleQuit()     => QuitClicked?.Invoke();

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (!_valid) return;

        _start.OnClicked -= HandleStart;
        _continue.OnClicked -= HandleContinue;
        _load.OnClicked -= HandleLoad;
        _album.OnClicked -= HandleAlbum;
        _settings.OnClicked -= HandleSettings;
        _quit.OnClicked -= HandleQuit;
    }

    #endregion

    public void SetContinueEnabled(bool enabled)
    {
        if (!_valid) return;

        if (_continue != null)
            _continue.SetInteractable(enabled);
    }

    public void SetLoadEnabled(bool enabled)
    {
        if (!_valid) return;

        if (_load != null)
            _load.SetInteractable(enabled);
    }

    public void SetAlbumEnabled(bool enabled)
    {
        if (!_valid) return;

        if (_album != null)
            _album.SetInteractable(enabled);
    }

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _titleBg,   Refs.TitleBG_Image);
        AppendMissing(ref missing, _titleLogo, Refs.TitleLogo_Image);

        AppendMissing(ref missing, _start,    Refs.StartButton_BWidget);
        AppendMissing(ref missing, _continue, Refs.ContinueButton_BWidget);
        AppendMissing(ref missing, _load,     Refs.LoadButton_BWidget);
        AppendMissing(ref missing, _album,    Refs.AlbumButton_BWidget);
        AppendMissing(ref missing, _settings, Refs.SettingsButton_BWidget);
        AppendMissing(ref missing, _quit,     Refs.QuitButton_BWidget);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[TitleUIRoot] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}