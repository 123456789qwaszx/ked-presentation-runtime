using System.Collections.Generic;

public sealed class SequenceImportSink : ICommandSpecSink
{
    private readonly List<ImportedStepDraft> _steps = new();

    private bool _isHoldActive;
    private ImportedStepDraft _holdStep;

    private string _pendingStepLabel;
    private GateToken _pendingGate = GateToken.Immediately();
    private bool _hasPendingGate;

    public IReadOnlyList<ImportedStepDraft> Steps => _steps;

    public void BeginHold()
    {
        if (_isHoldActive)
            return;

        _isHoldActive = true;
        _holdStep = new ImportedStepDraft();

        ApplyPendingMetaTo(_holdStep);
    }

    public void EndHold()
    {
        if (!_isHoldActive)
            return;

        _isHoldActive = false;

        if (_holdStep != null && _holdStep.commands.Count > 0)
            _steps.Add(_holdStep);

        _holdStep = null;
    }

    public void SetStepLabel(string label)
    {
        if (_isHoldActive)
        {
            if (_holdStep == null)
                _holdStep = new ImportedStepDraft();

            _holdStep.editorName = label;
            return;
        }

        _pendingStepLabel = label;
    }

    public void SetGate(GateToken gate)
    {
        if (_isHoldActive)
        {
            if (_holdStep == null)
                _holdStep = new ImportedStepDraft();

            _holdStep.gate = gate;
            return;
        }

        _pendingGate = gate;
        _hasPendingGate = true;
    }

    public void Enqueue(CommandSpecBase spec)
    {
        if (spec == null)
            return;

        if (_isHoldActive)
        {
            if (_holdStep == null)
            {
                _holdStep = new ImportedStepDraft();
                ApplyPendingMetaTo(_holdStep);
            }

            _holdStep.commands.Add(spec);
            return;
        }

        var step = new ImportedStepDraft();
        ApplyPendingMetaTo(step);

        step.commands.Add(spec);
        _steps.Add(step);
    }

    private void ApplyPendingMetaTo(ImportedStepDraft step)
    {
        if (step == null)
            return;

        if (!string.IsNullOrWhiteSpace(_pendingStepLabel))
            step.editorName = _pendingStepLabel;

        if (_hasPendingGate)
            step.gate = _pendingGate;

        ClearPendingMeta();
    }

    private void ClearPendingMeta()
    {
        _pendingStepLabel = null;
        _pendingGate = GateToken.Immediately();
        _hasPendingGate = false;
    }
}

public sealed class ImportedStepDraft
{
    public string editorName;
    public GateToken gate = GateToken.Immediately();
    public readonly List<CommandSpecBase> commands = new();
}