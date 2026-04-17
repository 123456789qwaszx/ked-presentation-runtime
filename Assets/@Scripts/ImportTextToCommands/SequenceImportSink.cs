using System.Collections.Generic;

public sealed class SequenceImportSink : ICommandSpecSink
{
    private readonly List<ImportedStepDraft> _steps = new();

    private bool _isHoldActive;
    private ImportedStepDraft _holdStep;

    public IReadOnlyList<ImportedStepDraft> Steps => _steps;

    public void BeginHold()
    {
        if (_isHoldActive)
            return;

        _isHoldActive = true;
        _holdStep = new ImportedStepDraft();
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

    public void Enqueue(CommandSpecBase spec)
    {
        if (spec == null)
            return;

        if (_isHoldActive)
        {
            if (_holdStep == null)
                _holdStep = new ImportedStepDraft();

            _holdStep.commands.Add(spec);
            return;
        }

        var step = new ImportedStepDraft();
        step.commands.Add(spec);
        _steps.Add(step);
    }
}

public sealed class ImportedStepDraft
{
    public string editorName;
    public GateToken gate;
    public readonly List<CommandSpecBase> commands = new();
}