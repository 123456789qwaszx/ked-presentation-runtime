using System.Threading;
using System.Threading.Tasks;

namespace Ked.Progression
{
    public interface ISceneRunner
    {
        Task<SceneRunResult> RunAsync(
            SceneRunContext context,
            CancellationToken cancellationToken);

        Task RequestReplayAsync(SceneRunContext scene);

        Task StopAsync();
    }
}
