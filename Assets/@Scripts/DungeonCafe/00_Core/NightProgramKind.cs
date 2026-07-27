/// <summary>
/// 밤에 메이드 한 명에게 적용할 수 있는 처리 방식.
/// </summary>
public enum NightProgramKind
{
    None = 0,

    // 위로와 치료. 붕괴도를 안전하게 낮춘다
    Care = 10,

    // 관리하의 의도적 억압하여 붕괴를 유도한다.
    // 붕괴도를 한계까지 끌어올릴 시, 붕괴도를 절반으로 낮추고 이벤트를 재생한다.
    ManagedRelease = 20,
}
