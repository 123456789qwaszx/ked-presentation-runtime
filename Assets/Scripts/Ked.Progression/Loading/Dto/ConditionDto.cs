namespace Ked.Progression
{
    public sealed class ConditionDto
    {
        public string Kind { get; set; }
        public string Key { get; set; }
        public string Op { get; set; }

        /// <summary>
        /// ⚠ §G2 — 저작 쪽은 <b>0을 키 자체를 생략해서</b> 내보낸다. 그래서 이 필드는
        /// 반드시 <c>int</c>여야 한다. <c>int?</c>로 두면 "없음"과 "0"이 갈려서 가장 흔한
        /// 조건인 <c>flag == false</c>(= Equal 0)가 통째로 어긋난다.
        /// </summary>
        public int IntValue { get; set; }
    }

}