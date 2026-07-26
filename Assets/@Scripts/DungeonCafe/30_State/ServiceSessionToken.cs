/// <summary>
/// 접객 세션 실행 토큰.
/// 세션이 취소/재시작된 뒤 뒤늦게 도착한 콜백이 공유 상태를 커밋하는 것을 막는다.
/// </summary>
public readonly struct ServiceSessionToken
{
    public readonly int Version;

    public ServiceSessionToken(int version)
    {
        Version = version;
    }
}
