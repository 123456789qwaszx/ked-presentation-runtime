using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

// 현재 타이틀 화면은 배경과 로고만 그린다.
// 버튼 배선은 ButtonWidget이 사라지면서 함께 없어졌다 — Refs의 *Button_BWidget 항목은
// 프리팹에 남아 있는 자리이고, 버튼을 되살리려면 위젯 타입부터 다시 만들어야 한다.
public sealed class TitleUIRoot : UIRoot<TitleUIRoot.Refs>
{
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

    #endregion

    protected override void OnInitialize()
    {
        _titleBg   = View.Image(Refs.TitleBG_Image);
        _titleLogo = View.Image(Refs.TitleLogo_Image);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ValidateRefs();
#endif
    }

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _titleBg,   Refs.TitleBG_Image);
        AppendMissing(ref missing, _titleLogo, Refs.TitleLogo_Image);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[TitleUIRoot] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}