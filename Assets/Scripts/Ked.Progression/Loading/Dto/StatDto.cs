namespace Ked.Progression
{
    /// <summary>
    /// ⚠ <see cref="Type"/>은 <c>"Number"</c> 또는 <c>"Bool"</c>이다.
    /// 저작 쪽 enum은 <c>Int</c>지만 내보내기가 이름을 번역해서 낸다.
    /// </summary>
    public sealed class StatDto
    {
        public string Key { get; set; }
        public string DisplayName { get; set; }
        public string Type { get; set; }
        public int Initial { get; set; }
        public int Minimum { get; set; }
        public int Maximum { get; set; }
    }
}