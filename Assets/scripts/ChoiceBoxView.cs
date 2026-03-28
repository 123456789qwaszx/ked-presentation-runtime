using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChoiceBoxView : UIBase<ChoiceBoxView.Refs>
{
    public event Action<int> OnClicked;

    public enum Refs
    {
        ChoiceBox_Root,
        ChoiceBox_Anchor,
        ChoiceBox_Pad,
        ChoiceBox_Image,

        ChoiceBoxTextArea00_Root,
        ChoiceBoxTextArea00_Anchor,
        ChoiceBoxTextArea00_Text,

        ChoiceBoxTextArea01_Root,
        ChoiceBoxTextArea01_Anchor,
        ChoiceBoxTextArea01_Text,

        // 버튼을 Refs로 잡으려면 여기 추가 필요:
        ChoiceBoxHit_Button
    }

    private TMP_Text _labelText;
    private Button _button;
    private CanvasGroup _rootCg;

    private int _index;

    protected override void Initialize()
    {
        _rootCg   = View.CanvasGroup(Refs.ChoiceBox_Root);
        _labelText = View.Text(Refs.ChoiceBoxTextArea00_Text);

        // ⚠️ 여기서 Button을 Refs로 가져오려면,
        // 프리팹에서 Button이 붙어있는 오브젝트를 View가 찾을 수 있게
        // enum에 ChoiceBox_Button 같은 키를 추가하고 그 오브젝트에 붙여줘야 해.
        //
        // 우선 “ChoiceBox_Root에 Button이 붙어있다”는 가정이면 아래처럼:
        _button = View.Button(Refs.ChoiceBoxHit_Button);

        // 템플릿은 기본 숨김(ChoicePanel에서 템플릿 비활성도 함)
        // if (_rootCg != null)
        // {
        //     _rootCg.alpha = 0f;
        //     _rootCg.interactable = false;
        //     _rootCg.blocksRaycasts = false;
        // }
    }

    public void Present(int index, string label)
    {
        _index = index;

        // 보이게
        if (_rootCg != null)
        {
            _rootCg.alpha = 1f;
            _rootCg.interactable = true;
            _rootCg.blocksRaycasts = true;
        }

        if (_labelText != null)
            _labelText.text = label ?? "";

        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleClick);
            _button.onClick.AddListener(HandleClick);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_button != null)
            _button.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick()
    {
        OnClicked?.Invoke(_index);
    }
}
