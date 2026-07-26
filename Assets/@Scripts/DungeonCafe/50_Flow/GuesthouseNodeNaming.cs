/// <summary>
/// 코드에서 생성하는 Yarn 노드 이름 규칙을 한 곳에 모은다.
/// 저작 에셋에 노드 이름이 명시되어 있으면 항상 그쪽이 우선한다.
/// </summary>
public static class GuesthouseNodeNaming
{
    public static string MasteryEvent(string maidId, BurdenAxis axis, int levelAfter)
        => $"Mastery_{maidId}_{axis}_{levelAfter}";

    public static string NightProgram(string maidId, NightProgramKind kind, BurdenAxis axis)
        => $"Night_{kind}_{maidId}_{axis}";

    public static string MaidConversation(int dayNumber)
        => $"Night_Conversation_Day{dayNumber}";

    public static string ServiceBriefingFallback(string monsterId)
        => $"Service_{monsterId}_Briefing";
}
