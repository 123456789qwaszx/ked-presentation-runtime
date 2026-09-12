// v3 envelope의 게임 데이터 부분. 기존 서버 metadata는 역직렬화 시 무시한다.
// 디렉터리와 FormatVersion을 유지하여 기존 로컬 저장을 그대로 읽는다.
public sealed class PlaythroughFile
{
    public int FormatVersion = 3;
    public LocalSaveFile Snapshot;
    public long LocalCommitVersion;
}
