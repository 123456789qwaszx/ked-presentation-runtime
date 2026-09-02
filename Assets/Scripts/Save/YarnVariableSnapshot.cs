using System.Collections.Generic;

// [3] 연출 변수의 통덤프 — 장면 끝에 굽고, 이어하기에서 declare 초기값 위에 덮는다.
// Yarn 저장소의 세 타입을 그대로 나눠 든다. 뭉치면 bool이 float로 도장돼 <<if>>가 안 읽힌다.
//
// 진행 층이 Yarn 저장소에 [2]를 심지 않으므로(G0) 필터링 없이 통째로 굽는다.
public sealed class YarnVariableSnapshot
{
    public Dictionary<string, float> Floats = new();
    public Dictionary<string, string> Strings = new();
    public Dictionary<string, bool> Bools = new();

    public int Count => Floats.Count + Strings.Count + Bools.Count;
}
