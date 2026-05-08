using System;
using System.Collections.Generic;

[Serializable]
public sealed class VNGlobalProgressData
{
    public List<string> unlockedCgKeys = new List<string>();
    public List<string> unlockedEndingKeys = new List<string>();
    public List<string> readLineIds = new List<string>();

    /// <summary>
    /// Continue 버튼이 최종적으로 참조하는 슬롯.
    /// 추천 정책: 안전한 AutoSave도 Continue 대상으로 갱신한다.
    /// </summary>
    public string continueSlotId = "";

    public string latestManualSlotId = "";
    public string latestAutoSlotId = "";

    public VNSettingsData settings = new VNSettingsData();

    public void Normalize()
    {
        if (unlockedCgKeys == null) unlockedCgKeys = new List<string>();
        if (unlockedEndingKeys == null) unlockedEndingKeys = new List<string>();
        if (readLineIds == null) readLineIds = new List<string>();

        if (continueSlotId == null) continueSlotId = "";
        if (latestManualSlotId == null) latestManualSlotId = "";
        if (latestAutoSlotId == null) latestAutoSlotId = "";

        if (settings == null) settings = new VNSettingsData();
    }
}

[Serializable]
public sealed class VNSettingsData
{
    public float masterVolume = 1f;
    public float bgmVolume = 1f;
    public float sfxVolume = 1f;
    public float voiceVolume = 1f;

    public float textSpeed = 1f;

    public bool autoMode = false;
    public float autoAdvanceDelay = 0.6f;

    /// <summary>
    /// true면 이미 읽은 라인만 스킵한다.
    /// false면 프로젝트 정책에 따라 읽지 않은 라인 스킵도 허용할 수 있다.
    /// </summary>
    public bool skipOnlyReadLines = true;

    public bool allowSkipUnread = false;

    public bool fullscreen = true;
    public int resolutionIndex = 0;
}