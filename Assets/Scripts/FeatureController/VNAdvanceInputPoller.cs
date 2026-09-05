using System.Collections.Generic;
using UnityEngine;

// VN 재생의 유일한 프레임 구동자.
public sealed class VNAdvanceInputPoller : MonoBehaviour
{
    [SerializeField] private VNAdvanceInputBindings _bindings = new();

    private DialogueAdvanceDispatcher _dialogueAdvanceDispatcher;
    private VNFeatureController _featureController;
    
    private VNLinePresentationState _linePresentationAdvanceState;
    private EpisodePlayer _episodePlayer;

    // 디버그 키(2번)로 재생할 노드.
    private string _yarnEntryKey;

    private string[] _debugEpisodeChain;

    // 진행 층 시작 경로.
    private ProgressionLauncher _progressionLauncher;

    // 세이브 정리.
    private SaveCoordinator _saveCoordinator;

    private bool _rapidSkipHeld;
    private bool _speedUpHeld;

    public void Initialize(
        DialogueAdvanceDispatcher dialogueAdvanceDispatcher,
        VNFeatureController featureController,
        VNLinePresentationState linePresentationAdvanceState,
        EpisodePlayer episodePlayer,
        string yarnEntryKey,
        string[] debugEpisodeChain,
        ProgressionLauncher progressionLauncher,
        SaveCoordinator saveCoordinator)
    {
        _dialogueAdvanceDispatcher = dialogueAdvanceDispatcher;
        _featureController = featureController;
        _linePresentationAdvanceState = linePresentationAdvanceState;
        _episodePlayer = episodePlayer;
        _yarnEntryKey = yarnEntryKey;
        _debugEpisodeChain = debugEpisodeChain;
        _progressionLauncher = progressionLauncher;
        _saveCoordinator = saveCoordinator;
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
        PollDebugNewGame();
        PollDebugBookmark();
        PollDebugLoadBookmark();

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

    // 싣고·대조하고·모는 것은 런처 담당. 세이브가 있으면 이어하기, 없으면 새 게임.
    private void PollDebugRunProgression()
    {
        // 선택지 입력은 여기로 안 옴. VNOptionItem 이 Selectable이기에 EventSystem 사용.
        if (_bindings.IsLoadProgressionPressed())
            StartProgression();
    }

    // 세이브를 버리고 새 회차로 (M7). 순서가 뜻이다 — 비운 뒤에 시작해야 런처가 새 게임으로 본다.
    private void PollDebugNewGame()
    {
        if (_bindings.IsNewGamePressed())
            StartNewGame();
    }

    // 즐겨찾기(현재 라인) — 디버그 키. 라인 표시 중이든 옵션 박스가 떠 있든 된다. 시크 중엔 안 된다.
    private void PollDebugBookmark()
    {
        if (!_bindings.IsBookmarkPressed())
            return;

        if (_progressionLauncher == null || !_progressionLauncher.IsRunning || _saveCoordinator == null)
            return;

        if (!_featureController.TryGetCurrentLine(out SaveLineTarget target, out string preview))
        {
            Debug.Log("[즐겨찾기] 지금은 찍을 라인이 없다(시크 중이거나 라인 전).");
            return;
        }

        _saveCoordinator.CreateBookmark(
            _progressionLauncher.PendingPath,
            _featureController.CreateYarnChoiceSnapshot(),
            target,
            preview);
    }

    // 마지막 즐겨찾기로 갈라지기 — 디버그 키. 목록 UI는 F5.
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

    // 진행을 시작하는 두 경로는 시작 동기화(복구·409 갈라지기가 활성 파일을 쓸 수 있다)를 먼저 기다린다.
    private async void StartProgression()
    {
        if (_progressionLauncher == null)
            return;

        if (_saveCoordinator != null)
            await _saveCoordinator.StartupSync;

        await _progressionLauncher.LaunchAsync();
    }

    private async void StartNewGame()
    {
        if (_progressionLauncher == null || _saveCoordinator == null || _progressionLauncher.IsRunning)
            return;

        await _saveCoordinator.StartupSync;
        await _saveCoordinator.StartNewGameAsync();
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