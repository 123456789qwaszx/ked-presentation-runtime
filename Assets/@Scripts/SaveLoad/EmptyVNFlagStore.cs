using System.Collections.Generic;
using UnityEngine;

public sealed class EmptyVNFlagStore : IVNFlagStore
{
    public List<VNFlagEntry> Capture()
    {
        return new List<VNFlagEntry>();
    }

    public void Restore(List<VNFlagEntry> flags)
    {
        Debug.LogWarning("[EmptyVNFlagStore] Restore called, but no real flag store is bound.");
    }
}

public sealed class AlwaysAllowVNSaveSafetyPolicy : IVNSaveSafetyPolicy
{
    public bool CanManualSaveNow(out string reason)
    {
        reason = "";
        return true;
    }

    public bool CanAutoSaveNow(out string reason)
    {
        reason = "";
        return true;
    }

    public bool CanLoadNow(out string reason)
    {
        reason = "";
        return true;
    }
}