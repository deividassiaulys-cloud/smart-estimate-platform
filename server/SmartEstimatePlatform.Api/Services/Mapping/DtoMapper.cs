using SmartEstimatePlatform.Api.Models.DTOs.Auth;
using SmartEstimatePlatform.Api.Models.DTOs.Estimates;
using SmartEstimatePlatform.Api.Models.DTOs.Materials;
using SmartEstimatePlatform.Api.Models.DTOs.Projects;
using SmartEstimatePlatform.Api.Models.DTOs.Team;
using SmartEstimatePlatform.Api.Models.DTOs.Works;
using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services.Mapping;

/// <summary>
/// The one place where entities are turned into response DTOs, so DTOs never depend on entities.
/// </summary>
public static class DtoMapper
{
    /// <summary>
    /// A material as shown in the Manager's catalog.
    /// </summary>
    public static MaterialResponse ToResponse(this Material material) => new()
    {
        Id = material.Id,
        Name = material.Name,
        Unit = material.Unit,
        Price = material.Price
    };

    /// <summary>
    /// A member of the Manager's company as shown on the Team page.
    /// </summary>
    public static TeamMemberResponse ToResponse(this User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Role = user.Role
    };

    /// <summary>
    /// The logged-in user as shown on the Dashboard. The user's company must be loaded.
    /// </summary>
    public static CurrentUserResponse ToCurrentUserResponse(this User user) => new()
    {
        FirstName = user.FirstName,
        LastName = user.LastName,
        CompanyName = user.Company.Name,
        Role = user.Role
    };

    /// <summary>
    /// The result of a registration or login, carrying the token issued for the user.
    /// </summary>
    public static AuthResponse ToAuthResponse(this User user, string token) => new()
    {
        Token = token,
        UserId = user.Id,
        CompanyId = user.CompanyId,
        Role = user.Role
    };

    /// <summary>
    /// A work with its composition. Each line's material must be loaded.
    /// </summary>
    public static WorkResponse ToResponse(this Work work) => new()
    {
        Id = work.Id,
        Name = work.Name,
        Unit = work.Unit,
        LabourRate = work.LabourRate,
        LabourNorm = work.LabourNorm,
        Lines = work.Lines.Select(line => new WorkMaterialLineResponse
        {
            Id = line.Id,
            MaterialId = line.MaterialId,
            MaterialName = line.Material.Name,
            MaterialUnit = line.Material.Unit,
            ConsumptionNorm = line.ConsumptionNorm,
            WastePercent = line.WastePercent
        }).ToList()
    };

    /// <summary>
    /// A project as shown to every user of its company.
    /// </summary>
    public static ProjectResponse ToResponse(this Project project) => new()
    {
        Id = project.Id,
        Name = project.Name,
        Description = project.Description
    };

    /// <summary>
    /// A whole estimate with the amounts from its <see cref="EstimateCalculation"/>.
    /// </summary>
    public static EstimateResponse ToResponse(this Estimate estimate, EstimateCalculation calculation) => new()
    {
        Id = estimate.Id,
        ProjectId = estimate.ProjectId,
        Name = estimate.Name,
        Status = estimate.Status,
        OverheadPercent = estimate.OverheadPercent,
        ProfitPercent = estimate.ProfitPercent,
        VatPercent = estimate.VatPercent,
        WorkLines = calculation.WorkLines.Select(work => new EstimateWorkLineResponse
        {
            Id = work.Line.Id,
            WorkName = work.Line.WorkName,
            WorkUnit = work.Line.WorkUnit,
            Quantity = work.Line.Quantity,
            LabourNorm = work.Line.LabourNorm,
            LabourRate = work.Line.LabourRate,
            LabourCost = work.LabourCost,
            MaterialLines = work.MaterialLines.Select(material => new EstimateMaterialLineResponse
            {
                Id = material.Line.Id,
                MaterialName = material.Line.MaterialName,
                MaterialUnit = material.Line.MaterialUnit,
                ConsumptionNorm = material.Line.ConsumptionNorm,
                WastePercent = material.Line.WastePercent,
                Quantity = material.Quantity,
                Price = material.Line.Price,
                Cost = material.Cost
            }).ToList()
        }).ToList(),
        DirectCosts = calculation.DirectCosts,
        Overhead = calculation.Overhead,
        Profit = calculation.Profit,
        Vat = calculation.Vat,
        Total = calculation.Total
    };

    /// <summary>
    /// An estimate as listed on Project Detail, with the total from its <see cref="EstimateCalculation"/>.
    /// </summary>
    public static EstimateSummaryResponse ToSummary(this Estimate estimate, EstimateCalculation calculation) => new()
    {
        Id = estimate.Id,
        Name = estimate.Name,
        Status = estimate.Status,
        Total = calculation.Total
    };

    /// <summary>
    /// An estimate as listed for any user: ID and name only.
    /// </summary>
    public static EstimateNameResponse ToNameResponse(this Estimate estimate) => new()
    {
        Id = estimate.Id,
        Name = estimate.Name
    };

    /// <summary>
    /// An estimate's work and material names, in the order they were added; nothing commercial.
    /// </summary>
    public static EstimateNamesResponse ToNamesResponse(this Estimate estimate) => new()
    {
        Name = estimate.Name,
        Works = estimate.WorkLines.OrderBy(w => w.Id).Select(work => new WorkNamesResponse
        {
            WorkName = work.WorkName,
            MaterialNames = work.MaterialLines.OrderBy(m => m.Id).Select(m => m.MaterialName).ToList()
        }).ToList()
    };

    /// <summary>
    /// A catalog refresh's changes and lines not found, with the totals before and after it.
    /// </summary>
    public static EstimateRefreshPreviewResponse ToPreviewResponse(this CatalogRefreshResult result, decimal currentTotal, decimal newTotal) => new()
    {
        Changes = result.Changes.Select(c => new RefreshChangeResponse
        {
            WorkName = c.WorkName,
            MaterialName = c.MaterialName,
            Field = c.Field,
            OldValue = c.OldValue,
            NewValue = c.NewValue
        }).ToList(),
        NotFound = result.NotFound.Select(m => new RefreshMissResponse { WorkName = m.WorkName, MaterialName = m.MaterialName }).ToList(),
        CurrentTotal = currentTotal,
        NewTotal = newTotal
    };
}
