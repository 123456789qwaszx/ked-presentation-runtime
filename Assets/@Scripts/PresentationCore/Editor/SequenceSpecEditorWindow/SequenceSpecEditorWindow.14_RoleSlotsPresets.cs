#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 목적
/// - RoleKey 슬롯은 최대 9개로 고정(핫키/빠른 주입용)하되,
///   “프리셋(세트)”을 여러 개 저장/로드할 수 있게 해서 사실상 무한 슬롯처럼 쓰게 한다.
/// - 프리셋은 프로젝트 에셋이 아니라 개인 작업 환경(로컬 EditorPrefs)에 저장된다.
///   → 팀 공유가 목적이 아니라, 각자 작업 속도를 올리는 ‘개인 설정’ 성격이다.
///
/// 이 파일이 담당하는 것(Responsibilities)
/// 1) 프리셋 저장소 (EditorPrefs)
/// - 활성 프리셋 이름: CPS.SequenceEditor.RoleSlots.Preset.Active
/// - 프리셋 자동 저장 토글: CPS.SequenceEditor.RoleSlots.Preset.AutoSave
/// - 프리셋 인덱스(이름 목록): CPS.SequenceEditor.RoleSlots.Presets.IndexJson
/// - 개별 프리셋 데이터: CPS.SequenceEditor.RoleSlots.Preset.<PresetName>
///   (EditorPrefs는 키 열거가 불가능하므로, 이름 목록을 별도의 IndexJson으로 관리한다.)
///
/// 2) 프리셋 UI (Preset Bar)
/// - Preset 드롭다운: 프리셋 선택 시 즉시 로드하여 “현재 작업 상태”에 덮어쓴다.
/// - Save: 현재 상태를 활성 프리셋에 덮어쓰기(Overwrite).
/// - Save As: 새 이름으로 프리셋 생성(인라인 입력).
/// - Delete: Default를 제외한 프리셋 삭제.
/// - AutoSave ON: 슬롯/설정 변경이 발생할 때 활성 프리셋을 자동으로 갱신한다.
///   (AutoSave가 켜져 있으면 Save 버튼은 비활성화하는 UX를 권장)
///
/// 3) 프리셋에 저장되는 값(정책)
/// - roleSlotCount: 화면에 표시할 슬롯 개수(최소/최대 clamp)
/// - slots: RoleKey 문자열 목록(항상 RoleSlotMaxCount=9 길이로 정규화)
/// - autoFillIdsOnAdd: Add Command 시 roleKey 자동 채움 사용 여부
/// - autoFillRoleSlotIndex: 자동 채움 대상으로 사용할 슬롯 인덱스
/// - roleApplyScope: “Set RoleKey” 적용 범위(CurrentTrack / AllTracks)
///
/// 주의/확장 포인트
/// - 이 시스템은 ‘개인용 EditorPrefs’ 기반이므로, 프로젝트/팀 공유가 필요하면
///   Export/Import(JSON 파일) 또는 ScriptableObject 프리셋으로 확장하는 것이 좋다.
/// - 프리셋 로드 시에는 현재 작업 상태(EditorPrefs의 RoleSlotsJson/Count 등)도 즉시 갱신하여,
///   “현재 UI에 보이는 값 = 저장된 값”이 항상 일치하도록 유지한다.

public sealed partial class SequenceSpecEditorWindow
{
    // =================================================================================================
    // Role Slots Presets (EditorPrefs)
    // =================================================================================================

    private const string PrefKey_RoleSlotsPresetActive = "CPS.SequenceEditor.RoleSlots.Preset.Active";
    private const string PrefKey_RoleSlotsPresetsIndexJson = "CPS.SequenceEditor.RoleSlots.Presets.IndexJson";

    private const string PrefKey_RoleSlotsPresetKeyPrefix = "CPS.SequenceEditor.RoleSlots.Preset.";

    [SerializeField] private string _roleSlotsPresetActive = "Default";
    
    private const string PrefKey_RoleSlotsPresetAutoSave = "CPS.SequenceEditor.RoleSlots.Preset.AutoSave";

    [SerializeField] private bool _roleSlotsPresetAutoSave = false;

    private static readonly List<string> _defaultPresetList = new List<string> { "Default" };

    [Serializable]
    private sealed class RoleSlotsPresetBox
    {
        public int roleSlotCount = 5;
        public List<string> slots = new();

        public bool autoFillIdsOnAdd = true;
        public int autoFillRoleSlotIndex = 0;

        public int roleApplyScope = 1; // default: AllTracks (see RoleKeyApplyScope)
    }

    [Serializable]
    private sealed class RoleSlotsPresetsIndexBox
    {
        public List<string> names = new();
    }

    // Simple "Save As" input state (inline, no modal)
    [SerializeField] private bool _showPresetSaveAsInline = false;
    [SerializeField] private string _presetSaveAsName = "";

    // -------------------------------------------------------------------------------------------------
    // Public hook: call this inside LoadPreferences() at the end (or after role prefs load)
    // -------------------------------------------------------------------------------------------------
    private void LoadRoleSlotsPresetSystemPrefs()
    {
        _roleSlotsPresetActive = EditorPrefs.GetString(PrefKey_RoleSlotsPresetActive, _roleSlotsPresetActive ?? "Default");
        if (string.IsNullOrWhiteSpace(_roleSlotsPresetActive))
            _roleSlotsPresetActive = "Default";

        _roleSlotsPresetAutoSave = EditorPrefs.GetBool(PrefKey_RoleSlotsPresetAutoSave, _roleSlotsPresetAutoSave);

        var names = LoadPresetsIndexNames();
        if (names.Count == 0)
        {
            names.Add("Default");
            SavePresetsIndexNames(names);
        }

        if (!names.Contains(_roleSlotsPresetActive))
        {
            names.Add(_roleSlotsPresetActive);
            SavePresetsIndexNames(names);
        }
    }

    // -------------------------------------------------------------------------------------------------
    // UI: draw preset bar. Put this above DrawRoleKeySlotsBar() or near your role slots controls.
    // -------------------------------------------------------------------------------------------------
    private void DrawRoleSlotsPresetBar()
    {
        const float boxW = 260f;

        EnsureRoleSlotsCapacity();
        LoadRoleSlotsPresetSystemPrefs_OncePerSession();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(boxW)))
        {
            // Row 1: Preset dropdown + buttons
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Preset", EditorStyles.miniBoldLabel, GUILayout.Width(46f));

                var names = LoadPresetsIndexNames();
                if (names.Count == 0)
                {
                    names.Add("Default");
                    SavePresetsIndexNames(names);
                }

                int curIndex = Mathf.Max(0, names.IndexOf(_roleSlotsPresetActive));
                if (curIndex < 0) curIndex = 0;

                EditorGUI.BeginChangeCheck();
                int nextIndex = EditorGUILayout.Popup(curIndex, names.ToArray(), GUILayout.Width(110f));
                if (EditorGUI.EndChangeCheck())
                {
                    string nextName = names[Mathf.Clamp(nextIndex, 0, names.Count - 1)];
                    if (!string.IsNullOrWhiteSpace(nextName))
                    {
                        _roleSlotsPresetActive = nextName;
                        EditorPrefs.SetString(PrefKey_RoleSlotsPresetActive, _roleSlotsPresetActive);

                        // Load selected preset -> overwrites current state -> saves current working prefs
                        LoadRoleSlotsPreset(_roleSlotsPresetActive);

                        GUI.FocusControl(null);
                        Repaint();
                    }
                }

                GUILayout.FlexibleSpace();

                // Save (overwrite active)
                bool disableSave = _roleSlotsPresetAutoSave || string.IsNullOrWhiteSpace(_roleSlotsPresetActive);

                using (new EditorGUI.DisabledScope(disableSave))
                {
                    var saveContent = new GUIContent(
                        "Save",
                        _roleSlotsPresetAutoSave
                            ? "AutoSave is ON. This preset is saved automatically."
                            : "Overwrite the active preset with current slots."
                    );

                    if (GUILayout.Button(saveContent, GUILayout.Width(44f)))
                    {
                        SaveRoleSlotsPreset(_roleSlotsPresetActive);
                        GUI.FocusControl(null);
                    }
                }

                // Save As (inline name input)
                if (GUILayout.Button(new GUIContent("Save As", "Save current slots as a NEW preset name."), GUILayout.Width(60f)))
                {
                    _showPresetSaveAsInline = !_showPresetSaveAsInline;
                    _presetSaveAsName = "";
                    GUI.FocusControl(null);
                }

                // Delete
                using (new EditorGUI.DisabledScope(!CanDeleteActivePreset()))
                {
                    if (GUILayout.Button(new GUIContent("Del", "Delete the active preset (except Default)."), GUILayout.Width(34f)))
                    {
                        if (EditorUtility.DisplayDialog("Delete Preset",
                                $"Delete preset '{_roleSlotsPresetActive}'?\nThis cannot be undone.",
                                "Delete", "Cancel"))
                        {
                            DeleteRoleSlotsPreset(_roleSlotsPresetActive);
                            GUI.FocusControl(null);
                            Repaint();
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }

            // Row 2: Save As inline input
            if (_showPresetSaveAsInline)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Name", EditorStyles.miniLabel, GUILayout.Width(46f));

                    EditorGUI.BeginChangeCheck();
                    _presetSaveAsName = EditorGUILayout.TextField(_presetSaveAsName ?? "", GUILayout.Width(140f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        // live sanitize preview is fine; final sanitize happens on save
                    }

                    GUILayout.FlexibleSpace();

                    using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_presetSaveAsName)))
                    {
                        if (GUILayout.Button(new GUIContent("OK", "Create preset with this name."), GUILayout.Width(34f)))
                        {
                            string name = SanitizePresetName(_presetSaveAsName);

                            if (string.IsNullOrWhiteSpace(name))
                            {
                                EditorUtility.DisplayDialog("Invalid Name", "Preset name is empty.", "OK");
                            }
                            else
                            {
                                SaveRoleSlotsPreset(name);
                                _roleSlotsPresetActive = name;
                                EditorPrefs.SetString(PrefKey_RoleSlotsPresetActive, _roleSlotsPresetActive);

                                _showPresetSaveAsInline = false;
                                _presetSaveAsName = "";

                                GUI.FocusControl(null);
                                Repaint();
                            }
                        }
                    }

                    if (GUILayout.Button(new GUIContent("Cancel", "Close Save As."), GUILayout.Width(54f)))
                    {
                        _showPresetSaveAsInline = false;
                        _presetSaveAsName = "";
                        GUI.FocusControl(null);
                    }
                }
            }
        }
    }

    // -------------------------------------------------------------------------------------------------
    // Preset operations
    // -------------------------------------------------------------------------------------------------

    private void SaveRoleSlotsPreset(string presetNameRaw)
    {
        EnsureRoleSlotsCapacity();

        string presetName = SanitizePresetName(presetNameRaw);
        if (string.IsNullOrWhiteSpace(presetName))
            return;

        // Write preset box from current editor state
        var box = new RoleSlotsPresetBox
        {
            roleSlotCount = Mathf.Clamp(_roleSlotCount, RoleSlotBaseCount, RoleSlotMaxCount),
            slots = new List<string>(_roleKeySlots ?? new List<string>()),

            autoFillIdsOnAdd = _autoFillIdsOnAdd,
            autoFillRoleSlotIndex = Mathf.Clamp(_autoFillRoleSlotIndex, 0, Mathf.Max(0, RoleSlotMaxCount - 1)),
            roleApplyScope = (int)_roleApplyScope
        };

        // Normalize slots to max count (9)
        NormalizeSlotsList(ref box.slots);

        // Clamp auto index to visible slot count
        box.autoFillRoleSlotIndex = Mathf.Clamp(box.autoFillRoleSlotIndex, 0, Mathf.Max(0, box.roleSlotCount - 1));

        string json = JsonUtility.ToJson(box);
        EditorPrefs.SetString(GetPresetKey(presetName), json);

        // Ensure name exists in index
        var names = LoadPresetsIndexNames();
        if (!names.Contains(presetName))
        {
            names.Add(presetName);
            SavePresetsIndexNames(names);
        }
    }

    private void LoadRoleSlotsPreset(string presetNameRaw)
    {
        string presetName = SanitizePresetName(presetNameRaw);
        if (string.IsNullOrWhiteSpace(presetName))
            return;

        string key = GetPresetKey(presetName);
        string json = EditorPrefs.GetString(key, "");

        if (string.IsNullOrEmpty(json))
        {
            // If preset not found but requested, do nothing (or fallback)
            return;
        }

        RoleSlotsPresetBox box;
        try
        {
            box = JsonUtility.FromJson<RoleSlotsPresetBox>(json);
        }
        catch
        {
            return;
        }

        if (box == null) return;

        // Apply to current state
        _roleSlotCount = Mathf.Clamp(box.roleSlotCount, RoleSlotBaseCount, RoleSlotMaxCount);

        _roleKeySlots = box.slots ?? new List<string>();
        NormalizeSlotsList(ref _roleKeySlots);

        _autoFillIdsOnAdd = box.autoFillIdsOnAdd;
        _autoFillRoleSlotIndex = Mathf.Clamp(box.autoFillRoleSlotIndex, 0, Mathf.Max(0, _roleSlotCount - 1));

        // Scope is an int in preset for forward-compat
        _roleApplyScope = (RoleKeyApplyScope)Mathf.Clamp(box.roleApplyScope, 0, 1);

        // Persist to working prefs immediately (so user state matches loaded preset)
        EditorPrefs.SetBool(PrefKey_AutoFillOnAdd, _autoFillIdsOnAdd);
        EditorPrefs.SetInt(PrefKey_AutoFillRoleSlotIndex, _autoFillRoleSlotIndex);
        EditorPrefs.SetInt(PrefKey_RoleApplyScope, (int)_roleApplyScope);

        SaveRoleSlotsPrefs(); // writes count + slots json
    }

    private void DeleteRoleSlotsPreset(string presetNameRaw)
    {
        string presetName = SanitizePresetName(presetNameRaw);
        if (string.IsNullOrWhiteSpace(presetName))
            return;

        if (string.Equals(presetName, "Default", StringComparison.OrdinalIgnoreCase))
            return;

        // Remove from index
        var names = LoadPresetsIndexNames();
        names.RemoveAll(n => string.Equals(n, presetName, StringComparison.OrdinalIgnoreCase));
        if (names.Count == 0)
            names.Add("Default");
        SavePresetsIndexNames(names);

        // Delete preset value (EditorPrefs doesn't have DeleteString; use DeleteKey)
        EditorPrefs.DeleteKey(GetPresetKey(presetName));

        // If active deleted -> fallback to Default (and attempt load)
        if (string.Equals(_roleSlotsPresetActive, presetName, StringComparison.OrdinalIgnoreCase))
        {
            _roleSlotsPresetActive = "Default";
            EditorPrefs.SetString(PrefKey_RoleSlotsPresetActive, _roleSlotsPresetActive);

            // If Default exists, load it; otherwise keep current
            LoadRoleSlotsPreset(_roleSlotsPresetActive);
        }
    }

    private bool CanDeleteActivePreset()
    {
        if (string.IsNullOrWhiteSpace(_roleSlotsPresetActive)) return false;
        return !string.Equals(_roleSlotsPresetActive, "Default", StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------------------------------
    // Preset index
    // -------------------------------------------------------------------------------------------------

    private List<string> LoadPresetsIndexNames()
    {
        string json = EditorPrefs.GetString(PrefKey_RoleSlotsPresetsIndexJson, "");
        if (string.IsNullOrEmpty(json))
        {
            return new List<string>(_defaultPresetList);
        }

        try
        {
            var box = JsonUtility.FromJson<RoleSlotsPresetsIndexBox>(json);
            var list = box?.names ?? new List<string>();

            // Normalize: remove empties, sanitize, unique
            var outList = new List<string>();
            for (int i = 0; i < list.Count; i++)
            {
                string n = SanitizePresetName(list[i]);
                if (string.IsNullOrWhiteSpace(n)) continue;
                if (!outList.Contains(n)) outList.Add(n);
            }

            if (outList.Count == 0)
            {
                outList.Add("Default");
            }
            
            return outList;
        }
        catch
        {
            return new List<string>(_defaultPresetList);
        }
    }

    private void SavePresetsIndexNames(List<string> names)
    {
        if (names == null)
            names = new List<string>();

        // sanitize + unique
        var outList = new List<string>();
        for (int i = 0; i < names.Count; i++)
        {
            string n = SanitizePresetName(names[i]);
            if (string.IsNullOrWhiteSpace(n)) continue;
            if (!outList.Contains(n)) outList.Add(n);
        }

        if (outList.Count == 0) 
            outList.Add("Default");

        var box = new RoleSlotsPresetsIndexBox { names = outList };
        EditorPrefs.SetString(PrefKey_RoleSlotsPresetsIndexJson, JsonUtility.ToJson(box));
    }

    private static string GetPresetKey(string presetName)
    {
        return PrefKey_RoleSlotsPresetKeyPrefix + presetName;
    }

    private static string SanitizePresetName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";

        string s = raw.Trim();

        // Keep it simple and EditorPrefs-safe:
        // - remove control chars
        // - disallow '.' to avoid confusion with prefix structure
        // - allow spaces, but collapse multiple spaces
        s = s.Replace(".", "_");
        s = s.Replace("/", "_");
        s = s.Replace("\\", "_");
        s = s.Replace(":", "_");
        s = s.Replace("|", "_");

        // Remove leading/trailing underscores caused by replacements
        s = s.Trim().Trim('_');

        // Hard cap length (optional)
        const int maxLen = 32;
        if (s.Length > maxLen)
            s = s.Substring(0, maxLen);

        if (string.IsNullOrWhiteSpace(s))
            return "";

        return s;
    }

    private static void NormalizeSlotsList(ref List<string> list)
    {
        if (list == null) list = new List<string>();

        while (list.Count < RoleSlotMaxCount)
            list.Add("");

        if (list.Count > RoleSlotMaxCount)
            list.RemoveRange(RoleSlotMaxCount, list.Count - RoleSlotMaxCount);
    }

    // -------------------------------------------------------------------------------------------------
    // One-time init guard (prevents repeated index normalization per repaint)
    // -------------------------------------------------------------------------------------------------
    private bool _didInitRolePresetSystem = false;

    private void LoadRoleSlotsPresetSystemPrefs_OncePerSession()
    {
        if (_didInitRolePresetSystem) return;
        _didInitRolePresetSystem = true;
        LoadRoleSlotsPresetSystemPrefs();
    }
}
#endif