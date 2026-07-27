using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

/// <summary>
/// 상시 표시 오버레이.
///
/// 패널과 달리 노드가 재생되는 동안에도 화면에 남는다.
/// UIManager 는 오버레이를 interactable=false, blocksRaycasts=false 로 올리므로
/// 대사 진행 입력을 가로채지 않는다. 여기에 버튼을 두어서는 안 된다.
///
/// 값은 GuesthouseHudSnapshot(v3) 복사본으로만 받는다. 진행 상태 객체를 붙들지 않는다.
/// </summary>
public sealed class GuesthouseStatusOverlay : UIOverlay<GuesthouseStatusOverlay.Refs>
{
    #region Refs
    public enum Refs
    {
        StatusBG_Root,
        StatusBG_Image,

        Progress_Root,
        Progress_Day_Text,
        Progress_Slot_Text,
        Progress_Phase_Text,

        Energy_Root,
        Energy_Value_Text,
        Energy_Gauge_Image,

        Maid_Root,
        Maid_Name_Text,
        Maid_Control_Text,
        Maid_Physical_Text,
        Maid_Physical_Gauge_Image,
        Maid_Mental_Text,
        Maid_Mental_Gauge_Image,
        Maid_Empathic_Text,
        Maid_Empathic_Gauge_Image,

        Monster_Root,
        Monster_Name_Text,
        Monster_Demand_Text,
        Monster_Satisfaction_Text,
        Monster_Satisfaction_Gauge_Image,
    }

    private Image _bgImage;

    private TMP_Text _dayText;
    private TMP_Text _slotText;
    private TMP_Text _phaseText;

    private TMP_Text _energyText;
    private Image _energyGauge;

    private GameObject _maidRoot;
    private TMP_Text _maidNameText;
    private TMP_Text _controlText;

    private TMP_Text _physicalText;
    private Image _physicalGauge;
    private TMP_Text _mentalText;
    private Image _mentalGauge;
    private TMP_Text _empathicText;
    private Image _empathicGauge;

    private GameObject _monsterRoot;
    private TMP_Text _monsterNameText;
    private TMP_Text _demandText;
    private TMP_Text _satisfactionText;
    private Image _satisfactionGauge;

    private bool _valid;
    #endregion

    protected override void OnInitialize()
    {
        _bgImage = View.Image(Refs.StatusBG_Image);

        _dayText = View.Text(Refs.Progress_Day_Text);
        _slotText = View.Text(Refs.Progress_Slot_Text);
        _phaseText = View.Text(Refs.Progress_Phase_Text);

        _energyText = View.Text(Refs.Energy_Value_Text);
        _energyGauge = View.Image(Refs.Energy_Gauge_Image);

        RectTransform maidRoot = View.Rect(Refs.Maid_Root);
        _maidRoot = maidRoot != null ? maidRoot.gameObject : null;

        _maidNameText = View.Text(Refs.Maid_Name_Text);
        _controlText = View.Text(Refs.Maid_Control_Text);

        _physicalText = View.Text(Refs.Maid_Physical_Text);
        _physicalGauge = View.Image(Refs.Maid_Physical_Gauge_Image);
        _mentalText = View.Text(Refs.Maid_Mental_Text);
        _mentalGauge = View.Image(Refs.Maid_Mental_Gauge_Image);
        _empathicText = View.Text(Refs.Maid_Empathic_Text);
        _empathicGauge = View.Image(Refs.Maid_Empathic_Gauge_Image);

        RectTransform monsterRoot = View.Rect(Refs.Monster_Root);
        _monsterRoot = monsterRoot != null ? monsterRoot.gameObject : null;

        _monsterNameText = View.Text(Refs.Monster_Name_Text);
        _demandText = View.Text(Refs.Monster_Demand_Text);
        _satisfactionText = View.Text(Refs.Monster_Satisfaction_Text);
        _satisfactionGauge = View.Image(Refs.Monster_Satisfaction_Gauge_Image);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
#else
        _valid = true;
#endif
    }

    /// <summary>노드 재생 직전에 호출된다. 여기서 무거운 작업을 하면 대사 시작이 밀린다.</summary>
    public void Apply(in GuesthouseHudSnapshot snapshot)
    {
        if (!_valid)
            return;

        ApplyProgress(snapshot);
        ApplyEnergy(snapshot);
        ApplyMaid(snapshot);
        ApplyMonster(snapshot);
    }

    private void ApplyProgress(in GuesthouseHudSnapshot snapshot)
    {
        if (_dayText != null)
            _dayText.text = $"{snapshot.DayNumber} / {snapshot.DayCount}일차";

        if (_slotText != null)
            _slotText.text = $"접객 {Mathf.Min(snapshot.SlotIndex + 1, snapshot.SlotCount)} / {snapshot.SlotCount}";

        if (_phaseText != null)
            _phaseText.text = snapshot.PhaseLabel ?? string.Empty;
    }

    private void ApplyEnergy(in GuesthouseHudSnapshot snapshot)
    {
        // v3 3장부: 할당 판정은 [오늘] 장부만 본다. 게이지도 오늘/할당이다.
        if (_energyText != null)
            _energyText.text =
                $"욕구 {snapshot.EnergyToday} / {snapshot.EnergyQuota}  (보유 {snapshot.EnergyHeld} / 누적 {snapshot.EnergyLifetime} / 가게 Lv{snapshot.ShopLevel})";

        SetGauge(_energyGauge, snapshot.EnergyToday, snapshot.EnergyQuota);
    }

    private void ApplyMaid(in GuesthouseHudSnapshot snapshot)
    {
        if (_maidRoot != null)
            _maidRoot.SetActive(snapshot.HasMaid);

        if (!snapshot.HasMaid)
            return;

        if (_maidNameText != null)
            _maidNameText.text = snapshot.MaidName;

        if (_controlText != null)
            _controlText.text = snapshot.ControlLabel;

        ApplyAxis(_physicalText, _physicalGauge, snapshot, BurdenAxis.Physical);
        ApplyAxis(_mentalText, _mentalGauge, snapshot, BurdenAxis.Mental);
        ApplyAxis(_empathicText, _empathicGauge, snapshot, BurdenAxis.Empathic);
    }

    private static void ApplyAxis(
        TMP_Text target,
        Image gauge,
        in GuesthouseHudSnapshot snapshot,
        BurdenAxis axis)
    {
        // v3: 0~200 단일 스케일. 100(통제 상실)/200(완전 붕괴) 눈금은 프리팹의 마커가 담당한다.
        int value = snapshot.Gauge[axis];
        int max = snapshot.TotalCollapseThreshold;

        if (target != null)
            target.text = $"{BurdenAxes.ToBurdenLabel(axis)} {value} / {max}";

        SetGauge(gauge, value, max);
    }

    private void ApplyMonster(in GuesthouseHudSnapshot snapshot)
    {
        if (_monsterRoot != null)
            _monsterRoot.SetActive(snapshot.HasMonster);

        if (!snapshot.HasMonster)
            return;

        if (_monsterNameText != null)
            _monsterNameText.text = snapshot.MonsterName;

        if (_demandText != null)
        {
            _demandText.text = snapshot.IsDemandKnown
                ? $"요구 유형: {BurdenAxes.ToAptitudeLabel(snapshot.DemandAxis)}"
                : "요구 유형: 미확인";
        }

        if (_satisfactionText != null)
            _satisfactionText.text = $"만족도 {snapshot.Satisfaction} / {snapshot.RequiredSatisfaction}";

        SetGauge(_satisfactionGauge, snapshot.Satisfaction, snapshot.RequiredSatisfaction);
    }

    /// <summary>Image 가 Filled 타입일 때만 채움값이 보인다. 아니면 조용히 무시된다.</summary>
    private static void SetGauge(Image gauge, int value, int max)
    {
        if (gauge == null)
            return;

        gauge.fillAmount = max <= 0 ? 0f : Mathf.Clamp01((float)value / max);
    }

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _bgImage, Refs.StatusBG_Image);
        AppendMissing(ref missing, _dayText, Refs.Progress_Day_Text);
        AppendMissing(ref missing, _energyText, Refs.Energy_Value_Text);
        AppendMissing(ref missing, _maidNameText, Refs.Maid_Name_Text);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[GuesthouseStatusOverlay] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}
