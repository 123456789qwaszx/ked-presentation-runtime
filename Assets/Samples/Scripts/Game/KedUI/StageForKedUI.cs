using UnityEngine;

/// <summary>
/// 새 예제 UI(KedUI)가 재생을 시작할 때 연출 무대를 보이게 하는 자리.
///
/// 무대(PresentationUIRoot)는 씬의 무대 캔버스에 늘 놓여 있고, 새 UI는 그 위 캔버스(정렬 20)에 얹힌다.
/// 그래서 할 일은 무대가 꺼져 있으면 켜는 것뿐이다 — 되감기·장면 전환마다 불려도 아무것도 바꾸지 않는다.
/// </summary>
public sealed class StageForKedUI : Ked.Samples.UI.IKedStagePort
{
    private readonly PresentationUIRoot _stage;

    public StageForKedUI(PresentationUIRoot stage)
    {
        _stage = stage;
    }

    public void ShowStage()
    {
        if (_stage == null)
            return;

        GameObject go = _stage.gameObject;
        if (!go.activeSelf)
            go.SetActive(true);
    }
}
