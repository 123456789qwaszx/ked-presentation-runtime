#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Command 삽입/추가 메뉴/타입 생성 로직.
/// 
/// 이 파트는 “현재 Step의 활성 Track(Interaction/Setup/Motion/Dialogue/FX) 커맨드 리스트”에
/// 새 커맨드를 끼워 넣는 표준 경로를 담당한다.
/// (즉, +Command 버튼/우클릭 메뉴/클립보드 붙여넣기/배치 추가 등 “삽입”의 최종 도착지)
///
/// 핵심 아이디어:
/// - SerializedProperty 기반([SerializeReference] List) 편집은 즉시 수정 대신 DelayModify로 래핑한다.
///   → Undo/Dirty/Compile/Repaint를 한 번에 일관되게 처리하고, GUI 이벤트 충돌을 줄인다.
/// - 삽입 전/후에 Foldout 상태를 Snapshot/Restore 해서,
///   리스트 변경(Insert/Delete/Reorder)에도 기존 커맨드들의 펼침 상태가 최대한 유지되도록 한다.
/// - 새로 추가된 커맨드는 기본적으로 접힌 상태(false)로 시작하며,
///   meta.track을 “삽입 당시 활성 트랙”으로 정규화한다.
/// - (옵션) Auto-fill(Role) 정책에 따라 roleKey를 자동 주입한다.
/// 
/// Responsibilities:
/// 1) InsertSingleAt / InsertBatchAt
///    - 지정 위치(insertAt)에 커맨드 1개/여러 개를 Insert
///    - managedReferenceValue에 새 인스턴스를 할당
///    - 메타(track) 정규화 + 디폴트 메타 적용 + roleKey 자동 채움(옵션)
///    - foldout snapshot/restore + 신규 항목 foldout 기본값(접힘) 적용
///    - UI 선택/스크롤 예약(_pendingCommandIndex, _scrollToNewCommand) 설정
///
/// 2) InsertCommandFactoryAt
///    - 외부에서 “생성 함수(factory)”로 커맨드 인스턴스를 공급받아 Insert
///      (예: 클립보드 JSON → CommandSpecBase 생성 후 삽입)
///    - expandNew로 신규 항목을 펼칠지 여부를 제어
///
/// 3) ShowCommandAddMenu
///    - 커맨드 타입 목록을 캐시하고, 메뉴 훅(SequenceEditorMenuHooks)이 있으면 우선 위임
///    - 훅이 없으면 GenericMenu로 폴백
///    - extendMenu로 “Delete” 같은 추가 메뉴를 호출자 측에서 확장 가능
///
/// 4) CreateCommandInstance / Auto-fill(Role)
///    - Activator.CreateInstance로 CommandSpecBase 파생 타입을 생성
///    - CommandMetaDefaults.GetDefault(t)로 기본 메타를 주입(실패해도 무시)
///    - _autoFillIdsOnAdd가 켜져 있으면 현재 Role 슬롯의 roleKey를 자동 주입
///
/// 5) 메타 동기화(중요)
///    - NormalizeInsertedCommandMeta: SerializedProperty 상의 meta.track 값을 강제로 targetTrack으로 맞춤
///    - SyncMetaAfterInsert: 실제 managedReferenceValue(=CommandSpecBase)에도 Editor_SetMeta로 동기화
///      → “프로퍼티 상의 meta”와 “객체 내부 meta”가 어긋나지 않게 하는 안전장치
///
/// Look here when you want to change:
/// - “어디에 삽입하나?” 정책: insertAt 계산 방식, 선택/스크롤 정책(_pendingCommandIndex/_scrollToNewCommand)
/// - “새 커맨드 기본 상태”: 신규 foldout 기본값(접힘/펼침), expandNew/scroll 옵션의 의미
/// - “기본 메타/트랙 정책”: 어떤 트랙으로 들어갈지, 메타 초기화 방식(Editor_SetMeta / defaults)
/// - “Auto-fill(Role) 정책”: roleKey 슬롯 개수/선택 방식, screenId 등 다른 필드 자동 채움 확장
/// - “메뉴 구성”: 훅 우선순위, 폴백 메뉴 항목, batch preset 제공 방식
/// </summary>
public sealed partial class SequenceSpecEditorWindow
{
    private void InsertSingleAt(string commandsPath, int insertAt, Type t, bool scroll)
    {
        DelayModify("Add Command", so =>
        {
            var fresh = so.FindProperty(commandsPath);
            if (fresh == null || !fresh.isArray)
                return;

            var map = GetFoldoutMap(commandsPath);
            var foldouts = SnapshotCommandFoldouts(fresh);

            int idx = Mathf.Clamp(insertAt, 0, fresh.arraySize);
            fresh.InsertArrayElementAtIndex(idx);

            var el = fresh.GetArrayElementAtIndex(idx);
            el.managedReferenceValue = CreateCommandInstance(t);

            long newId = el.managedReferenceId;

            RestoreCommandFoldouts(fresh, foldouts, newIdToCollapse: -1);

            el.isExpanded = false;
            if (map != null && newId != 0)
                map[newId] = false;

            _pendingCommandIndex = idx;
            _commandsList = null;
        });
    }

    private void InsertBatchAt(string commandsPath, int insertAt, IReadOnlyList<Type> types, bool scroll)
    {
        if (types == null || types.Count == 0)
            return;

        DelayModify("Add Command Set", so =>
        {
            var fresh = so.FindProperty(commandsPath);
            if (fresh == null || !fresh.isArray)
                return;

            var map = GetFoldoutMap(commandsPath);
            var foldouts = SnapshotCommandFoldouts(fresh);

            int baseIdx = Mathf.Clamp(insertAt, 0, fresh.arraySize);
            var newIds = new List<long>(types.Count);

            for (int i = 0; i < types.Count; i++)
            {
                int idx = baseIdx + i;
                fresh.InsertArrayElementAtIndex(idx);

                var el = fresh.GetArrayElementAtIndex(idx);
                el.managedReferenceValue = CreateCommandInstance(types[i]);
                el.isExpanded = false;

                long id = el.managedReferenceId;
                if (id != 0)
                    newIds.Add(id);
            }

            RestoreCommandFoldouts(fresh, foldouts, newIdToCollapse: -1);

            if (map != null)
            {
                for (int i = 0; i < newIds.Count; i++)
                    map[newIds[i]] = false;
            }

            _pendingCommandIndex = baseIdx;
            _commandsList = null;
        });
    }

    private void InsertCommandFactoryAt(
        string commandsPath,
        int insertAt,
        Func<CommandSpecBase> factory,
        bool scroll,
        bool expandNew)
    {
        DelayModify("Insert Command", so =>
        {
            var fresh = so.FindProperty(commandsPath);
            if (fresh == null || !fresh.isArray)
                return;

            var foldouts = SnapshotCommandFoldouts(fresh);

            int idx = Mathf.Clamp(insertAt, 0, fresh.arraySize);
            fresh.InsertArrayElementAtIndex(idx);

            var el = fresh.GetArrayElementAtIndex(idx);
            el.managedReferenceValue = factory?.Invoke();

            RestoreCommandFoldouts(fresh, foldouts, newIdToCollapse: -1);

            el.isExpanded = expandNew;

            _pendingCommandIndex = idx;
            _commandsList = null;
        });
    }

    private void ShowCommandAddMenu(
        string commandsPath,
        int insertAt,
        Action<Type> onSingle,
        Action<IReadOnlyList<Type>> onBatch,
        Action<GenericMenu> extendMenu = null)
    {
        CacheCommandTypes();

        bool handled = SequenceEditorMenuHooks.TryShowCommandMenu(
            commandTypes: _cachedCommandTypes,
            onAddSingleRequested: onSingle,
            onAddBatchRequested: onBatch,
            extendMenu: menu => { extendMenu?.Invoke(menu); });

        if (handled)
            return;

        var fallback = new GenericMenu();

        if (_cachedCommandTypes == null || _cachedCommandTypes.Count == 0)
        {
            fallback.AddDisabledItem(new GUIContent("No command types found"));
        }
        else
        {
            foreach (var t in _cachedCommandTypes)
            {
                var tt = t;
                fallback.AddItem(new GUIContent(tt.Name), false, () => onSingle(tt));
            }
        }

        extendMenu?.Invoke(fallback);
        fallback.ShowAsContext();
    }

    private CommandSpecBase CreateCommandInstance(Type t)
    {
        var inst = (CommandSpecBase)Activator.CreateInstance(t);

        if (_autoFillIdsOnAdd && inst != null)
        {
            string key = GetAutoFillRoleKey();
            if (!string.IsNullOrWhiteSpace(key))
                ApplyAutoFillRoleSlotKey(inst, key);
        }

        return inst;
    }

    private static void ApplyAutoFillRoleSlotKey(CommandSpecBase inst, string key)
    {
        switch (inst)
        {
            case CharacterRigCommandSpecBase charRigSpec:
                charRigSpec.targetKey = key;
                break;

            case SetupCharRigCommandSpec setupSpec:
                setupSpec.roleKey = key;
                break;

            case CastCharacterCommandSpec castSpec:
                castSpec.slotKey = key;
                break;
        }
    }

    private string GetAutoFillRoleKey()
    {
        int idx = Mathf.Clamp(_autoFillRoleSlotIndex, 0, 4);
        string s = _roleKeySlots[idx];
        return s ?? string.Empty;
    }
}
#endif
