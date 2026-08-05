using System;
using System.Collections.Generic;
using Ked.Presentation.Core;
using UnityEngine;

// U14 등가성 하네스의 캡처 절반 — 살아 있는 무대에서 StageState를 읽어낸다.
//
// 새로 발명한 것이 없다: 노드 상태 캡처는 b-3 어댑터의 CaptureLive와 같은 필드 읽기이고,
// 노드 열거는 리그 이름 파싱이 아니라 CharacterRigRefs의 타입 접근(GetRect)이라
// role prefix가 붙은 실제 오브젝트 이름과 무관하게 스키마 id로 키가 잡힌다.
// 키 규약은 리듀서와 동일: "{slotKey}/{스키마 id}", 리그 루트는 "{slotKey}/__root".
//
// 아무것도 쓰지 않는다 — 읽기 전용이다.
public static class StageStateCapture
{
    public static StageState Capture(
        CharacterRigRegistry characterRigs,
        PresentationShotResponseSystem shotSystem,
        Vec2 baseResolution)
    {
        if (characterRigs == null)
            throw new ArgumentNullException(nameof(characterRigs));

        StageState state = new StageState(new RectSpace(baseResolution, Vec2.Half));

        List<KeyValuePair<string, CharacterRigRefs>> rigs = new();
        characterRigs.CollectAliveRigEntries(rigs);

        foreach (KeyValuePair<string, CharacterRigRefs> pair in rigs)
            CaptureRig(state, pair.Key, pair.Value);

        if (shotSystem != null)
            state.Shot = PresentationIntentStateCoreBridge.ToCore(shotSystem.CurrentState);

        return state;
    }

    private static void CaptureRig(StageState state, string slotKey, CharacterRigRefs refs)
    {
        state.RegisterSlot(slotKey);

        string rootKey = StageState.NodeKeyOf(slotKey, RigSchemaLoader.RootKey);
        state.Nodes.Add(rootKey, null, CaptureNode(refs.RigRoot));
        CaptureAlpha(state, rootKey, refs.RigRoot);

        // 스키마 선언 순서(부모 먼저)를 그대로 탄다 — 트리 Add의 부모 선행 조건과 맞는다.
        foreach (CharacterRigSchema.NodeDef def in CharacterRigSchema.Nodes)
        {
            if (!TryToTarget(def.Id, out CharacterRigTarget target))
                continue;

            RectTransform rect = refs.GetRect(target);

            if (rect == null)
                continue; // 없는 노드는 캡처 자체가 안 된다 — 비교기가 "접힘 전용"으로 세운다

            string key = StageState.NodeKeyOf(slotKey, def.Id.ToString());
            string parentKey = def.Parent.HasValue
                ? StageState.NodeKeyOf(slotKey, def.Parent.Value.ToString())
                : rootKey;

            if (!state.Nodes.Contains(parentKey))
                continue; // 부모가 캡처 안 됐으면 자식도 잇지 않는다 (구조가 깨졌다는 뜻 — 비교기가 잡는다)

            state.Nodes.Add(key, parentKey, CaptureNode(rect));
            CaptureAlpha(state, key, rect);
        }
    }

    private static RectNodeState CaptureNode(RectTransform rect)
    {
        Vector2 anchoredPosition = rect.anchoredPosition;
        Vector2 anchorMin = rect.anchorMin;
        Vector2 anchorMax = rect.anchorMax;
        Vector2 pivot = rect.pivot;
        Vector2 sizeDelta = rect.sizeDelta;
        Vector3 localScale = rect.localScale;
        Vector3 localEuler = rect.localEulerAngles;

        return new RectNodeState(
            anchoredPosition: new Vec2(anchoredPosition.x, anchoredPosition.y),
            anchorMin: new Vec2(anchorMin.x, anchorMin.y),
            anchorMax: new Vec2(anchorMax.x, anchorMax.y),
            pivot: new Vec2(pivot.x, pivot.y),
            sizeDelta: new Vec2(sizeDelta.x, sizeDelta.y),
            localScale: new Vec3(localScale.x, localScale.y, localScale.z),
            localEulerAngles: new Vec3(localEuler.x, localEuler.y, localEuler.z));
    }

    private static void CaptureAlpha(StageState state, string key, RectTransform rect)
    {
        if (rect.TryGetComponent(out CanvasGroup group))
            state.SetAlpha(key, group.alpha);
    }

    private static bool TryToTarget(CharacterRigSchema.Refs id, out CharacterRigTarget target)
    {
        // 두 enum은 이름이 1:1이다 (CharacterRigTarget이 RigRoot 하나를 더 가질 뿐).
        return Enum.TryParse(id.ToString(), out target);
    }
}
