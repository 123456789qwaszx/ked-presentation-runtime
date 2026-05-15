using UnityEngine;

public sealed class SlantedMaskResetGroup : MonoBehaviour
{
    [SerializeField] private SlantedMaskGraphic[] _masks;

    public void ResetAllToHiddenOffset()
    {
        if (_masks == null)
            return;

        for (int i = 0; i < _masks.Length; i++)
        {
            SlantedMaskGraphic mask = _masks[i];

            if (mask == null)
                continue;

            mask.ResetToHiddenOffset();
        }
    }
}