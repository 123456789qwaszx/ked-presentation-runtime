using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterEmojiMaterialRuntime
{
    private readonly Image _image;

    public Material RuntimeMaterial { get; private set; }

    public CharacterEmojiMaterialRuntime(Image image)
    {
        _image = image;
    }

    public void DestroyRuntimeMaterial()
    {
        if (_image != null && _image.material == RuntimeMaterial)
            _image.material = null;

        if (RuntimeMaterial != null)
        {
            Object.Destroy(RuntimeMaterial);
            RuntimeMaterial = null;
        }
    }
}