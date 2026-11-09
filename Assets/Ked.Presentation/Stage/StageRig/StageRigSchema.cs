using System.Globalization;
using Ked.Presentation.Core;

/// <summary>
/// 무대 리그의 계층 — 프리팹이 아니라 이 스키마가 소유한다. 캐릭터 리그(CharacterRigSchema)와 같은 규율이다.
///
/// 노드의 자리가 그 노드의 뜻이다:
///   Stage_Root      세상의 자리. 안 움직인다. 리그 좌표 계산의 경계(RigSpaceRoot)
///     Mask_Root     장면 전환 마스크. 카메라 위에 있어야 카메라가 밀어낸 내용을 자른다
///       Camera_Pan  샷의 팬. 중립 (0,0)
///         Camera_Zoom   샷의 줌. 중립 배율 1
///           Layer_{d}   깊이 층. 샷에 d만큼 반응한다(ShotLayoutMath). 위치·배율을 이 한 노드에 쓴다. 중립 항등
///             …_Content   리그가 붙는 곳. 블러 대상
///     MaskEdge_Root 마스크 테두리 그래픽. Mask_Root의 형제(마스크에 잘리지 않아야 한다)
///
/// 무대 루트 **바깥**에 컷인 창이 하나 더 있다(2026-11-09, docs/PLAN_cutin.md):
///   {stage}_Window      무대 전체를 들고 팝 · 회전한다. 중립 항등(무대 루트와 같은 rect). 피벗은 컷인을 열 때 판 아래 가운데로
///     {stage}_Panel     컷인 판 그림(Image). 무대 루트보다 뒤(형제 0). 닫혀 있으면 꺼져 있다
///     {stage}_Stage_Root
/// 창이 무대 루트 바깥이라 리그 좌표 계산(측정 체인 · 샷 식)은 창을 모른다 — 창이 팝하는 도중에 place를 해도 같다.
/// 창은 아래 Nodes 표에 없다(표는 무대 루트에서 시작한다). StageRigBuilder.EnsureCutIn이 세운다.
///
/// 카메라가 무대 **아래**에 있어서 무대마다 카메라가 있다. 어느 무대의 카메라를 샷이 움직이는지는 역할(StageRoles)이
/// 정한다 — 지금은 본 무대만이다. 구조가 아니라 정책이다(2026-11-06 D2).
///
/// 층은 고정 노드가 아니다. 깊이 값 d로 식별하고 처음 참조될 때 세운다(StageRigBuilder.EnsureLayer).
/// 라벨(far…close)은 StageLayerLabels의 눈금이다.
///
/// 모든 노드는 stretch 앵커 · 피벗 중앙 · 항등 트랜스폼으로 선다. 그래서 중립값이 상수이고 바인드가 필요 없다.
/// 노드 이름 = "{무대 키}_{노드}" — 한 씬에 무대가 여럿이라 접두사로 가른다.
/// </summary>
public static class StageRigSchema
{
    public enum Refs
    {
        Stage_Root,
        Mask_Root,
        MaskEdge_Root,
        Camera_Pan,
        Camera_Zoom,
    }

    public sealed class NodeDef
    {
        public Refs Id;
        public Refs? Parent;
    }

    public static readonly NodeDef[] Nodes =
    {
        new() { Id = Refs.Stage_Root, Parent = null },
        new() { Id = Refs.Mask_Root, Parent = Refs.Stage_Root },
        new() { Id = Refs.MaskEdge_Root, Parent = Refs.Stage_Root },
        new() { Id = Refs.Camera_Pan, Parent = Refs.Mask_Root },
        new() { Id = Refs.Camera_Zoom, Parent = Refs.Camera_Pan },
    };

    public const string LayerPrefix = "Layer_";
    public const string ContentSuffix = "_Content";

    public static string NodeName(string stageKey, Refs id) => $"{stageKey}_{id}";

    /// <summary>컷인 창 — 무대 루트의 부모.</summary>
    public static string WindowName(string stageKey) => $"{stageKey}_Window";

    /// <summary>컷인 판 — 창의 첫 자식(무대 루트보다 뒤에 그려진다).</summary>
    public static string PanelName(string stageKey) => $"{stageKey}_Panel";

    public static string LayerName(string stageKey, float depth)
        => $"{stageKey}_{LayerPrefix}{StageLayerLabels.NameOf(depth)}";

    public static string ContentName(string stageKey, float depth)
        => LayerName(stageKey, depth) + ContentSuffix;

    /// <summary>"{무대}_Layer_{라벨|숫자}" 이름에서 d를 읽는다. 층 이름이 아니면 false.</summary>
    public static bool TryParseLayerName(string stageKey, string nodeName, out float depth)
    {
        depth = 0f;

        string prefix = $"{stageKey}_{LayerPrefix}";

        if (nodeName == null || !nodeName.StartsWith(prefix, System.StringComparison.Ordinal))
            return false;

        if (nodeName.EndsWith(ContentSuffix, System.StringComparison.Ordinal))
            return false;

        string label = nodeName.Substring(prefix.Length);

        if (StageLayerLabels.TryGetDepth(label, out depth))
            return true;

        return float.TryParse(label, NumberStyles.Float, CultureInfo.InvariantCulture, out depth);
    }
}
