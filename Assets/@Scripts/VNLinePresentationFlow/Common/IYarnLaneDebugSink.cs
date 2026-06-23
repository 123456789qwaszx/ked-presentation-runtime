public interface IYarnLaneDebugSink
{
    void SetMain(string nodeName, string rawText);
    void SetPresentation(string nodeName, string rawText);
    void SetOneShot(string nodeName, string rawText);

    void ClearMain();
    void ClearPresentation();
    void ClearOneShot();
}