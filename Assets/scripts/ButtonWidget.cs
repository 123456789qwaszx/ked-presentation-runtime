using System;
using TMPro;
using UnityEngine.UI;

public sealed class ButtonWidget : UIBase<ButtonWidget.Refs>
{
    public event Action OnClicked;

    public enum Refs
    {
        BWidgetBG_Image,    // 패치 대상
        BWidgetIcon_Image,  // 패치 대상
        BWidgetLabel_Text,

        BWidgetHit_Button
    }

    private TMP_Text _label;
    private Button _hit;

    protected override void Initialize()
    {
        _label = View.Text(Refs.BWidgetLabel_Text);
        _hit   = View.Button(Refs.BWidgetHit_Button);

        BindEvent(_hit, _ => OnClicked?.Invoke(), ETouchEvent.Click);
    }

    public void SetLabel(string text)
    {
        if (_label) _label.text = text ?? "";
    }


    public void SetInteractable(bool interactable)
    {
        if (_hit) _hit.interactable = interactable;
    }
}