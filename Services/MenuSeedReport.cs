using System.Collections.Generic;

namespace PhiluWedding.Services;

/// <summary>
/// Outcome of a menu catalog seed run.
///
/// <para>
/// The report distinguishes rows that were created from rows that already
/// existed and were therefore left untouched, and it lists every conflict it
/// noticed (for example a row whose stored name differs from the seed file).
/// </para>
/// </summary>
public sealed class MenuSeedReport
{
    private readonly Dictionary<string, int> _created = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _kept = new(StringComparer.Ordinal);

    /// <summary>Number of rows created for a section.</summary>
    public int Created(string section)
    {
        _created.TryGetValue(section, out var value);
        return value;
    }

    /// <summary>Number of rows that already existed and were left untouched.</summary>
    public int Kept(string section)
    {
        _kept.TryGetValue(section, out var value);
        return value;
    }

    internal void CountCreated(string section, int count = 1) =>
        _created[section] = _created.GetValueOrDefault(section) + count;

    internal void CountKept(string section, int count = 1) =>
        _kept[section] = _kept.GetValueOrDefault(section) + count;

    /// <summary>
    /// Differences between the seed file and rows that already existed. They
    /// are reported, never silently applied.
    /// </summary>
    public List<string> Conflicts { get; } = new();

    /// <summary>Problems that stopped part of the seed from being applied.</summary>
    public List<string> Errors { get; } = new();

    /// <summary>True when nothing went wrong.</summary>
    public bool Succeeded => Errors.Count == 0;

    /// <summary>A short, human readable summary.</summary>
    public string Summary()
    {
        var created = string.Join(", ", _created.OrderBy(k => k.Key, StringComparer.Ordinal)
            .Select(k => $"{k.Key}={k.Value}"));
        var kept = string.Join(", ", _kept.OrderBy(k => k.Key, StringComparer.Ordinal)
            .Select(k => $"{k.Key}={k.Value}"));

        return $"created: {created} | kept: {kept} | conflicts: {Conflicts.Count} | errors: {Errors.Count}";
    }
}
