using System;
using System.Collections.Generic;
using Yarn.Markup;

public sealed class InlineAdvanceManifest
{
    public const string DefaultMarkerName = "advance";

    private readonly int[] _positions;
    private int _cursor;

    public static InlineAdvanceManifest Empty { get; } =
        new(Array.Empty<int>());

    private InlineAdvanceManifest(int[] positions)
    {
        _positions = positions;
    }

    public int Count => _positions.Length;
    public int ConsumedCount => _cursor;
    public bool IsEmpty => _positions.Length == 0;
    public bool IsExhausted => _cursor >= _positions.Length;

    public static InlineAdvanceManifest Build(
        MarkupParseResult markup,
        string markerName = DefaultMarkerName)
    {
        if (markup.Attributes == null || markup.Attributes.Count == 0)
            return Empty;

        string text = markup.Text ?? string.Empty;
        int textLength = text.Length;

        List<int> positions = null;

        for (int i = 0; i < markup.Attributes.Count; i++)
        {
            MarkupAttribute attribute = markup.Attributes[i];

            if (!string.Equals(attribute.Name, markerName, StringComparison.Ordinal))
                continue;

            // Inline advance is a point event only.
            // Range [advance]...[/advance] is not part of this contract.
            if (attribute.Length > 0)
                continue;

            // With the current typewriter, empty text has no character callback.
            // Use presentation beat / command line for textless presentation advance.
            if (textLength <= 0)
                continue;

            int position = attribute.Position;

            if (position < 0)
                position = 0;

            // Current ActionMarkupHandler fires only before visible characters.
            // A point marker at text end would otherwise never be observed.
            if (position >= textLength)
                position = textLength - 1;

            (positions ??= new List<int>()).Add(position);
        }

        if (positions == null)
            return Empty;

        positions.Sort();
        return new InlineAdvanceManifest(positions.ToArray());
    }

    public bool HasPendingAt(int charIndex)
    {
        return !IsExhausted && _positions[_cursor] <= charIndex;
    }

    public bool TryConsumeNext(out int ordinal)
    {
        if (IsExhausted)
        {
            ordinal = -1;
            return false;
        }

        ordinal = _cursor;
        _cursor++;
        return true;
    }

    public int DrainRemaining()
    {
        int remaining = _positions.Length - _cursor;
        _cursor = _positions.Length;
        return remaining;
    }

    public void Reset()
    {
        _cursor = 0;
    }
}