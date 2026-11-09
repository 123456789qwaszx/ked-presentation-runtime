namespace Ked.Presentation.Core
{
    public static partial class NodeTargets
    {
        // ── 배경 사이징 (CharRigImageSizingPolicy.SourcePixels) ──
        //
        // 배경은 **원본 픽셀 크기 그대로** 그린다 — 캔버스 기준 1px = 원본 1px, 피벗(가운데) 자리에.
        // 화면보다 큰 배경은 화면 밖까지 걸치고, 카메라(샷)는 그 안을 본다. 높이 맞춤은 2026-11-09에 걷었다:
        // 배경 전체가 화면에 들어와 여백이 없어서, 카메라가 조금만 줌아웃해도 가장자리가 드러났다.
        // (근거: 원본 게임 실영상 — 2540×1800 배경의 가운데 2336×1078이 보이고, 줌은 그 여백 안에서 일어난다.)
        //
        // 노드는 앵커와 상관없이 같은 식으로 쓴다:
        //   rect 크기 = 부모 크기 × (anchorMax − anchorMin) + sizeDelta
        //   → sizeDelta = 그림 크기 − 부모 크기 × (anchorMax − anchorMin)
        // 리그의 그림 노드는 늘림 앵커(0,0)–(1,1)이라 sizeDelta = 그림 크기 − 부모 크기다.
        // 런타임 CharRigImageSizingPolicy.ApplySourcePixels가 같은 식을 쓴다.

        public static NodeTarget BackgroundSizing(
            string imageNodeKey, Vec2 parentSize, Vec2 anchorMin, Vec2 anchorMax, Vec2 spriteSize)
            => NodeTarget.SizeDelta(imageNodeKey, new Vec2(
                spriteSize.X - parentSize.X * (anchorMax.X - anchorMin.X),
                spriteSize.Y - parentSize.Y * (anchorMax.Y - anchorMin.Y)));
    }
}
