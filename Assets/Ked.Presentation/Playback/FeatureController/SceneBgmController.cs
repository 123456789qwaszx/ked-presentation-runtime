// BGM은 장면 안에서 완결된다 — 장면마다 무음에서 시작하고, 장면이 곡을 정하지 않으면 끊는다.
// 같은 곡이면 끊지 않는다: 장면을 열 때는 의도만 비우고(Hold), 소리는 압축이 아닌 줄의 머리 커맨드가 다 돈 뒤에 맞춘다(Release).
// 되감기·로드·갈라지기도 같은 길이다 — 시크가 장면을 루트부터 다시 지나가며 의도를 쌓고, 표적 줄 머리에서 맞춘다.
public sealed class SceneBgmController
{
    private readonly BgmPlayer _bgm;

    public SceneBgmController(BgmPlayer bgm)
    {
        _bgm = bgm;
    }

    public void BeginScene() => _bgm?.Hold();

    public void SettleAfterLineHead() => _bgm?.Release();
}
