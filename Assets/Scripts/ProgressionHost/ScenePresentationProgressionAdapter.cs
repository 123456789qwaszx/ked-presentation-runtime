using System.Threading.Tasks;
using Ked.Progression;

/// <summary>
/// 진행 층이 보는 장면 재생 계약(IScenePresentation)을 ScenePresentation에 잇는다.
///
/// ScenePresentation은 진행 층을 모른다. 연출 단독(NodePlaybackHost)도 같은 객체를
/// 쓰기 때문이다. 진행 층과의 접점은 이 어댑터 하나다.
/// </summary>
public sealed class ScenePresentationProgressionAdapter : IScenePresentation
{
    private readonly ScenePresentation _presentation;

    public ScenePresentationProgressionAdapter(ScenePresentation presentation)
    {
        _presentation = presentation;
    }

    public void BeginScene() => _presentation.BeginScene();

    public Task PlayEpisodeAsync(string nodeName) => _presentation.PlayEpisodeAsync(nodeName);

    public void PrepareReplay() => _presentation.PrepareReplay();

    public Task StopAsync() => _presentation.StopAsync();
}
