using System;
using System.Collections.Generic;

[Serializable]
public sealed class EpisodeYarnEntryData
{
    public string EpisodeId;
    public EpisodeNodeKind Kind;
    public string YarnNodeName;
}