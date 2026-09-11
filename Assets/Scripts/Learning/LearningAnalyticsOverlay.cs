using UnityEngine;

// U1 학습용 표시창. 실제 게임 UI 계약에는 들어가지 않는다.
public sealed class LearningAnalyticsOverlay : MonoBehaviour
{
    private static LearningAnalyticsOverlay _instance;
    private string _message = "[U1 서버 콘텐츠] 조회 대기";

    public static void Show(string message)
    {
        Ensure()._message = message;
    }

    private static LearningAnalyticsOverlay Ensure()
    {
        if (_instance != null)
            return _instance;

        GameObject go = new("LearningAnalyticsOverlay");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<LearningAnalyticsOverlay>();
        return _instance;
    }

    private void OnGUI()
    {
        GUI.Box(
            new Rect(12f, 12f, 520f, 110f),
            _message);
    }
}
