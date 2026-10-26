using System.Collections.Generic;

namespace PhiluWedding.Services;

/// <summary>
/// Outcome of a menu catalog cleanup run.
///
/// <para>
/// The report separates the rows that were changed from the rows that were
/// already in the intended state, and it lists every row the cleanup
/// expected but did not find. Missing rows are only reported — the cleanup
/// never creates a row.
/// </para>
/// </summary>
public sealed class MenuPruneReport
{
    private readonly Dictionary<string, int> _updated = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _unchanged = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _missing = new(StringComparer.Ordinal);

    /// <summary>Number of rows the cleanup changed.</summary>
    public int Updated(string subject)
    {
        _updated.TryGetValue(subject, out var value);
        return value;
    }

    /// <summary>
    /// Number of rows that were already in the intended state and were
    /// therefore left untouched.
    /// </summary>
    public int Unchanged(string subject)
    {
        _unchanged.TryGetValue(subject, out var value);
        return value;
    }

    /// <summary>
    /// Number of rows the cleanup expected but did not find. The cleanup
    /// never creates them.
    /// </summary>
    public int Missing(string subject)
    {
        _missing.TryGetValue(subject, out var value);
        return value;
    }

    internal void CountUpdated(string subject, int count = 1) =>
        _updated[subject] = _updated.GetValueOrDefault(subject) + count;

    internal void CountUnchanged(string subject, int count = 1) =>
        _unchanged[subject] = _unchanged.GetValueOrDefault(subject) + count;

    internal void CountMissing(string subject, int count = 1) =>
        _missing[subject] = _missing.GetValueOrDefault(subject) + count;

    /// <summary>
    /// Rows the cleanup deliberately did not touch, for example a legacy
    /// option that belongs to no price tier.
    /// </summary>
    public List<string> Conflicts { get; } = new();

    /// <summary>Problems that stopped part of the cleanup from being applied.</summary>
    public List<string> Errors { get; } = new();

    /// <summary>True when nothing went wrong.</summary>
    public bool Succeeded => Errors.Count == 0;

    /// <summary>The total number of rows the cleanup changed.</summary>
    public int TotalChanges => _updated.Values.Sum();

    /// <summary>A short, human readable summary.</summary>
    public string Summary()
    {
        var updated = string.Join(", ", _updated.OrderBy(k => k.Key, StringComparer.Ordinal)
            .Select(k => $"{k.Key}={k.Value}"));
        var unchanged = string.Join(", ", _unchanged.OrderBy(k => k.Key, StringComparer.Ordinal)
            .Select(k => $"{k.Key}={k.Value}"));
        var missing = string.Join(", ", _missing.OrderBy(k => k.Key, StringComparer.Ordinal)
            .Select(k => $"{k.Key}={k.Value}"));

        return $"updated: {updated} | unchanged: {unchanged} | missing: {missing} | conflicts: {Conflicts.Count} | errors: {Errors.Count}";
    }
}
