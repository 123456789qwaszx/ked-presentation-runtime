using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Ked.Presentation.Core;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// 등가성 하네스 — "코어가 접은 상태"와 "실제 재생한 무대"가 같은지 라인마다 판정한다.
//
// 관측점: DialogueAdvanceDispatcher.BeforeNextLineDispatched
//   = "라인 완전 표시 + 전진 직전" — 트윈이 다 끝나 정착 상태가 안정된 순간.
// 비교: (yarn 원문을 코어 리듀서로 그 라인까지 접은 상태) vs (살아 있는 무대 캡처).
// 판정: StageStateComparer. **불일치가 나면 ε를 늘리는 게 아니라 리듀서를 고친다.**
//
// 폴드 모델 (대표 에피소드의 선형 구조 전제):
//   스토리 그룹 j의 커맨드를 j = 0..현재 라인까지 누적해서 접는다.
//   한 노드가 곧 한 시간표다 — 레인은 하나뿐이다.
//
// ⚠ 이 프로젝트의 yarn 원문에는 #line 태그가 없다(실측: 8개 파일 전부 0건).
//   런타임 라인 ID는 컴파일 시 생성되므로 ID 매칭이 원리적으로 불가능하다.
//   그래서 기본은 전진 순서 커서이고, 태그가 있으면 ID가 이긴다.
//
// ── 측정 프로토콜 (실측으로 갈린 것이라 지킬 것) ────────────────────────────
//
// 【기준】 랩드스킵(좌 Ctrl) 재생 — 폴드 대조는 이 방식으로만 판정한다.
//   모든 커맨드가 즉시 확정되므로 라이브가 곧 정착 상태다.
//   근거는 CommandBase.Execute다: ShouldCompressCommandExecution이면 SkipPolicy를 보고
//   OnSkip으로 빠진다 — 트윈이 아예 만들어지지 않는다.
//
//   ⚠ **0번 라인 함정.** 좌 Ctrl을 재생 시작 뒤에 누르면 첫 라인만 압축 경로를 타지 않고
//   실제 트윈으로 돈다. 그러면 0번 라인에서만 진행 중인 중간값이 캡처된다. 실측 예:
//     lineIndex=0  c3/CharacterPortraitSprite_Root.alpha  접힘=1 vs 캡처=0.9085627
//   **중간 alpha가 찍혔다는 것 자체가 그 라인이 압축되지 않았다는 증거다** —
//   랩드스킵이었다면 0 아니면 1만 나온다. 폴드를 의심하기 전에 이걸 먼저 배제할 것.
//   0번 라인까지 판정하려면 첫 전진 전부터 좌 Ctrl을 누르고 있어야 한다.
//
// 【참고】 라인 단위(HurryUpLine) 재생 — 판정 기준으로 쓰지 말 것.
//   절차적 연기(sway·idle_breathe·jolt·dip·tap)가 실제로 돌 시간이 생기고,
//   트윈이 진행 중인 중간 상태가 캡처된다. 실측 예:
//     CharacterPortrait_SwayPivot.localEulerAngles  캡처=(0,0,7.53) / (0,0,359.62) …
//     CharSlot_Track_Idle.anchoredPosition          캡처=(0, 7.99) …
//     CharacterPortraitSpriteOverlay_Root.alpha     캡처=0.018 ~ 0.207 (크로스페이드 중)
//   값이 매 라인 제각각인 것이 특징이다 — 목표값이 아니라 시간 함수의 스냅샷이라서.
//
//   이 부류는 정지 프레임에 목표가 **정의되지 않는다**. 리듀서가 접을 수 없고
//   Unhandled로 남는 것이 맞다(Documentation~/reduction-boundary.md의 "절차적 커맨드").
//   다만 라인 단위 재생은 **절차적 축이 어디에 있는지 드러내는 용도**로는 유용하다.
// ─────────────────────────────────────────────────────────────────────────────
public sealed class StageEquivalenceHarness : MonoBehaviour
{
    private VNRuntimeStateProvider _provider;
    private PresentationScopeSession _session;
    private PresentationShotResponseSystem _shotSystem;
    private DialogueAdvanceDispatcher _dispatcher;

    private StageReducerTuning _tuning;

    private readonly Dictionary<string, List<YarnLineGroup>> _nodeGroups = new(StringComparer.Ordinal);
    private readonly List<string> _extractWarnings = new();

    // 현재 따라가는 스토리 노드의 시간표.
    private string _storyNode;
    private List<YarnLineGroup> _storyGroups;
    private readonly Dictionary<string, int> _lineIndexById = new(StringComparer.Ordinal);
    private readonly List<StageState> _foldedByLine = new();

    // 태그가 없는 원문에서는 순서 커서로 라인을 따라간다(선형 전진 전제).
    // 롤백/분기가 끼면 대사 검증이 어긋남을 알린다.
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
        public int foldOnlyNodes;
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
        PresentationScopeSession session,
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
                $"[등가성 하네스] 준비 완료. 노드 {_nodeGroups.Count}개 파싱, " +
                $"추출 경고 {_extractWarnings.Count}건. 에피소드를 재생하면 라인마다 판정한다.");
        }
        catch (Exception ex)
        {
            // 하네스가 재생을 막으면 안 된다. 못 서면 조용히 빠지되 이유는 남긴다.
            Debug.LogError($"[등가성 하네스] 초기화 실패 — 하네스 비활성: {ex}");
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
        string tuningDir = Path.Combine(
            Path.GetDirectoryName(Application.dataPath)!, "ExportedTuning");

        RigSchemasFileDto rigSchemas = JsonUtility.FromJson<RigSchemasFileDto>(
            File.ReadAllText(Path.Combine(tuningDir, "rig-schemas.json")));

        BaseResolutionJson baseResolution = JsonUtility.FromJson<BaseResolutionJson>(
            File.ReadAllText(Path.Combine(tuningDir, "base-resolution.json")));

        RoleAnchorTuningFileDto roleAnchors = JsonUtility.FromJson<RoleAnchorTuningFileDto>(
            File.ReadAllText(Path.Combine(tuningDir, "presets", "role-anchor.json")));

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
            RoleAnchors = roleAnchors?.MonoBehaviour,
            DepthPresets = depthTuning?.MonoBehaviour?.presets,
            FocusTuning = focusTuning?.MonoBehaviour,
            PortraitDimensions = LoadPortraitDimensions(tuningDir),
        };
    }

    /// <summary>
    /// 초상 치수는 다른 덤프와 달리 없을 수 있다(exporter 재수출 전 브랜치).
    /// 없으면 초상 사이징 전체가 Unhandled로 잡히므로 **조용히 넘어가지 않고 경고한다.**
    /// </summary>
    private static PortraitDimensionsFileDto LoadPortraitDimensions(string tuningDir)
    {
        string path = Path.Combine(tuningDir, "portrait-dimensions.json");

        if (!File.Exists(path))
        {
            Debug.LogWarning(
                $"[StageEquivalenceHarness] portrait-dimensions.json이 없다 ({path}). " +
                "초상 사이징이 전부 Unhandled가 된다 — Ked/U12/Export Presentation Tuning Dump 실행 필요.");

            return null;
        }

        PortraitDimensionsFileDto dto = JsonUtility.FromJson<PortraitDimensionsFileDto>(
            File.ReadAllText(path));

        if (dto?.entries == null || dto.entries.Count == 0)
        {
            Debug.LogWarning(
                "[StageEquivalenceHarness] portrait-dimensions.json이 비어 있다 — " +
                "PortraitGeneratedDB를 읽지 못했을 수 있다. export-report.json의 경고를 확인할 것.");
        }

        return dto;
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
            Debug.LogWarning($"[등가성 하네스] 스토리 노드 '{storyNode}'의 원문을 못 찾았다.");
            return false;
        }

        _storyNode = storyNode;
        _storyGroups = groups;
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

    /// <summary>0..lineIndex까지 누적해서 접는다. 이미 접은 라인은 재사용한다.</summary>
    private StageState FoldedUpTo(int lineIndex)
    {
        while (_foldedByLine.Count <= lineIndex)
        {
            int j = _foldedByLine.Count;

            StageState state = j == 0
                ? StageReducer.CreateInitialState(_tuning)
                : _foldedByLine[j - 1];

            if (j < _storyGroups.Count)
                state = StageReducer.ApplyAll(state, _storyGroups[j].Commands, _tuning);

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

            // 라인 인덱스: 태그가 있으면 ID 매칭, 없으면(이 프로젝트가 그렇다) 전진 순서 커서.
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
                    $"[등가성 하네스] 라인 커서({lineIndex})가 원문 시간표({_storyGroups.Count}줄)를 넘었다 — " +
                    "선형 전진 전제가 깨졌다(분기/롤백?). 이후 판정을 멈춘다.");

                return;
            }

            VerifyLineTextDrift(lineIndex);

            StageState folded = FoldedUpTo(lineIndex);

            CommandRunScope scope = _session.CurrentScope;

            if (scope == null)
                return;

            StageState captured = StageStateCapture.Capture(
                scope.CharacterRigs, _shotSystem, _tuning.BaseResolution);

            StageStateComparer.Result result = StageStateComparer.Compare(folded, captured);

            LineVerdict verdict = new()
            {
                lineIndex = lineIndex,
                lineId = lineId,
                equivalent = result.IsEquivalent,
                comparedNodes = result.ComparedNodes,
                foldOnlyNodes = result.FoldOnlyNodes,
                unhandledCount = folded.Unhandled.Count,
            };

            verdict.mismatches.AddRange(result.Mismatches);
            _verdicts.Add(verdict);

            if (result.IsEquivalent)
            {
                Debug.Log($"[등가성] 라인 {lineIndex}: 등가 ✓ — {result}");
            }
            else
            {
                Debug.LogWarning(
                    $"[등가성] 라인 {lineIndex}: 불일치 ✗ — {result}\n" +
                    string.Join("\n", result.Mismatches.GetRange(0, Math.Min(10, result.Mismatches.Count))));
            }
        }
        catch (Exception ex)
        {
            // 판정이 재생을 막으면 안 된다.
            Debug.LogError($"[등가성 하네스] 판정 중 예외: {ex}");
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

        if (expected.Contains(actual) || actual.Contains(expected))
            return;

        _timelineDriftWarned = true;

        Debug.LogWarning(
            $"[등가성 하네스] 라인 {lineIndex}의 대사가 원문 시간표와 다르다 — " +
            "시간표 어긋남(분기/롤백/스킵?). 이후 판정의 인덱스를 의심할 것.\n" +
            $"원문: {_storyGroups[lineIndex].LineText}\n표시: {_provider.CurrentLinePreview}");

        static string Normalize(string s)
            => string.IsNullOrEmpty(s) ? null : s.Replace(" ", "").Replace("　", "").Trim();
    }

    // ── 리포트 ───────────────────────────────────────────────────────

    private void WriteReport()
    {
        try
        {
            Report report = new() { storyNode = _storyNode };

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

            // 마지막 라인의 Unhandled가 "아직 못 접는 것"의 전체 목록이다 — 수렴의 작업 목록.
            if (_foldedByLine.Count > 0)
            {
                foreach (UnhandledCommand unhandled in _foldedByLine[^1].Unhandled)
                    report.finalUnhandled.Add(unhandled.ToString());
            }

            string dir = Path.Combine(
                Path.GetDirectoryName(Application.dataPath)!, "EquivalenceReports");

            Directory.CreateDirectory(dir);

            string path = Path.Combine(
                dir,
                $"equivalence-{DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}.json");

            File.WriteAllText(path, JsonUtility.ToJson(report, true));

            Debug.Log($"[등가성 하네스] 리포트: {path} — {report.verdict}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[등가성 하네스] 리포트 쓰기 실패: {ex}");
        }
    }
}
