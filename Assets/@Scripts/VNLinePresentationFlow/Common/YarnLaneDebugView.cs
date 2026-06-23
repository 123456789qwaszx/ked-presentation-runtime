using TMPro;
using UnityEngine;

public sealed class YarnLaneDebugView : MonoBehaviour, IYarnLaneDebugSink
{
    [Header("Main")]
    [SerializeField] private TMP_Text _mainNodeText;
    [SerializeField] private TMP_Text _mainLineText;

    [Header("Presentation")]
    [SerializeField] private TMP_Text _presentationNodeText;
    [SerializeField] private TMP_Text _presentationLineText;

    [Header("OneShot")]
    [SerializeField] private TMP_Text _oneShotNodeText;
    [SerializeField] private TMP_Text _oneShotLineText;

    public void SetMain(string nodeName, string rawText)
    {
        Set(_mainNodeText, _mainLineText, nodeName, rawText);
    }

    public void SetPresentation(string nodeName, string rawText)
    {
        Set(_presentationNodeText, _presentationLineText, nodeName, rawText);
    }

    public void SetOneShot(string nodeName, string rawText)
    {
        Set(_oneShotNodeText, _oneShotLineText, nodeName, rawText);
    }

    public void ClearMain()
    {
        SetMain("-", "");
    }

    public void ClearPresentation()
    {
        SetPresentation("-", "");
    }

    public void ClearOneShot()
    {
        SetOneShot("-", "");
    }

    private static void Set(TMP_Text nodeText, TMP_Text lineText, string nodeName, string rawText)
    {
        if (nodeText != null)
            nodeText.text = string.IsNullOrEmpty(nodeName) ? "-" : nodeName;

        if (lineText != null)
            lineText.text = string.IsNullOrEmpty(rawText) ? "" : rawText;
    }
}