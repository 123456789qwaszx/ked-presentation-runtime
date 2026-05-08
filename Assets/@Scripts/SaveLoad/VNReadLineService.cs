using System.Collections.Generic;
using UnityEngine;

public sealed class VNReadLineService
{
    private readonly VNGlobalProgressData _globalData;
    private readonly IVNGlobalProgressRepository _globalRepo;

    private HashSet<string> _readLineSet;

    public int SaveEveryNLines = 20;

    private int _pendingCount;

    public VNReadLineService(
        VNGlobalProgressData globalData,
        IVNGlobalProgressRepository globalRepo)
    {
        _globalData = globalData;
        _globalRepo = globalRepo;

        _globalData.Normalize();
        RebuildCache();
    }

    public bool HasRead(string lineId)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            return false;

        return _readLineSet.Contains(lineId);
    }

    public void MarkAsRead(string lineId)
    {
        if (string.IsNullOrWhiteSpace(lineId))
            return;

        if (_readLineSet.Contains(lineId))
            return;

        _readLineSet.Add(lineId);
        _globalData.readLineIds.Add(lineId);

        _pendingCount++;

        if (_pendingCount >= SaveEveryNLines)
            Flush();
    }

    public void Flush()
    {
        if (_pendingCount <= 0)
            return;

        _globalRepo.Save(_globalData);
        _pendingCount = 0;
    }

    public void RebuildCache()
    {
        _globalData.Normalize();
        _readLineSet = new HashSet<string>(_globalData.readLineIds);
    }
}