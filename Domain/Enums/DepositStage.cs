namespace PhiluWedding.Domain.Enums;

/// <summary>
/// The deposit milestones a wedding banquet contract is paid in.
/// </summary>
public enum DepositStage
{
    /// <summary>First deposit — reserves the banquet date.</summary>
    First = 1,

    /// <summary>Second deposit — confirms the chosen menu.</summary>
    Second = 2,

    /// <summary>Final payment — settled right after the event ends.</summary>
    Final = 3
}
