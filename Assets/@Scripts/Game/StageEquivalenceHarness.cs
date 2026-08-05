using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Ked.Presentation.Core;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// U14 등가성 하네스 — VnTool Phase 2b의 초록불을 판정하는 장치.
//
// 관측점: DialogueAdvanceDispatcher.BeforeNextLineDispatched
//   = "라인 완전 표시 + 전진 직전" — 정착 상태가 안정된 순간.
// 비교: (yarn 원문을 코어 리듀서로 그 라인까지 접은 상태) vs (살아 있는 무대 캡처).
// 판정: StageStateComparer (ε = b-1 정책. 불일치는 ε를 늘리는 게 아니라 리듀서를 고친다).
//
// 폴드 모델 (대표 에피소드의 선형 구조 전제):
//   스토리 그룹 j의 커맨드(beat X는 X 노드 전체로, pres_start P는 P를 서브 레인으로 전개)
//   + 서브 레인 그룹 j — 를 j = 0..현재 라인까지 접는다.
//   서브 레인은 메인 라인당 1행 전진(hold 1) 가정이다. 어긋나면 결과가 그것을 알려 준다.
//
// 실행 프로토콜: 부트스트랩의 enableEquivalenceHarness 켜고 에피소드 재생.
//   랩드스킵 권장(모든 커맨드가 즉시 확정 = 라이브가 곧 정착 상태).
//   라인마다 콘솔 판정, 종료 시 EquivalenceReports/*.json.
// ─────────────────────────────────────────────────────────────────────────────
public sealed class StageEquivalenceHarness : MonoBehaviour
{
    private VNRuntimeStateProvider _provider;
    private PresentationLaneScopeSession _session;
    private PresentationShotResponseSystem _shotSystem;
    private DialogueAdvanceDispatcher _dispatcher;

    private StageReducerTuning _tuning;
    private readonly Dictionary<string, List<YarnLineGroup>> _nodeGroups = new(StringComparer.Ordinal);
    private readonly List<string> _extractWarnings = new();

    // 현재 따라가는 스토리 노드의 시간표.
    private string _storyNode;
    private List<YarnLineGroup> _storyGroups;
    private List<YarnLineGroup> _presGroups;
    private readonly Dictionary<string, int> _lineIndexById = new(StringComparer.Ordinal);
    private readonly List<StageState> _foldedByLine = new();

    // 원문에 #line 태그가 없는 프로젝트에서는 ID 매칭이 불가능하다(런타임 ID는
    // 컴파일 시 생성). 그래서 기본은 "전진 순서 커서"이고, 태그가 있으면 ID가 이긴다.
    // 커서 방식은 선형 전진 전제다 — 롤백/로드가 끼면 대사 검증(아래)이 어긋남을 알린다.
    private int _lineCursor;
    private bool _timelineDriftWarned;

    private readonly List<LineVerdict> _verdicts = new();
    private bool _ready;

    [Serializable]
    private sealed class LineVerdict
    {
        public int lineIndex;
        public string lineId;
        public bool equivalent;
        public int comparedNodes;
        public int unhandledCount;
        public List<string> mismatches = new();
    }

    [Serializable]
    private sealed class Report
    {
        public string storyNode;
        public string verdict;
        public int equivalentLines;
        public int mismatchedLines;
        public List<LineVerdict> lines = new();
        public List<string> extractWarnings = new();
        public List<string> finalUnhandled = new();
    }

    public void Initialize(
        VNRuntimeStateProvider provider,
        PresentationLaneScopeSession session,
        PresentationShotResponseSystem shotSystem,
        DialogueAdvanceDispatcher dispatcher)
    {
        _provider = provider;
        _session = session;
        _shotSystem = shotSystem;
        _dispatcher = dispatcher;

        try
        {
            LoadTuning();
            LoadDialogueFiles();
            _ready = true;

            _dispatcher.BeforeNextLineDispatched += CompareAtCurrentLine;

            Debug.Log(
                $"[StageEquivalenceHarness] 준비 완료. 노드 {_nodeGroups.Count}개 파싱, " +
                $"경고 {_extractWarnings.Count}건. 에피소드를 재생하면 라인마다 판정한다.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[StageEquivalenceHarness] 초기화 실패 — 하네스 비활성: {ex}");
        }
    }

    private void OnDestroy()
    {
        if (_dispatcher != null)
            _dispatcher.BeforeNextLineDispatched -= CompareAtCurrentLine;

        if (_verdicts.Count > 0)
            WriteReport();
    }

    // ── 준비 ─────────────────────────────────────────────────────────

    private void LoadTuning()
    {
        string root = Path.GetDirectoryName(Application.dataPath);
        string tuningDir = Path.Combine(root, "ExportedTuning");

        RigSchemasFileDto rigSchemas = JsonUtility.FromJson<RigSchemasFileDto>(
            File.ReadAllText(Path.Combine(tuningDir, "rig-schemas.json")));

        BaseResolutionJson baseResolution = JsonUtility.FromJson<BaseResolutionJson>(
            File.ReadAllText(Path.Combine(tuningDir, "base-resolution.json")));

        DepthTuningFileDto depthTuning = JsonUtility.FromJson<DepthTuningFileDto>(
            File.ReadAllText(Path.Combine(tuningDir, "presets", "depth.json")));

        FocusTuningFileDto focusTuning = JsonUtility.FromJson<FocusTuningFileDto>(
            File.ReadAllText(Path.Combine(tuningDir, "presets", "focus-tuning.json")));

        _tuning = new StageReducerTuning
        {
            RigSchemas = rigSchemas,
            ReferenceStageWidth = baseResolution.referenceResolution.x,
            BaseResolution = new Vec2(
                baseResolution.referenceResolution.x,
                baseResolution.referenceResolution.y),
            DepthPresets = depthTuning?.MonoBehaviour?.presets,
            FocusTuning = focusTuning?.MonoBehaviour,
        };
    }

    [Serializable]
    private sealed class BaseResolutionJson
    {
        public Vector2 referenceResolution;
    }

    private void LoadDialogueFiles()
    {
        string dialogueDir = Path.Combine(Application.dataPath, "@Dialogue");

        foreach (string path in Directory.GetFiles(dialogueDir, "*.yarn"))
        {
            List<YarnNodeGroups> nodes = YarnCommandTextExtractor.ExtractNodes(
                File.ReadAllText(path), Path.GetFileName(path), _extractWarnings);

            foreach (YarnNodeGroups node in nodes)
            {
                if (string.IsNullOrEmpty(node.NodeName))
                    continue;

                if (!_nodeGroups.TryAdd(node.NodeName, node.Groups))
                    _extractWarnings.Add($"노드 이름 중복: {node.NodeName} ({Path.GetFileName(path)})");
            }
        }
    }

    // ── 시간표 ───────────────────────────────────────────────────────

    private bool TryBuildTimeline(string storyNode)
    {
        if (!_nodeGroups.TryGetValue(storyNode, out List<YarnLineGroup> groups))
        {
            Debug.LogWarning($"[StageEquivalenceHarness] 스토리 노드 '{storyNode}'의 원문을 못 찾았다.");
            return false;
        }

        _storyNode = storyNode;
        _storyGroups = groups;
        _presGroups = null;
        _lineIndexById.Clear();
        _foldedByLine.Clear();
        _verdicts.Clear();
        _lineCursor = 0;
        _timelineDriftWarned = false;

        for (int i = 0; i < groups.Count; i++)
        {
            if (!string.IsNullOrEmpty(groups[i].LineId))
                _lineIndexById[groups[i].LineId] = i;
        }

        return true;
    }

    /// <summary>스토리 그룹의 커맨드를 전개한다: beat X → X 노드 전체, pres_start P → 서브 레인 등록.</summary>
    private IEnumerable<StageCommand> ExpandStoryCommands(List<StageCommand> commands)
    {
        foreach (StageCommand command in commands)
        {
            if (command.Name == "beat" || command.Name == "beat_fx")
            {
                string beatNode = command.Arg(0);

                if (beatNode != null && _nodeGroups.TryGetValue(beatNode, out List<YarnLineGroup> beatGroups))
                {
                    foreach (YarnLineGroup g in beatGroups)
                        foreach (StageCommand inner in g.Commands)
                            yield return inner;
                }
                else
                {
                    yield return command; // 노드를 못 찾으면 그대로 → 리듀서 Unhandled로 남는다
                }

                continue;
            }

            if (command.Name == "pres_start")
            {
                string presNode = command.Arg(0);

                if (presNode != null && _nodeGroups.TryGetValue(presNode, out List<YarnLineGroup> presGroups))
                    _presGroups = presGroups;
                else
                    yield return command;

                continue;
            }

            yield return command;
        }
    }

    private StageState FoldedUpTo(int lineIndex)
    {
        while (_foldedByLine.Count <= lineIndex)
        {
            int j = _foldedByLine.Count;

            StageState state = j == 0
                ? StageReducer.CreateInitialState(_tuning)
                : _foldedByLine[j - 1];

            if (j < _storyGroups.Count)
                state = StageReducer.ApplyAll(state, ExpandStoryCommands(_storyGroups[j].Commands), _tuning);

            // 서브 레인은 메인 라인당 1행 전진 가정 — 그룹 j가 메인 라인 j에 실린다.
            if (_presGroups != null && j < _presGroups.Count)
                state = StageReducer.ApplyAll(state, _presGroups[j].Commands, _tuning);

            _foldedByLine.Add(state);
        }

        return _foldedByLine[lineIndex];
    }

    // ── 판정 ─────────────────────────────────────────────────────────

    private void CompareAtCurrentLine()
    {
        if (!_ready)
            return;

        try
        {
            string nodeName = _provider.CurrentNodeName;
            string lineId = _provider.CurrentLineId;

            if (string.IsNullOrEmpty(nodeName))
                return;

            // 다른 스토리 노드로 넘어갔다(다음 에피소드 등): 지금까지를 리포트로 내리고 재구축.
            if (_storyNode != null && nodeName != _storyNode)
            {
                if (_verdicts.Count > 0)
                    WriteReport();

                _storyNode = null;
            }

            if (_storyNode == null && !TryBuildTimeline(nodeName))
                return;

            // 라인 인덱스: 태그가 있으면 ID 매칭, 없으면(이 프로젝트 원문이 그렇다) 전진 순서 커서.
            int lineIndex;

            if (!string.IsNullOrEmpty(lineId) && _lineIndexById.TryGetValue(lineId, out int byId))
            {
                lineIndex = byId;
                _lineCursor = byId + 1;
            }
            else
            {
                lineIndex = _lineCursor++;
            }

            if (lineIndex >= _storyGroups.Count)
            {
                Debug.LogWarning(
                    $"[StageEquivalenceHarness] 라인 커서({lineIndex})가 원문 시간표({_storyGroups.Count}줄)를 " +
                    "넘었다 — 선형 전진 전제가 깨졌다(분기/롤백?). 이후 판정을 멈춘다.");
                return;
            }

            // 대사 검증: 커서가 가리키는 원문 대사와 실제 표시 대사가 다르면 시간표가 어긋난 것이다.
            VerifyLineTextDrift(lineIndex);

            StageState folded = FoldedUpTo(lineIndex);

            CommandRunScope scope = _session.CurrentScope;

            if (scope == null)
                return;

            StageState captured = StageStateCapture.Capture(
                scope.CharacterRigs, _shotSystem, _tuning.BaseResolution);

            StageStateComparer.Result result = StageStateComparer.Compare(folded, captured);

            LineVerdict verdict = new LineVerdict
            {
                lineIndex = lineIndex,
                lineId = lineId,
                equivalent = result.IsEquivalent,
                comparedNodes = result.ComparedNodes,
                unhandledCount = folded.Unhandled.Count,
            };

            verdict.mismatches.AddRange(result.Mismatches);
            _verdicts.Add(verdict);

            if (result.IsEquivalent)
            {
                Debug.Log($"[U14] 라인 {lineIndex} ({lineId}): 등가 ✓ — {result}");
            }
            else
            {
                Debug.LogWarning(
                    $"[U14] 라인 {lineIndex} ({lineId}): 불일치 ✗ — {result}\n" +
                    string.Join("\n", result.Mismatches.GetRange(0, Math.Min(10, result.Mismatches.Count))));
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[StageEquivalenceHarness] 판정 중 예외: {ex}");
        }
    }

    /// <summary>
    /// 순서 커서의 안전망: 원문 대사와 실제 표시 대사가 어긋나면 알린다(1회).
    /// 화자 표기·태그 차이가 있을 수 있어 "포함" 수준의 느슨한 검사다.
    /// </summary>
    private void VerifyLineTextDrift(int lineIndex)
    {
        if (_timelineDriftWarned)
            return;

        string expected = Normalize(_storyGroups[lineIndex].LineText);
        string actual = Normalize(_provider.CurrentLinePreview);

        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(actual))
            return;

        // 짧은 쪽이 긴 쪽에 들어 있으면 같은 대사로 본다.
        bool matches = expected.Contains(actual) || actual.Contains(expected);

        if (!matches)
        {
            _timelineDriftWarned = true;
            Debug.LogWarning(
                $"[StageEquivalenceHarness] 라인 {lineIndex}의 대사가 원문 시간표와 다르다 — " +
                $"시간표 어긋남(분기/롤백/스킵?). 이후 판정의 인덱스를 의심할 것.\n" +
                $"원문: {_storyGroups[lineIndex].LineText}\n표시: {_provider.CurrentLinePreview}");
        }

        static string Normalize(string s)
            => string.IsNullOrEmpty(s) ? null : s.Replace(" ", "").Replace("　", "").Trim();
    }

    // ── 리포트 ───────────────────────────────────────────────────────

    private void WriteReport()
    {
        try
        {
            Report report = new Report { storyNode = _storyNode };

            foreach (LineVerdict verdict in _verdicts)
            {
                report.lines.Add(verdict);

                if (verdict.equivalent)
                    report.equivalentLines++;
                else
                    report.mismatchedLines++;
            }

            report.verdict = report.mismatchedLines == 0
                ? $"등가 — {report.equivalentLines}라인 전부"
                : $"불일치 {report.mismatchedLines}/{report.equivalentLines + report.mismatchedLines}라인";

            report.extractWarnings.AddRange(_extractWarnings);

            if (_foldedByLine.Count > 0)
            {
                foreach (UnhandledCommand unhandled in _foldedByLine[_foldedByLine.Count - 1].Unhandled)
                    report.finalUnhandled.Add(unhandled.ToString());
            }

            string dir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "EquivalenceReports");
            Directory.CreateDirectory(dir);

            string path = Path.Combine(
                dir,
                $"equivalence-{DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}.json");

            File.WriteAllText(path, JsonUtility.ToJson(report, true));
            Debug.Log($"[StageEquivalenceHarness] 리포트: {path} — {report.verdict}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[StageEquivalenceHarness] 리포트 쓰기 실패: {ex}");
        }
    }
}
