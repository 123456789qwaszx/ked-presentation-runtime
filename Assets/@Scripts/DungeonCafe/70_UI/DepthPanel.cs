using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

/// <summary>
/// 붕괴심층 패널. (v3 §4)
///
/// 심층 한 비트에서 세 국면을 같은 화면이 담당한다:
/// 1) 개입 - 굴림 전 능력 토글 후 [주사위를 굴린다]
/// 2) 굴림 제시 - 결과를 보여주고 재굴림/구간 하향 능력 또는 [받아들인다]
/// 3) 회수 선택 - [지금 데리고 나온다](탈출 x0.5) / [한 번 더 남긴다]
///
/// 여기가 게임에서 가장 극적인 화면이다. 구간표(회수/위험/치명/특수)는
/// 공개 조건(고도 파악/능력/가게 Lv5)을 만족할 때만 수치가 보인다.
/// </summary>
public sealed class DepthPanel : UIPanel<DepthPanel.Refs>, IManagedUI
{
    /// <summary>개입 확정: 굴림 전 사용할 능력 id 목록.</summary>
    public event Action<IReadOnlyList<string>> OnInterventionConfirmed;
    /// <summary>굴림 제시 결정: 사용할 능력 id (null = 받아들인다). 재굴림/하향 판별은 바인딩이 한다.</summary>
    public event Action<string> OnRollDecided;
    /// <summary>회수 선택: true = 지금 탈출.</summary>
    public event Action<bool> OnRecoveryChosen;

    #region Refs
    public enum Refs
    {
        DepthBG_Root,
        DepthBG_Image,

        Header_Text,
        Band_Text,
        Roll_Text,

        DepthList_Root,
        DepthList_Content,
        DepthPrefab,
    }

    private Image _bgImage;
    private TMP_Text _headerText;
    private TMP_Text _bandText;
    private TMP_Text _rollText;
    private RectTransform _content;

    [SerializeField] private VNOptionItem _depthPrefab;

    private readonly DungeonCafeOptionItemList _list = new();
    private readonly List<DungeonCafeOptionEntry> _entries = new();
    private readonly List<Action> _handlers = new();

    private DepthInterventionRequest _intervention;
    private readonly HashSet<string> _toggled = new(StringComparer.Ordinal);

    private bool _valid;
    private bool _locked;
    #endregion

    protected override void OnInitialize()
    {
        _bgImage = View.Image(Refs.DepthBG_Image);
        _headerText = View.Text(Refs.Header_Text);
        _bandText = View.Text(Refs.Band_Text);
        _rollText = View.Text(Refs.Roll_Text);
        _content = View.Rect(Refs.DepthList_Content);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif

        _list.Configure(_depthPrefab, _content);

        _list.OnSubmitted -= HandleSubmitted;
        _list.OnSubmitted += HandleSubmitted;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        _list.OnSubmitted -= HandleSubmitted;
        _list.Clear();
    }

    // ------------------------------------------------------------
    // 국면 1: 개입
    // ------------------------------------------------------------
    public void PresentIntervention(DepthInterventionRequest request)
    {
        if (!_valid || request == null)
            return;

        _intervention = request;
        _toggled.Clear();
        _locked = false;

        ApplyHeader(request.Session, request.DepthBeatIndex);
        ApplyLayout(request.Layout, request.LayoutRevealed, request.PredictedBand);

        if (_rollText != null)
            _rollText.text = string.Empty;

        RebuildIntervention();
        _list.SetLocked(false);
    }

    private void RebuildIntervention()
    {
        BeginEntries();

        AddEntry("주사위를 굴린다", () =>
        {
            Lock();
            OnInterventionConfirmed?.Invoke(new List<string>(_toggled));
        });

        for (int i = 0; i < _intervention.AvailableAbilityIds.Count; i++)
        {
            string id = _intervention.AvailableAbilityIds[i];
            bool on = _toggled.Contains(id);
            AddEntry($"{(on ? "(O)" : "(X)")} 능력: {id}{(on ? "  (사용 예약)" : string.Empty)}", () =>
            {
                if (!_toggled.Add(id)) _toggled.Remove(id);
                RebuildIntervention();
            });
        }

        _list.Rebuild(_entries);
    }

    // ------------------------------------------------------------
    // 국면 2: 굴림 제시
    // ------------------------------------------------------------
    public void PresentRoll(
        ServiceSessionState session, in DepthRollResult roll, IReadOnlyList<string> postRollAbilityIds)
    {
        if (!_valid || session == null)
            return;

        _locked = false;

        ApplyHeader(session, session.DepthBeatCount);

        if (_rollText != null)
            _rollText.text =
                $"기본 {roll.BaseRoll}  보정 {roll.ClampedModifierSum:+0;-0;+0}" +
                (roll.WasModifierClamped ? " (클램프 ±30)" : string.Empty) +
                $"\n최종 {roll.FinalValue}  ->  {ToBandLabel(roll.Band)}" +
                (roll.CollapseGain > 0 ? $"  붕괴 +{roll.CollapseGain}" : string.Empty);

        BeginEntries();

        AddEntry(" 받아들인다", () =>
        {
            Lock();
            OnRollDecided?.Invoke(null);
        });

        if (postRollAbilityIds != null)
        {
            for (int i = 0; i < postRollAbilityIds.Count; i++)
            {
                string id = postRollAbilityIds[i];
                AddEntry($"능력 사용: {id}", () =>
                {
                    Lock();
                    OnRollDecided?.Invoke(id);
                });
            }
        }

        _list.Rebuild(_entries);
        _list.SetLocked(false);
    }

    // ------------------------------------------------------------
    // 국면 3: 회수 선택
    // ------------------------------------------------------------
    public void PresentRecoveryChoice(ServiceSessionState session)
    {
        if (!_valid || session == null)
            return;

        _locked = false;

        ApplyHeader(session, session.DepthBeatCount);

        if (_rollText != null)
            _rollText.text = "회수 구간 - 손을 뻗을 수 있습니다.";

        BeginEntries();

        AddEntry($"지금 데리고 나온다\n결산 x0.5 / 게이지 99 회수", () =>
        {
            Lock();
            OnRecoveryChosen?.Invoke(true);
        });

        AddEntry($" 한 번 더 남긴다\n반응은 계속 쌓이지만, 다음 굴림은 보장되지 않는다", () =>
        {
            Lock();
            OnRecoveryChosen?.Invoke(false);
        });

        _list.Rebuild(_entries);
        _list.SetLocked(false);
    }

    // ------------------------------------------------------------
    // 공통
    // ------------------------------------------------------------
    private void ApplyHeader(ServiceSessionState session, int depthBeat)
    {
        if (_headerText == null)
            return;

        _headerText.text =
            $"붕괴심층 - {session.Maid.DisplayName} / {session.Monster.DisplayName}\n" +
            $"{depthBeat}번째 굴림 / {BurdenAxes.ToBurdenLabel(session.DepthAxis)} " +
            $"{session.Maid.Gauge.Get(session.DepthAxis)} / 200";
    }

    private void ApplyLayout(in DepthBandLayout layout, bool revealed, DepthBand? predicted)
    {
        if (_bandText == null)
            return;

        string prediction = predicted.HasValue ? $"\n징후: {ToBandLabel(predicted.Value)} 구간이 짙다" : string.Empty;

        _bandText.text = revealed
            ? $"회수 1~{layout.RecoveryMax} / 위험 ~{layout.RiskyMax} / 치명 ~{layout.FatalMax} / 특수 ~99{prediction}"
            : $"결과표 비공개 - 고도 파악/능력/가게 Lv5 로 열린다{prediction}";
    }

    private void BeginEntries()
    {
        _entries.Clear();
        _handlers.Clear();
    }

    private void AddEntry(string label, Action handler)
    {
        _entries.Add(new DungeonCafeOptionEntry(label));
        _handlers.Add(handler);
    }

    private void Lock()
    {
        _locked = true;
        _list.SetLocked(true);
    }

    private void HandleSubmitted(int index)
    {
        if (_locked || index < 0 || index >= _handlers.Count)
            return;

        _handlers[index]?.Invoke();
    }

    private static string ToBandLabel(DepthBand band) => band switch
    {
        DepthBand.Recovery => "회수",
        DepthBand.Risky => "위험",
        DepthBand.Fatal => "치명",
        DepthBand.Special => "특수",
        _ => band.ToString(),
    };

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _bgImage, Refs.DepthBG_Image);
        AppendMissing(ref missing, _headerText, Refs.Header_Text);
        AppendMissing(ref missing, _content, Refs.DepthList_Content);
        AppendMissing(ref missing, _depthPrefab, Refs.DepthPrefab);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[DepthPanel] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}
