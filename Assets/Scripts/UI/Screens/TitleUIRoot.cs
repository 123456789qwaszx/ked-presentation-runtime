using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UIRefValidation;

public sealed class TitleUIRoot : UIRoot<TitleUIRoot.Refs>
{
    public event Action ContinueClicked;
    public event Action StartClicked;
    public event Action LoadClicked;
    public event Action QuitClicked;
    public event Action SwitchExampleClicked;

    #region Refs

    public enum Refs
    {
        TitleBG_Image,
        TitleLogo_Image,

        ContinueButton_Button,
        StartButton_Button,
        LoadButton_Button,
        QuitButton_Button,

        // 예제 전환(framework-scope 4단계). 씬에 아직 없으면 경고만 하고 나머지 타이틀은 그대로 돈다.
        ExampleLabel_Text,
        SwitchExampleButton_Button,
    }

    private Image _titleBg;
    private Image _titleLogo;

    private Button _continueButton;
    private Button _startButton;
    private Button _loadButton;
    private Button _quitButton;

    private TMP_Text _exampleLabel;
    private Button _switchExampleButton;

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
        _quitButton     = View.Button(Refs.QuitButton_Button);

        _exampleLabel        = View.Text(Refs.ExampleLabel_Text);
        _switchExampleButton = View.Button(Refs.SwitchExampleButton_Button);
    }

    private void BindHandlers()
    {
        BindEvent(_continueButton, PressContinueButton);
        BindEvent(_startButton, PressStartButton);
        BindEvent(_loadButton, PressLoadButton);
        BindEvent(_quitButton, PressQuitButton);
        BindEvent(_switchExampleButton, PressSwitchExampleButton);
    }

    // 저장 슬롯이 없는 예제(연출 단독)는 불러오기를 숨긴다.
    public void SetLoadVisible(bool visible)
    {
        View.Button(Refs.LoadButton_Button).gameObject.SetActive(visible);
    }

    // 지금 예제와, 전환 버튼이 데려갈 예제. 버튼의 글씨는 자식 TMP_Text가 있으면 고친다.
    public void SetExample(string exampleName, string otherExampleName)
    {
        if (_exampleLabel)
            _exampleLabel.text = string.IsNullOrEmpty(exampleName) ? "" : $"예제: {exampleName}";

        if (!_switchExampleButton)
            return;

        _switchExampleButton.gameObject.SetActive(!string.IsNullOrEmpty(otherExampleName));

        TMP_Text label = _switchExampleButton.GetComponentInChildren<TMP_Text>(true);
        if (label && !string.IsNullOrEmpty(otherExampleName))
            label.text = $"{otherExampleName} 예제로";
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

    private void PressQuitButton(PointerEventData _)
    {
        QuitClicked?.Invoke();
    }

    private void PressSwitchExampleButton(PointerEventData _)
    {
        SwitchExampleClicked?.Invoke();
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
        AppendMissing(ref missing, _quitButton,     Refs.QuitButton_Button);

        AppendMissing(ref missing, _exampleLabel,        Refs.ExampleLabel_Text);
        AppendMissing(ref missing, _switchExampleButton, Refs.SwitchExampleButton_Button);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[TitleUIRoot] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}