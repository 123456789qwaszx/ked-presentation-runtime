// PresentationSession is the sole owner of dialogue time progression.
// All other components may report state or perform execution,
// but only Tick() is allowed to advance steps or nodes.

using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PresentationSession
{
    // ---- Dependencies (injected) ----
    private readonly StepGatePlanBuilder _gatePlanner;
    private readonly StepGateAdvancer _gateAdvancer;
    private readonly CommandExecutor _executor;
    
    // ---- Session-owned context ----
    private readonly PresentationSessionContext _context;
    private readonly LinePresentationAdvanceState _linePresentationAdvanceState;
    
    // ---- Active run (per-Session) ----
    private CommandRunScope _sessionScope;
    
    // ---- Runtime state ----
    private SequenceProgressState _state;
    private SequenceSpecSO _sequence;
    
    public CommandRunScope CurrentScope => _sessionScope;
    public bool IsRunning => _sequence != null && _state != null && _sessionScope != null;

    public bool IsNodeBusy()
    {
        if (!IsRunning)
            return false;
        
        return _sessionScope.IsNodeBusy;;
    }
    
    public PresentationSession(
        StepGatePlanBuilder gatePlanner,
        StepGateAdvancer gateAdvancer,
        CommandExecutor executor,
        PresentationSessionContext presentationSessionContext,
        LinePresentationAdvanceState linePresentationAdvanceState
    )
    {
        _gatePlanner = gatePlanner;
        _gateAdvancer = gateAdvancer;
        _executor = executor;
        _context = presentationSessionContext;
        _linePresentationAdvanceState = linePresentationAdvanceState;
    }

    
    public void Start(Route route, SequenceSpecSO sequence)
    {
        if (sequence == null)
            return;

        if (IsRunning)
            EndImmediately();

        _gateAdvancer.ClearLatchedSignals();
        //_executor.Stop();
        
        _state = new SequenceProgressState(route);
        _sequence = sequence;
        _sessionScope = new CommandRunScope(_context, _linePresentationAdvanceState);
        
        _context.ResetSessionFlagsForStart();
        
        _gatePlanner.BuildForCurrentNode(_sequence, _state);
        
        PlayStep(
            nodeIndex: _state.NodeIndex,
            stepIndex: _state.StepGate.Cursor);
        
        if (TryApplyDebugStartStepName())
            return;
    }

    public void Tick()
    {
        if (_sequence == null || _state == null) return;
        
        if (_context == null || _context.CloseRequested)
        {
            End();
            return;
        }

        while (true)
        {
            bool advanced = _gateAdvancer.TryAdvanceStepGate(_state, _context);
            if (!advanced)
                break;
            
            if (_state.IsNodeCompleted)
            {
                // ---- Node boundary ----
                _state.NodeIndex++;
                int nextNodeIndex = _state.NodeIndex;

                // if (_state.NodeIndex >= _sequence.nodes.Count)
                // {
                //     End();
                //     return;
                // }

                if (_context == null || _context.CloseRequested)
                {
                    End();
                    return;
                }

                _gateAdvancer.ClearLatchedSignals();
                _gatePlanner.BuildForCurrentNode(_sequence, _state);

                int firstStep = _state.StepGate.Cursor;
                PlayStep(nextNodeIndex, firstStep);
                return;
            }

            // ---- Step boundary ----
            int currentNodeIndex = _state.NodeIndex;
            int currentStep = _state.StepGate.Cursor;
            PlayStep(currentNodeIndex, currentStep);
        } 
    }
    
    public void RequestEnd()
    {
        _context.RequestClose();
    }

    public void EndImmediately()
    {
        End();
    }
    
    
    private void PlayStep(int nodeIndex, int stepIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= _sequence.nodes.Count) return;

        NodeSpec node = _sequence.nodes[nodeIndex];
        _executor.PlayStep(node, stepIndex, _sessionScope);
    }
    
    private void End()
    {
        _gateAdvancer.ClearLatchedSignals();
        _executor.FinishAll(); // clear the session scope.
        _sequence = null;
        _state = null;
    }

    #region Editor
    private bool TryApplyDebugStartStepName()
    {
        if (_sequence == null || _state == null) return false;

        if (!_context.IsDebugStartEnabled)
            return false;

        string startName = _context.DebugStartStepName; 
        if (string.IsNullOrEmpty(startName)) return false;

        if (StepNameResolver.TryResolveUnique(_sequence, startName, out int nodeIndex, out int stepIndex, out int matchCount))
        {
            ApplyStartAt(nodeIndex, stepIndex);
            return true;
        }
        

        if (matchCount == 0)
        {
            Debug.LogWarning($"[CPS] DebugStartStepName not found: '{startName}'", _sequence);
            return false;
        }

        var matches = new List<(int n, int s)>(matchCount);
        StepNameResolver.TryResolveUnique(_sequence, startName, out _, out _, out matchCount, matches);

        var msg = $"[CPS] DebugStartStepName is not unique: '{startName}' (matches={matchCount})\n";
        for (int i = 0; i < matches.Count; i++)
            msg += $"  - node={matches[i].n}, step={matches[i].s}\n";

        Debug.LogWarning(msg, _sequence);
        return false;
    }

    private void ApplyStartAt(int nodeIndex, int stepIndex)
    {
        _state.NodeIndex = nodeIndex;

        _gatePlanner.BuildForCurrentNode(_sequence, _state);

        if (_state.StepGate.Tokens != null)
            _state.StepGate.Cursor = Mathf.Clamp(stepIndex, 0, _state.StepGate.Tokens.Count - 1);

        PlayStep(_state.NodeIndex, _state.StepGate.Cursor);
    }
    #endregion
    
    
    public bool TryGetCurrentAnchor(out int nodeIndex, out int stepIndex)
    {
        nodeIndex = -1;
        stepIndex = -1;

        if (_sequence == null || _state == null)
            return false;

        nodeIndex = _state.NodeIndex;
        stepIndex = _state.StepGate.Cursor;
        return true;
    }
    

    public bool JumpTo(int nodeIndex, int stepIndex)
    {
        if (_sequence == null || _state == null)
            return false;

        if (nodeIndex < 0 || nodeIndex >= _sequence.nodes.Count)
            return false;

        _executor.Stop();

        _state.NodeIndex = nodeIndex;
        _gatePlanner.BuildForCurrentNode(_sequence, _state);

        if (_state.StepGate.Tokens == null || _state.StepGate.Tokens.Count == 0)
            return false;

        _state.StepGate.Cursor = Mathf.Clamp(
            stepIndex,
            0,
            _state.StepGate.Tokens.Count - 1
        );

        PlayStep(_state.NodeIndex, _state.StepGate.Cursor);
        return true;
    }
}