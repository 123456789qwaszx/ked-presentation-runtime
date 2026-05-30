using System;
using Yarn.Unity;

[Serializable]
public struct YarnLineMeta
{
    public string nodeName;
    public string lineId;
    public string charName;
    public string rawText;

    public YarnLineMeta(string nodeName, string lineId, string charName, string rawText)
    {
        this.nodeName = nodeName;
        this.lineId = lineId;
        this.charName = charName;
        this.rawText = rawText;
    }
}

public sealed class YarnLineLifecycleBridge
{
    public event Action<YarnLineMeta> LineEntered;
    
    private YarnLineMeta _currentMeta;
    public YarnLineMeta CurrentMeta => _currentMeta;
    
     public void RefreshCurrentLineMeta(LocalizedLine line, string nodeName)
     {
         _currentMeta = new YarnLineMeta(
             nodeName,
             line.TextID,
             line.CharacterName,
             line.TextWithoutCharacterName.Text);

         LineEntered?.Invoke(_currentMeta);
     }
}