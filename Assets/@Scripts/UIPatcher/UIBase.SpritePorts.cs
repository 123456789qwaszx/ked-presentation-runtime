using System;
using System.Collections.Generic;
using UnityEngine;

public interface IUISpritePortProvider
{
    IReadOnlyList<string> GetSpritePortIds();
    bool TrySetSprite(string portId, Sprite sprite);
}

public abstract partial class UIBase<TRefs> : IUISpritePortProvider
    where TRefs : struct, Enum
{
    private List<string> _cachedSpritePortIds;

    // Refs(Enum)를 스캔해서 "_Image"로 끝나는 것을 자동으로 수집 및, Sprite 포트로 인식.
    public IReadOnlyList<string> GetSpritePortIds()
    {
        if (_cachedSpritePortIds != null)
            return _cachedSpritePortIds;

        _cachedSpritePortIds = new List<string>();

        foreach (TRefs enumValue in Enum.GetValues(typeof(TRefs)))
        {
            string name = enumValue.ToString();

            if (name.EndsWith("_Image", StringComparison.Ordinal))
            {
                _cachedSpritePortIds.Add(name);
            }
        }

        return _cachedSpritePortIds;
    }

    // portId에 해당하는 Image 컴포넌트에 Sprite 설정.
    public bool TrySetSprite(string portId, Sprite sprite)
    {
        Enum.TryParse(portId, out TRefs enumKey);
        
        View.Image(enumKey).sprite = sprite;
        
        return true;
    }
}