namespace Ked.Presentation.Core
{
    /// <summary>
    /// 슬롯 스폰 · 배역/별칭 · 구조 축 — 다른 모든 폴드의 전제다.
    /// 슬롯이 서야 노드가 생기고, 배역/별칭이 있어야 커맨드가 대상을 푼다.
    /// </summary>
    public static partial class StageReducer
    {
        // ── 슬롯 ─────────────────────────────────────────────────────

        private static bool ApplySlot(
            StageState state, in StageCommand cmd, StageReducerTuning tuning,
            string stageKey, string layerKey, out string reason)
        {
            if (!TryGetSlotKey(cmd, out string slotKey, out reason))
                return false;

            if (state.HasSlot(slotKey))
            {
                // 같은 슬롯 재선언: 런타임은 기존 리그를 재사용한다. 부착만 갱신한다.
                state.SetAttachment(slotKey, new SlotAttachment(stageKey, layerKey));
                return true;
            }

            if (tuning.RigSchemas == null)
            {
                reason = "tuning에 리그 스키마가 없어 슬롯을 세울 수 없다";
                return false;
            }

            if (!TryFindCharacterRig(tuning.RigSchemas, out RigSchemaRigDto rig))
            {
                reason = "리그 스키마 덤프에 character 리그가 없다";
                return false;
            }

            // v1 가정 1: 컨테이너는 항등이므로 리그를 루트 공간 직속으로 세운다.
            RigSchemaLoader.AddRigTo(state.Nodes, rig, keyPrefix: slotKey + "/");
            state.RegisterSlot(slotKey);
            state.SetAttachment(slotKey, new SlotAttachment(stageKey, layerKey));

            // 초기 가시성: 덤프의 CanvasGroup alpha.
            // 초상·오버레이·이모지 루트는 0으로 태어난다 — 기본값 1로 두면
            // show 전 구간이 전부 어긋난다(첫 리포트의 최대 불일치 클래스였다).
            for (int i = 0; i < rig.nodes.Count; i++)
            {
                RigSchemaNodeDto node = rig.nodes[i];

                if (node != null && node.hasCanvasGroup)
                    state.SetAlpha(StageState.NodeKeyOf(slotKey, node.id), node.canvasGroupAlpha);
            }

            return true;
        }

        private static bool TryFindCharacterRig(RigSchemasFileDto file, out RigSchemaRigDto rig)
        {
            rig = null;

            if (file.rigs == null)
                return false;

            for (int i = 0; i < file.rigs.Count; i++)
            {
                if (file.rigs[i]?.rigKind == "character")
                    rig = file.rigs[i];
            }

            return rig != null;
        }

        // ── 배역·별칭 ────────────────────────────────────────────────

        /// <summary>cast (slot, characterKey, [variant="a"], [emotion="1"]) — 배역만 접는다.</summary>
        private static bool ApplyCast(StageState state, in StageCommand cmd, out string reason)
        {
            if (!TryGetSpawnedSlot(state, cmd, out string slotKey, out reason))
                return false;

            string characterKey = cmd.Arg(1);

            if (string.IsNullOrEmpty(characterKey))
            {
                reason = "cast에 캐릭터 키가 없다";
                return false;
            }

            state.SetCast(slotKey, characterKey);

            // 브리지는 cast를 pose+face로 팬아웃한다(초상 그림·사이징). 그 축은 아직
            // 상태 모델 밖이다 — 배역은 접되 초상 축은 기록으로 남긴다.
            state.AddUnhandled(cmd, "초상 축(변형·표정·사이징)은 아직 상태 모델 밖");

            return true;
        }

        private static bool ApplyActor(StageState state, in StageCommand cmd, out string reason)
        {
            reason = null;

            string aliasSymbol = cmd.Arg(0);
            string targetKey = cmd.Arg(1);

            if (string.IsNullOrEmpty(aliasSymbol) || string.IsNullOrEmpty(targetKey))
            {
                reason = "actor 인자가 모자란다 (별칭, 대상)";
                return false;
            }

            state.SetAlias(aliasSymbol, targetKey);
            return true;
        }

        // ── 구조 ─────────────────────────────────────────────────────

        private static bool ApplyCharTo(
            StageState state, in StageCommand cmd, string stageKey, string layerKey, out string reason)
        {
            if (!TryGetSpawnedSlot(state, cmd, out string slotKey, out reason))
                return false;

            state.SetAttachment(slotKey, new SlotAttachment(stageKey, layerKey));
            return true;
        }
    }
}
