namespace Ked.Progression
{
    public sealed class StatChangeDto
    {
        public string Key { get; set; }

        /// <summary><c>Op</c>가 <c>"Set"</c>이면 <b>정할 값</b>, 그 외에는 증감량이다.</summary>
        public int Amount { get; set; }

        /// <summary>
        /// 변화의 종류. 비어 있거나 <c>"Add"</c>면 더하기, <c>"Set"</c>이면 정하기.
        ///
        /// ⚠ §G1 — enum이 아니라 <b>이름 문자열</b>로 받는다. DTO에 enum을 두면 모르는
        /// 이름이 기본값 0(=Add)으로 조용히 미끄러져, 깃발을 켜려던 간선이 아무것도
        /// 안 하는 간선이 된다. 문자열이면 로더가 "모르는 이름"으로 잡아낸다.
        ///
        /// ⚠ 비어 있음 = <c>Add</c>는 <b>의도된 기본</b>이다. 이 칸이 서기 전에 나간
        /// 챕터 JSON이 한 글자도 안 바뀌고 그대로 실려야 한다.
        /// </summary>
        public string Op { get; set; }
    }
}