using System;
using System.Collections.Generic;

/// <summary>
/// 모션이 겨눌 수 있는 노드 — 리그 이름(motions.json의 <c>rig</c>)마다. 모션 로더가 클립을 제 리그의 스키마로 검증한다.
/// 리그 종류가 늘면 여기에 한 줄을 더한다.
/// </summary>
public static class MotionRigSchemas
{
    public const string CharacterRigKind = "character";

    public static IReadOnlyDictionary<string, ICollection<string>> NodeIdsByRig()
        => new Dictionary<string, ICollection<string>>(StringComparer.Ordinal)
        {
            [CharacterRigKind] = CharacterRigSchema.NodeIds(),
            [EmojiRigSchema.RigKind] = EmojiRigSchema.MotionNodeIds(),
        };
}
