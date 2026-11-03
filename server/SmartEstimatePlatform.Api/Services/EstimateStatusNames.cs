using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Lithuanian estimate status names for generated files. They must match the client's estimateStatusNames
/// (client/src/utils/estimateStatus.ts); server and client cannot share code, so each side has its own test.
/// </summary>
public static class EstimateStatusNames
{
    /// <summary>
    /// The status as shown to users.
    /// </summary>
    public static string Of(EstimateStatus status) => status switch
    {
        EstimateStatus.Draft => "Juodraštis",
        EstimateStatus.Confirmed => "Patvirtinta",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}
