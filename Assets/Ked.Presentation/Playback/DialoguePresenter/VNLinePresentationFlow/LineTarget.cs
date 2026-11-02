// 연출이 "지금 어디까지 보여 줬는가"를 말하는 단위다. 저장이 이 값을 싣지만,
// 저장이 없어도 롤백과 시크가 같은 좌표로 움직인다.
internal sealed class LineTarget
{
    public string LineId;
    public int Occurrence;
}
