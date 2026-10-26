// 이 하네스는 Unity/Yarn에 매인 것만 대역으로 두고, SceneRunner와 그 협력자는 실제 소스를 컴파일한다.
using System.Collections.Generic;

namespace UnityEngine
{
    public static class Debug
    {
        public static readonly List<string> Logs = new();
        public static readonly List<string> Errors = new();

        public static void Log(object value) => Logs.Add(value?.ToString());
        public static void LogWarning(object value) => Logs.Add(value?.ToString());
        public static void LogError(object value) => Errors.Add(value?.ToString());
    }
}

// 진짜는 VNYarnLineBoundary.cs에 있고 Yarn.Unity를 참조한다. 모양만 같게 둔다.
public readonly struct YarnLineMeta
{
    public readonly string nodeName;
    public readonly string lineId;
    public readonly string charName;
    public readonly string rawText;

    public YarnLineMeta(string nodeName, string lineId, string charName, string rawText)
    {
        this.nodeName = nodeName;
        this.lineId = lineId;
        this.charName = charName;
        this.rawText = rawText;
    }
}
