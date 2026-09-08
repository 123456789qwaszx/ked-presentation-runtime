using System;
using System.Collections.Generic;

[Serializable]
public sealed class AlbumProgress
{
    public int FormatVersion = 1;

    public List<string> UnlockedIds = new();
}