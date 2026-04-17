#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// (Editor) SequenceSpec 편집기의 "데이터 변경(CRUD) + 안전한 적용 파이프라인" 파트.
/// 이 partial은 UI를 그리는 쪽이 아니라, **노드/스텝/커맨드의 추가·삭제 같은 실제 데이터 변형**을 담당한다.
/// 핵심 목적은 3가지:
/// 1) Undo/Dirty/SerializedObject 적용을 일관되게 처리한다.
/// 2) ReorderableList/선택 상태/스크롤·네비게이션 상태를 “변경 후”에 자연스럽게 재동기화한다.
/// 3) SerializeReference(ManagedReference) 커맨드 리스트의 삭제/정리(두 번 Delete 필요 케이스 포함)와
///    foldout 상태 스냅샷/복원을 함께 보장한다.
///
/// ─────────────────────────────────────────────────────────────
/// 여기서 보면 좋은 경우(= 뭘 건드리고 싶을 때)
/// - “Add Node/Step/Command”의 정책을 바꾸고 싶다
///   → AddNode(), AddStep(...), (InsertSingleAt/InsertBatchAt는 다른 partial에 있을 가능성)
/// - 삭제 UX/정책(확인창, 삭제 후 선택 이동, 컬럼 이동)을 바꾸고 싶다
///   → DeleteSelectedNodeWithConfirm(), DeleteCheckedNodesWithConfirm(), DeleteCommandAt(...)
/// - 삭제 후 다음 선택 인덱스 규칙을 바꾸고 싶다
///   → NextIndexAfterDelete(...)
/// - 변경 적용 타이밍(즉시 vs 다음 프레임), Undo 라벨, 강제 리빌드 조건을 다듬고 싶다
///   → DelayModify(...), DeleteArrayElementByPath(...)
/// - “현재 트랙의 커맨드 리스트” 해석/탐색 방식을 바꾸고 싶다(트랙 구조 변경, 필드명 변경)
///   → FindActiveTrackList(...), FindTrackListOnStep(...)
///
/// ─────────────────────────────────────────────────────────────
/// 설계 메모
/// - 대부분의 변경은 DelayModify()로 감싼다:
///   EditorApplication.delayCall을 통해 “현재 GUI 이벤트 처리 중” 즉시 구조를 바꾸다가
///   ReorderableList/SerializedProperty 순회와 충돌하는 문제를 피한다.
/// - DeleteArrayElementAtIndex는 Unity 직렬화 규칙상 1회로 끝나지 않는 경우가 있어,
///   ObjectReference/ManagedReference null 잔상 케이스를 감지해 2회 삭제를 수행한다.
/// - DeleteCommandAt은 foldout 상태를 Snapshot/Restore하여, 삭제로 인한 ID/배열 재정렬에도
///   사용자가 펼쳐둔 상태가 최대한 유지되도록 한다.
/// </summary>
public sealed partial class SequenceSpecEditorWindow
{
    private void AddNode()
    {
        if (_nodesProp == null) return;

        Undo.RecordObject(targetSequence, "Add Node");

        int idx = _nodesProp.arraySize;
        _nodesProp.arraySize++;

        var newNode = _nodesProp.GetArrayElementAtIndex(idx);

        var nameProp = newNode.FindPropertyRelative("editorName");
        if (nameProp != null) nameProp.stringValue = "";

        var stepsProp = newNode.FindPropertyRelative("steps");
        if (stepsProp != null && stepsProp.isArray)
            stepsProp.arraySize = 0;

        _selectedNode = idx;
        _selectedStep = -1;

        _so.ApplyModifiedProperties();
        EditorUtility.SetDirty(targetSequence);

        ForceCompileAll();

        _stepsList = null;
        _commandsList = null;

        if (_nodesList != null) _nodesList.index = _selectedNode;

        Repaint();
    }

    private void DeleteSelectedNodeWithoutConfirm()
    {
        if (_nodesProp == null || !_nodesProp.isArray) return;
        int idx = (_nodesList != null) ? _nodesList.index : _selectedNode;
        if (idx < 0 || idx >= _nodesProp.arraySize) return;

        string nodesPath = _nodesProp.propertyPath;

        DeleteArrayElementByPath("Delete Node", nodesPath, idx, after: () =>
        {
            _selectedNode = Mathf.Clamp(idx - 1, 0, _nodesProp.arraySize - 2);
            _selectedStep = -1;

            _stepsList = null;
            _commandsList = null;

            ForceCompileAll();
        });
    }

    private void DeleteCheckedNodesWithoutConfirm()
    {
        if (_nodesProp == null || !_nodesProp.isArray) return;
        if (_checkedNodes.Count == 0) return;

        int[] indices = _checkedNodes
            .Where(i => i >= 0 && i < _nodesProp.arraySize)
            .Distinct()
            .OrderByDescending(i => i)
            .ToArray();

        if (indices.Length == 0) return;

        string nodesPath = _nodesProp.propertyPath;

        DelayModify("Delete Nodes", so =>
        {
            var arr = so.FindProperty(nodesPath);
            if (arr == null || !arr.isArray) return;

            for (int k = 0; k < indices.Length; k++)
            {
                int idx = indices[k];
                if (idx < 0 || idx >= arr.arraySize) continue;

                arr.DeleteArrayElementAtIndex(idx);

                if (idx < arr.arraySize)
                {
                    var el = arr.GetArrayElementAtIndex(idx);
                    bool needsSecondDelete =
                        (el.propertyType == SerializedPropertyType.ObjectReference &&
                         el.objectReferenceValue == null) ||
                        (el.propertyType == SerializedPropertyType.ManagedReference &&
                         el.managedReferenceValue == null);

                    if (needsSecondDelete)
                        arr.DeleteArrayElementAtIndex(idx);
                }
            }

            _checkedNodes.Clear();
            _deleteMultiMode = false;

            _selectedNode = Mathf.Clamp(_selectedNode, 0, arr.arraySize - 1);
            _selectedStep = -1;

            _nodesList = null;
            _stepsList = null;
            _commandsList = null;
        }, forceRebuild: true);
    }

    private void AddStep(SerializedProperty stepsProp)
    {
        int nodeIndex = _selectedNode;

        DelayModify("Add Step", so =>
        {
            var seq = (SequenceSpecSO)so.targetObject;
            if (seq == null) return;
            if (nodeIndex < 0 || nodeIndex >= seq.nodes.Count) return;

            var node = seq.nodes[nodeIndex];
            node.steps ??= new List<StepSpec>();

            int insertAt = node.steps.Count;
            node.steps.Insert(insertAt, CreateBlankStep());

            _selectedStep = insertAt;

            _stepsList = null;
            _commandsList = null;
        });
    }

    private void DeleteCommandAt(string commandsPath, int index, Action after = null)
    {
        DelayModify("Delete Command", so =>
        {
            var arr = so.FindProperty(commandsPath);
            if (arr == null || !arr.isArray) return;
            if (index < 0 || index >= arr.arraySize) return;

            long deletedId = 0;
            var delEl = arr.GetArrayElementAtIndex(index);
            if (delEl != null && delEl.propertyType == SerializedPropertyType.ManagedReference)
                deletedId = delEl.managedReferenceId;

            var foldouts = SnapshotCommandFoldouts(arr);

            arr.DeleteArrayElementAtIndex(index);
            if (index < arr.arraySize)
            {
                var el = arr.GetArrayElementAtIndex(index);
                bool needsSecondDelete =
                    (el.propertyType == SerializedPropertyType.ObjectReference && el.objectReferenceValue == null) ||
                    (el.propertyType == SerializedPropertyType.ManagedReference && el.managedReferenceValue == null);

                if (needsSecondDelete)
                    arr.DeleteArrayElementAtIndex(index);
            }

            RestoreCommandFoldouts(arr, foldouts, newIdToCollapse: -1);

            var map = GetFoldoutMap(commandsPath);
            if (map != null && deletedId != 0)
                map.Remove(deletedId);

            _pendingCommandIndex = NextIndexAfterDelete(index, arr.arraySize);

            if (arr.arraySize <= 0)
            {
                _navColumn = NavColumn.Steps;
                ClearCommandSelection();
                _commandsList = null;
                _commandsPropPath = null;

                _scrollToCommandIndex = false;
                _scrollTargetCommandIndex = -1;
            }
            else
            {
                _scrollToCommandIndex = true;
                _scrollTargetCommandIndex = _pendingCommandIndex;
            }

            after?.Invoke();
        });
    }

    private static int NextIndexAfterDelete(int deletedIndex, int newCount)
    {
        if (newCount <= 0) return -1;
        return Mathf.Clamp(deletedIndex, 0, newCount - 1);
    }

    private void DelayModify(string undoLabel, Action<SerializedObject> action, bool forceRebuild = false)
    {
        EditorApplication.delayCall += () =>
        {
            if (targetSequence == null) return;

            Undo.RecordObject(targetSequence, undoLabel);

            var so = new SerializedObject(targetSequence);
            so.Update();

            action?.Invoke(so);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(targetSequence);

            ForceCompileAll();

            if (forceRebuild)
                RebuildIfNeeded(force: true);

            Repaint();
        };
    }

    private void DeleteArrayElementByPath(string undoLabel, string arrayPropPath, int index, Action after = null)
    {
        DelayModify(undoLabel, so =>
        {
            var arr = so.FindProperty(arrayPropPath);
            if (arr == null || !arr.isArray) return;
            if (index < 0 || index >= arr.arraySize) return;

            arr.DeleteArrayElementAtIndex(index);

            if (index < arr.arraySize)
            {
                var el = arr.GetArrayElementAtIndex(index);

                bool needsSecondDelete =
                    (el.propertyType == SerializedPropertyType.ObjectReference && el.objectReferenceValue == null) ||
                    (el.propertyType == SerializedPropertyType.ManagedReference && el.managedReferenceValue == null);

                if (needsSecondDelete)
                    arr.DeleteArrayElementAtIndex(index);
            }

            after?.Invoke();
        });
    }

    private void ShowContextMenu(Action<GenericMenu> build)
    {
        var menu = new GenericMenu();
        build?.Invoke(menu);
        menu.ShowAsContext();
    }
}
#endif