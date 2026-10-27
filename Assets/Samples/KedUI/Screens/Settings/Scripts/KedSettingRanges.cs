namespace Ked.Samples.UI
{
    /// <summary>
    /// 설정 값의 범위 — 설정 Page의 슬라이더와 앱의 설정 값(ked: App의 KedSettings)이 같이 쓴다.
    /// </summary>
    public static class KedSettingRanges
    {
        public const float MinTextScale = 0.8f;
        public const float MaxTextScale = 1.3f;
        public const float MinTypeSpeed = 10f;
        public const float MaxTypeSpeed = 90f;
        // 0이면 대사창 · 이름표 그림이 다 사라진다. 글자는 그림과 따로라 늘 보인다(PlayDialogueBox.SetOpacity).
        public const float MinBoxOpacity = 0f;
        public const float MinAutoDelay = 0.5f;
        public const float MaxAutoDelay = 5f;
    }
}
