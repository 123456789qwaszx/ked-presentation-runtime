using UnityEngine;

// 새 예제 UI(KedUI)와 옛 UI가 한 씬에 있는 동안(PLAN_uipf-ui U3 전까지)의 경계.
//
// 연출 무대(배경·인물·화면 효과)는 옛 캔버스의 PresentationUIRoot가 그린다. 새 UI는 그 위 캔버스(정렬 20)에서
// 퀵메뉴·대사창·선택지·메뉴를 그린다. 그래서 옛 UI 쪽에서는
//   - 무대 화면은 재생이 시작될 때 앞으로 가져오고(ShowStage), 그 HUD 층(옛 대사창·선택지·버튼)은 감춘다.
//   - 옛 타이틀·백로그·확인·시스템 메뉴는 시작할 때 감춘다 — 새 UI가 같은 일을 한다.
//
// 이 파일은 Ked.Samples.UI 네임스페이스를 가져오지 않는다. 옛 UI와 새 UI가 같은 이름(ConfirmPanel 등)을 쓰기 때문이다.

/// <summary>무대를 앞으로 — 옛 UIManager의 Root를 PresentationUIRoot로.</summary>
public sealed class LegacyStageForKedUI : Ked.Samples.UI.IKedStagePort
{
    private readonly UIManager _legacy;

    public LegacyStageForKedUI(UIManager legacy)
    {
        _legacy = legacy;
    }

    public void ShowStage()
    {
        if (_legacy == null)
            return;

        // 이미 무대가 보이면 패치를 다시 돌리지 않는다(되감기·장면 전환마다 불린다).
        if (_legacy.CurSceneRoot is PresentationUIRoot current && current.gameObject.activeInHierarchy)
        {
            current.SetLegacyHudVisible(false);
            return;
        }

        _legacy.SwitchRoot<PresentationUIRoot>(
            afterPatched: root => root.SetLegacyHudVisible(false),
            forceRefresh: true);
    }

    /// <summary>시작할 때 한 번 — 새 UI가 맡는 옛 화면을 감춘다.</summary>
    public static void HideLegacyScreens(UIManager legacy)
    {
        if (legacy == null)
            return;

        Hide(legacy.GetUI<TitleUIRoot>());
        Hide(legacy.GetUI<BacklogPanel>());
        Hide(legacy.GetUI<ConfirmPanel>());
        Hide(legacy.GetUI<SystemMenuPanel>());
        Hide(legacy.GetUI<DialogueSurfaceBox>());
        Hide(legacy.GetUI<VNDefaultOptionsPanel>());

        PresentationUIRoot stage = legacy.GetUI<PresentationUIRoot>();
        if (stage != null)
            stage.SetLegacyHudVisible(false);
    }

    private static void Hide(UIBase view)
    {
        if (view != null && view.gameObject.activeSelf)
            view.gameObject.SetActive(false);
    }
}
