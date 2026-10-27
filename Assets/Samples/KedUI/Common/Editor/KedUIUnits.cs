using System.Collections.Generic;

namespace Ked.Samples.UI.Editor
{
    /// <summary>
    /// KedUI의 화면 묶음 설정. 규칙(생성물 자리 · 묶음 감사)과 그림을 이름으로 찾는 도구는 UIPF의
    /// UIScreenUnits · UISpriteIndex(Editor/UI/Authoring)이고, 여기는 KedUI가 넘기는 값만 둔다.
    ///
    ///   Screens/{묶음}/Scripts · Prefabs · Presentation · Sprites · Data · Editor
    ///   Common/   여러 화면이 같이 쓰는 키트 스크립트 · 그림 · 글꼴
    ///
    /// 묶음은 제 묶음 · Common · UIPF · 본체(게임 쪽 프레임워크) · Unity 패키지만 가리킬 수 있다(Audit).
    /// </summary>
    internal static class KedUIUnits
    {
        public const string UnitsDir = KedUIKit.Root + "/Screens";
        public const string CommonDir = KedUIKit.Root + "/Common";

        private const string UipfDir = "Assets/UIPresentationFlow";
        private const string CoreDir = "Assets/Ked.Presentation"; // 선택지 항목(VNOptionItem) · 대사창 배치 DB 같은 본체 계약

        public static readonly UIScreenUnits Rules = new(UnitsDir, CommonDir, UipfDir, CoreDir);

        /// <summary>
        /// KedUI 아래 PNG를 이름으로 — 그림은 Art/ · Common/Sprites/ · 묶음의 Sprites/ 어디에 있어도 된다.
        /// KedUI는 같은 이름을 하나도 두지 않는다 — 겹치면 모두 problems에 올린다.
        /// </summary>
        public static UISpriteIndex Art(List<string> problems)
        {
            UISpriteIndex index = UISpriteIndex.Build(new[] { KedUIKit.Root });
            index.ReportDuplicates(problems);
            return index;
        }
    }
}
