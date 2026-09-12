using System;
using UnityEngine;

// 학습용 표시창. 실제 게임 UI 계약에는 들어가지 않는다.
public sealed class LearningAnalyticsOverlay : MonoBehaviour
{
    private static LearningAnalyticsOverlay _instance;
    private static Action _backupAction;
    private static Action _restoreCheckAction;
    private static Action<long> _restoreAction;

    private string _message = "[학습 서버] 대기";
    private string _restoreServerId = string.Empty;

    public static void Show(string message)
    {
        Ensure()._message = message;
    }

    public static void SetBackupAction(Action backupAction)
    {
        _backupAction = backupAction;
        Ensure();
    }

    public static void SetRestoreCheckAction(Action restoreCheckAction)
    {
        _restoreCheckAction = restoreCheckAction;
        Ensure();
    }

    public static void SetRestoreAction(Action<long> restoreAction)
    {
        _restoreAction = restoreAction;
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
            new Rect(12f, 12f, 560f, 205f),
            _message);

        GUI.enabled = _backupAction != null;

        if (GUI.Button(
                new Rect(22f, 122f, 160f, 28f),
                "서버에 저장"))
        {
            _backupAction?.Invoke();
        }

        GUI.enabled = _restoreCheckAction != null;

        if (GUI.Button(
                new Rect(192f, 122f, 160f, 28f),
                "서버 저장 확인"))
        {
            _restoreCheckAction?.Invoke();
        }

        GUI.enabled = true;
        GUI.Label(
            new Rect(22f, 160f, 140f, 24f),
            "server playthroughId");

        _restoreServerId = GUI.TextField(
            new Rect(164f, 158f, 110f, 26f),
            _restoreServerId);

        GUI.enabled = _restoreAction != null;

        if (GUI.Button(
                new Rect(284f, 157f, 160f, 28f),
                "서버에서 복원"))
        {
            if (!long.TryParse(_restoreServerId, out long serverPlaythroughId)
                || serverPlaythroughId <= 0)
            {
                Show("[U4 서버 복원] 숫자 server playthroughId를 입력하세요.");
            }
            else
            {
                _restoreAction?.Invoke(serverPlaythroughId);
            }
        }

        GUI.enabled = true;
    }
}
