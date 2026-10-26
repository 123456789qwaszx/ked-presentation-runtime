using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 타이틀의 예제 전환.
///
/// 한 실행에 재생 호스트는 하나다(PresentationRuntime.AttachHost) — 실행 중에 호스트를 갈아 끼우지 않는다.
/// 그래서 고른 예제를 기억해 두고 씬을 다시 연다. SampleBootstrap은 Awake에서 기억된 예제를
/// 인스펙터 값보다 먼저 본다. 기억은 플레이를 새로 시작하면 지워진다 — 그때는 인스펙터 값이 기준이다.
/// </summary>
public static class SampleExampleSwitch
{
    private static SampleBootstrap.SampleExample? s_chosen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ForgetOnPlay() => s_chosen = null;

    /// <summary>이번 조립에 쓸 예제 — 타이틀에서 고른 것이 있으면 그것, 없으면 인스펙터 값.</summary>
    public static SampleBootstrap.SampleExample Resolve(SampleBootstrap.SampleExample inspectorValue) =>
        s_chosen ?? inspectorValue;

    public static SampleBootstrap.SampleExample Other(SampleBootstrap.SampleExample example) =>
        example == SampleBootstrap.SampleExample.PresentationOnly
            ? SampleBootstrap.SampleExample.ProgressionAndSave
            : SampleBootstrap.SampleExample.PresentationOnly;

    public static string DisplayName(SampleBootstrap.SampleExample example) =>
        example == SampleBootstrap.SampleExample.PresentationOnly ? "연출 단독" : "진행·저장";

    /// <summary>그 예제로 씬을 다시 연다. 타이틀에서만 부른다 — 재생 중이 아니다.</summary>
    public static void SwitchTo(SampleBootstrap.SampleExample example)
    {
        s_chosen = example;

        Debug.Log($"[예제] '{DisplayName(example)}' 예제로 씬을 다시 연다.");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
