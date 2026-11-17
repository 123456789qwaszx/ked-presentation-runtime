using System;

[Serializable]
public abstract class CommandSpecBase
{
    // 전경 — 끝날 때까지 스텝의 다음 명령과 넘김을 붙잡는다(SequencePlayer · AdvanceGate). 압축 중에는 기다리지 않는다.
    // 무대 파서가 pause · screen_cover에 세운다. 대사 줄의 머리 · 자리에는 기다림이 없다(docs/PLAN_line-beat.md D3).
    public bool wait;
}