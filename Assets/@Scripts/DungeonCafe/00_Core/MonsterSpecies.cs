/// <summary>
/// 몬스터 종족.
/// 개별 몬스터가 아니라 종족 단위로 통제 상실 이후의 파멸 방향이 결정된다.
/// 배드엔딩과 업무 수첩의 위험 경고는 이 단위로 작성한다.
/// </summary>
public enum MonsterSpecies
{
    None = 0,

    // 기생 장비종: 통제권을 잃은 메이드가 장비의 새로운 착용자가 된다.
    ParasiticEquipment = 10,

    // 기억 포식종: 메이드가 자신의 기억과 역할을 잃고 몬스터가 요구하는 인물로 변한다.
    MemoryDevourer = 20,

    // 감응 증폭종: 메이드가 주입된 충동과 자신의 감정을 구분하지 못하게 된다.
    ResonanceAmplifier = 30,

    // 포식·구속종: 철수해야 할 시점을 스스로 판단하지 못하고 접객을 계속한다.
    PredatoryBinder = 40,
}
