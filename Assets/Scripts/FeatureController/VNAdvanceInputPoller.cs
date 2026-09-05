using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// VN 재생의 유일한 프레임 입력 구동자.
public sealed class VNAdvanceInputPoller : MonoBehaviour
{
    [SerializeField] private VNAdvanceInputBindings _bindings = new();

    private DialogueAdvanceDispatcher _dialogueAdvanceDispatcher;
    private VNFeatureController _featureController;

    private ScenePlaybackSession _scenePlayback;
    private ScenePlaybackDebugRunner _debugPlayback;

    // 디버그 키로 직접 재생할 Yarn node.
    private string _yarnEntryKey;
    private string[] _debugEpisodeChain;

    private ProgressionLauncher _progressionLauncher;
    private SaveCoordinator _saveCoordinator;

    private bool _rapidSkipHeld;
    private bool _speedUpHeld;

    public void Initialize(
        DialogueAdvanceDispatcher dialogueAdvanceDispatcher,
        VNFeatureController featureController,
        ScenePlaybackSession scenePlayback,
        ScenePlaybackDebugRunner debugPlayback,
        string yarnEntryKey,
        string[] debugEpisodeChain,
        ProgressionLauncher progressionLauncher,
        SaveCoordinator saveCoordinator)
    {
        _dialogueAdvanceDispatcher = dialogueAdvanceDispatcher;
        _featureController = featureController;
        _scenePlayback = scenePlayback;
        _debugPlayback = debugPlayback;
        _yarnEntryKey = yarnEntryKey;
        _debugEpisodeChain = debugEpisodeChain;
        _progressionLauncher = progressionLauncher;
        _saveCoordinator = saveCoordinator;
    }

    private void Update()
    {
        if (_dialogueAdvanceDispatcher == null || _featureController == null)
            return;

        PollAdvance();
        PollRapidSkip();
        PollSpeedUpMode();
        PollFeatureToggles();

        PollDebugRunYarn();
        PollDebugRunEpisodeChain();
        PollDebugRunProgression();
        PollDebugNewGame();
        PollDebugBookmark();
        PollDebugLoadBookmark();

        _featureController.Tick();
    }

    // Update에서 호출하는 입력 handler라 async void.
    private async void PollDebugRunYarn()
    {
        if (!_bindings.IsRunYarnPressed())
            return;

        if (!CanRunDebugPlayback())
            return;

        await _debugPlayback.RunSingleNodeAsync(_yarnEntryKey);
    }

    private async void PollDebugRunEpisodeChain()
    {
        if (!_bindings.IsRunEpisodeChainPressed())
            return;

        if (!CanRunDebugPlayback())
            return;

        if (_debugEpisodeChain == null || _debugEpisodeChain.Length == 0)
        {
            Debug.LogWarning("[연결] 이어 재생할 노드가 비어 있다.");
            return;
        }

        await _debugPlayback.RunNodeChainAsync(_debugEpisodeChain);
    }

    // 세이브가 있으면 이어하기, 없으면 새 게임.
    private void PollDebugRunProgression()
    {
        if (_bindings.IsLoadProgressionPressed())
            StartProgression();
    }

    private void PollDebugNewGame()
    {
        if (_bindings.IsNewGamePressed())
            StartNewGame();
    }

    private void PollDebugBookmark()
    {
        if (!_bindings.IsBookmarkPressed())
            return;

        if (_progressionLauncher == null
            || !_progressionLauncher.IsRunning
            || _saveCoordinator == null)
        {
            return;
        }

        if (!_featureController.TryGetCurrentLine(
                out SaveLineTarget target,
                out string preview))
        {
            Debug.Log(
                "[즐겨찾기] 지금은 찍을 라인이 없다(시크 중이거나 라인 전).");

            return;
        }

        _saveCoordinator.CreateBookmark(
            _progressionLauncher.PendingPath,
            _featureController.CreateYarnChoiceSnapshot(),
            target,
            preview);
    }

    private async void PollDebugLoadBookmark()
    {
        if (!_bindings.IsLoadBookmarkPressed())
            return;

        if (_progressionLauncher == null || _saveCoordinator == null)
            return;

        IReadOnlyList<Bookmark> bookmarks = _saveCoordinator.Bookmarks;

        if (bookmarks.Count == 0)
        {
            Debug.Log("[즐겨찾기] 아직 없다.");
            return;
        }

        Bookmark latest = bookmarks[bookmarks.Count - 1];

        await _progressionLauncher.StopAsync();
        await _saveCoordinator.ForkFromBookmark(latest);
        await _progressionLauncher.LaunchAsync();
    }

    private async void StartProgression()
    {
        if (_progressionLauncher == null)
            return;

        if (_debugPlayback != null && _debugPlayback.IsRunning)
            return;

        if (_saveCoordinator != null)
            await _saveCoordinator.StartupSync;

        await _progressionLauncher.LaunchAsync();
    }

    private async void StartNewGame()
    {
        if (_progressionLauncher == null
            || _saveCoordinator == null
            || _progressionLauncher.IsRunning
            || (_debugPlayback != null && _debugPlayback.IsRunning))
        {
            return;
        }

        await _saveCoordinator.StartupSync;
        await _saveCoordinator.StartNewGameAsync();
        await _progressionLauncher.LaunchAsync();
    }

    private bool CanRunDebugPlayback()
    {
        if (_progressionLauncher != null && _progressionLauncher.IsRunning)
        {
            Debug.Log(
                "[진행] 도는 중이라 대사 단독 재생 키를 무시한다.");

            return false;
        }

        return _debugPlayback != null && !_debugPlayback.IsRunning;
    }

    private void PollAdvance()
    {
        if (_bindings.IsAdvancePressed())
            _dialogueAdvanceDispatcher.DispatchAdvance();
    }

    private void PollRapidSkip()
    {
        bool held = _bindings.IsRapidSkipHeld();

        if (held && !_rapidSkipHeld)
            _featureController.BeginRapidSkip();

        if (!held && _rapidSkipHeld)
            _featureController.EndRapidSkip();

        _rapidSkipHeld = held;
    }

    private void PollSpeedUpMode()
    {
        bool held = _bindings.IsSpeedUpHeld();

        if (held && !_speedUpHeld)
            _featureController.BeginSpeedUpMode();

        if (!held && _speedUpHeld)
            _featureController.EndSpeedUpMode();

        _speedUpHeld = held;

        if (_bindings.IsSpeedUpTogglePressed())
            _featureController.ToggleSpeedUpMode();
    }

    private async void PollFeatureToggles()
    {
        if (_bindings.IsAutoTogglePressed())
            _featureController.ToggleAuto();

        if (!_bindings.IsRollbackPressed())
            return;

        if (!_featureController.RequestRollbackOneStep())
            return;

        await _scenePlayback.RequestReplayAsync();
    }
}