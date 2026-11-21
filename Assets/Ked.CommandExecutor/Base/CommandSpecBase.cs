using System;

[Serializable]
public abstract class CommandSpecBase
{
    // 전경 — 끝날 때까지 제 묶음(앵커)의 다음 명령과 넘김을 붙잡는다(SequencePlayer · AdvanceGate). 압축 중에는 기다리지 않는다.
    // 무대 파서가 pause · screen_cover에 세운다. 줄의 다음 앵커는 앞 앵커의 전경이 끝난 뒤에 열린다(docs/PLAN_line-anchor.md A7 · AR3).
    public bool wait;
}