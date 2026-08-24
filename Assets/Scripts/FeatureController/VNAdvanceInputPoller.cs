using UnityEngine;

// VN 재생의 유일한 프레임 구동자.
public sealed class VNAdvanceInputPoller : MonoBehaviour
{
    private readonly VNAdvanceInputBindings _bindings = new();

    private DialogueAdvanceDispatcher _dialogueAdvanceDispatcher;
    private VNFeatureController _featureController;
    
    private VNLinePresentationState _linePresentationAdvanceState;
    private EpisodePlayer _episodePlayer;

    // 디버그 키(2번)로 재생할 노드.
    private string _yarnEntryKey;

    private string[] _debugEpisodeChain;

    // 진행 층을 시작 경로.
    private ProgressionLauncher _progressionLauncher;

    private bool _rapidSkipHeld;
    private bool _speedUpHeld;

    public void Initialize(
        DialogueAdvanceDispatcher dialogueAdvanceDispatcher,
        VNFeatureController featureController,
        VNLinePresentationState linePresentationAdvanceState,
        EpisodePlayer episodePlayer,
        string yarnEntryKey,
        string[] debugEpisodeChain,
        ProgressionLauncher progressionLauncher)
    {
        _dialogueAdvanceDispatcher = dialogueAdvanceDispatcher;
        _featureController = featureController;
        _linePresentationAdvanceState = linePresentationAdvanceState;
        _episodePlayer = episodePlayer;
        _yarnEntryKey = yarnEntryKey;
        _debugEpisodeChain = debugEpisodeChain;
        _progressionLauncher = progressionLauncher;
    }

    private void Update()
    {
        if (_dialogueAdvanceDispatcher == null || _featureController == null)
            return;

        // 입력을 먼저 반영한 뒤 그 결과로 틱을 돌림.
        PollAdvance();
        PollRapidSkip();
        PollSpeedUpMode();
        PollFeatureToggles();
        PollDebugRunYarn();
        PollDebugRunEpisodeChain();
        PollDebugRunProgression();

        _featureController.Tick();
    }

    // Update에서 부르는 입력 핸들러라 async void다. 첫 await에서 바로 반환.
    private async void PollDebugRunYarn()
    {
        if (!_bindings.IsRunYarnPressed())
            return;

        if (IsProgressionRunning())
            return;

        await _episodePlayer.StartGameAsync(_yarnEntryKey);
    }

    private async void PollDebugRunEpisodeChain()
    {
        if (!_bindings.IsRunEpisodeChainPressed())
            return;

        if (IsProgressionRunning())
            return;

        if (_debugEpisodeChain == null || _debugEpisodeChain.Length == 0)
        {
            Debug.LogWarning("[연결] 이어 재생할 노드가 비어 있다.");
            return;
        }

        // 시작과 끝을 둘 다 찍음.
        for (int i = 0; i < _debugEpisodeChain.Length; i++)
        {
            string nodeName = _debugEpisodeChain[i];

            Debug.Log($"[연결] {i + 1}/{_debugEpisodeChain.Length} 시작 — \"{nodeName}\"");

            // 첫 진입 이후로는 백로그를 이어야 하므로 다른 경로 사용.
            if (i == 0)
                await _episodePlayer.StartGameAsync(nodeName);
            else
                await _episodePlayer.ContinueEpisodeAsync(nodeName);

            Debug.Log($"[연결] {i + 1}/{_debugEpisodeChain.Length} 끝 — \"{nodeName}\"");
        }

        Debug.Log("[연결] 사슬 끝.");
    }

    // 싣고·대조하고·모는 것은 런처 담당.
    private void PollDebugRunProgression()
    {
        // 선택지 입력은 여기로 안 옴. VNOptionItem 이 Selectable이기에 EventSystem 사용.
        if (_bindings.IsLoadProgressionPressed())
            StartProgression();
    }

    private async void StartProgression()
    {
        if (_progressionLauncher == null)
            return;

        await _progressionLauncher.LaunchAsync();
    }

    private bool IsProgressionRunning()
    {
        if (_progressionLauncher == null || !_progressionLauncher.IsRunning)
            return false;

        Debug.Log("[진행] 도는 중이라 대사 단독 재생 키를 무시한다.");
        return true;
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

        if (_bindings.IsRollbackPressed())
        {
            if (!_featureController.RequestRollbackOneStep())
                return;

            await _episodePlayer.RequestReplayAsync();
        }
    }
}