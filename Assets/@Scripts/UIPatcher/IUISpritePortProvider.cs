using System.Collections.Generic;
using UnityEngine;

public interface IUISpritePortProvider
{
    // View.Image(Refs.)
    IReadOnlyList<string> GetSpritePortIds();
    
    // 실제로 스프라이트 연결법
    bool TrySetSprite(string portId, Sprite sprite);
}