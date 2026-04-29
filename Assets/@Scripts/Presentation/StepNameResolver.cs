using System;
using System.Collections.Generic;

public static class StepNameResolver
{
    public static bool TryResolveUnique(
        SequenceSpecSO seq,
        string editorName,
        out int nodeIndex,
        out int stepIndex,
        out int matchCount,
        List<(int n, int s)> matchesBuffer = null)
    {
        nodeIndex = 0;
        stepIndex = 0;
        matchCount = 0;

        if (seq == null || string.IsNullOrEmpty(editorName) || seq.nodes == null)
            return false;

        matchesBuffer?.Clear();

        for (int n = 0; n < seq.nodes.Count; n++)
        {
            var node = seq.nodes[n];
            if (node?.steps == null) continue;

            for (int s = 0; s < node.steps.Count; s++)
            {
                var step = node.steps[s];
                if (step == null) continue;

                if (string.Equals(step.editorName, editorName, StringComparison.Ordinal))
                {
                    matchCount++;
                    if (matchCount == 1)
                    {
                        nodeIndex = n;
                        stepIndex = s;
                    }

                    matchesBuffer?.Add((n, s));
                }
            }
        }

        return matchCount == 1;
    }
    
    public static bool TryResolveUnique(
        SequenceSpecSO seq,
        string editorName,
        out int nodeIndex,
        out int stepIndex,
        out int matchCount)
    {
        return TryResolveUnique(seq, editorName, out nodeIndex, out stepIndex, out matchCount, null);
    }
}