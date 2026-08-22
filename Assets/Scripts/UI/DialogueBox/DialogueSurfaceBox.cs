using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DialogueSurfaceBox
    : PresentationDialogueBoxViewBase<DialogueSurfaceBox.Refs>
{
    public enum Refs
    {
        SurfaceBox_Root,

        // 접미사가 아니라 접두사로 'Image'를 씀..
        // UIBase의 스프라이트 포트 수집은
        // "_Image로 끝나는 Refs"를 자동으로 테마 패처(ui/{theme}/{portId})가 덮어쓰기 때문.
        // 레이아웃 프리셋(= 박스 종류)이 정하므로 그 수집에서 빠져 있어야 함.
        ImageSurfaceBox,

        SurfaceBoxLine_Text,
        SurfaceBoxName_Text,
    }

    public override RectTransform Root
        => View.Rect(Refs.SurfaceBox_Root);

    public override Image BoxImage
        => View.Image(Refs.ImageSurfaceBox);

    public override CanvasGroup CanvasGroup
        => View.CanvasGroup(Refs.SurfaceBox_Root);

    public override TMP_Text LineText
        => View.Text(Refs.SurfaceBoxLine_Text);

    public override TMP_Text NameText
        => View.Text(Refs.SurfaceBoxName_Text);
}