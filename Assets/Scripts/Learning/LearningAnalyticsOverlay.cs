using System;
using UnityEngine;

// 학습용 표시창. 실제 게임 UI 계약에는 들어가지 않는다.
public sealed class LearningAnalyticsOverlay : MonoBehaviour
{
    private static LearningAnalyticsOverlay _instance;
    private static Action _backupAction;

    private string _message = "[학습 서버] 대기";

    public static void Show(string message)
    {
        Ensure()._message = message;
    }

    public static void SetBackupAction(Action backupAction)
    {
        _backupAction = backupAction;
        Ensure();
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
            new Rect(12f, 12f, 560f, 150f),
            _message);

        GUI.enabled = _backupAction != null;

        if (GUI.Button(
                new Rect(22f, 122f, 160f, 28f),
                "서버에 저장"))
        {
            _backupAction?.Invoke();
        }

        GUI.enabled = true;
    }
}
