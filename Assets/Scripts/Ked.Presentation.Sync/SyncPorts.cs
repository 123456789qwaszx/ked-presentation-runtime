using System.Threading.Tasks;

namespace Ked.Presentation.Sync
{
    // 동기화 대상 레인 포트
    public interface ILaneRunner
    {
        bool IsRunning { get; }

        void Start(string nodeName);
        Task StopAsync();
        void RequestNextLine();
    }

    // 프레임 경계.
    public interface IFrameClock
    {
        Task NextFrameAsync();
    }
}