using System.Collections.Generic;
using System.Text;

public sealed class VNStoryGraphValidationResult
{
    private readonly List<VNStoryGraphValidationMessage> _messages =
        new List<VNStoryGraphValidationMessage>();

    public IReadOnlyList<VNStoryGraphValidationMessage> Messages
    {
        get { return _messages; }
    }

    public bool HasError
    {
        get
        {
            for (int i = 0; i < _messages.Count; i++)
            {
                if (_messages[i].severity == VNStoryValidationSeverity.Error)
                    return true;
            }

            return false;
        }
    }

    public bool HasWarning
    {
        get
        {
            for (int i = 0; i < _messages.Count; i++)
            {
                if (_messages[i].severity == VNStoryValidationSeverity.Warning)
                    return true;
            }

            return false;
        }
    }

    public void Add(
        VNStoryValidationSeverity severity,
        string nodeId,
        string message)
    {
        _messages.Add(new VNStoryGraphValidationMessage
        {
            severity = severity,
            nodeId = nodeId,
            message = message
        });
    }

    public string ToReport()
    {
        if (_messages.Count == 0)
            return "Validation passed. No issues found.";

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < _messages.Count; i++)
        {
            VNStoryGraphValidationMessage msg = _messages[i];

            sb.Append("[");
            sb.Append(msg.severity);
            sb.Append("] ");

            if (!string.IsNullOrWhiteSpace(msg.nodeId))
            {
                sb.Append("(");
                sb.Append(msg.nodeId);
                sb.Append(") ");
            }

            sb.AppendLine(msg.message);
        }

        return sb.ToString();
    }
}

public struct VNStoryGraphValidationMessage
{
    public VNStoryValidationSeverity severity;
    public string nodeId;
    public string message;
}