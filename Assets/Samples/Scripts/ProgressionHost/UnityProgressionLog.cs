using Ked.Progression;
using UnityEngine;

// 진행 런타임의 로그 포트를 Unity 콘솔로 잇는다.
// 런타임은 UnityEngine을 참조하지 않으므로 로그도 여기서만 엔진에 닿는다.
public sealed class UnityProgressionLog : IProgressionLog
{
    public void Info(string message) => Debug.Log(message);

    public void Warning(string message) => Debug.LogWarning(message);

    public void Error(string message) => Debug.LogError(message);
}
