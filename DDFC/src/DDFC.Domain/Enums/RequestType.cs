namespace DDFC.Domain.Enums;

public enum RequestType
{
    /// <summary>Standard NOC/NDC Possession &amp; House Design workflow.</summary>
    PossessionDesign = 0,
    /// <summary>Revised house plan workflow (no Transfer/Finance/Possession steps).</summary>
    RevisedPlan = 1,
    /// <summary>As-built plan workflow (no Transfer/Finance/Possession steps).</summary>
    AsBuiltPlan = 2,
}
