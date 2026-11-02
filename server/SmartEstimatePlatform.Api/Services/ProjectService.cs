using Microsoft.EntityFrameworkCore;
using SmartEstimatePlatform.Api.Data;
using SmartEstimatePlatform.Api.Models.DTOs.Projects;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services.Mapping;

namespace SmartEstimatePlatform.Api.Services;

/// <inheritdoc cref="IProjectService" />
public class ProjectService(AppDbContext db) : IProjectService
{
    /// <inheritdoc />
    public async Task<List<ProjectResponse>> ListAsync()
    {
        var projects = await db.Projects.OrderBy(p => p.Name).ThenBy(p => p.Id).ToListAsync();
        return projects.Select(p => p.ToResponse()).ToList();
    }

    /// <inheritdoc />
    public async Task<ProjectResponse> GetAsync(int id) =>
        (await FindAsync(id)).ToResponse();

    /// <inheritdoc />
    public async Task<ProjectResponse> CreateAsync(ProjectRequest request)
    {
        var project = new Project(request.Name.Trim(), TrimmedDescription(request));
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return project.ToResponse();
    }

    /// <inheritdoc />
    public async Task<ProjectResponse> UpdateAsync(int id, ProjectRequest request)
    {
        var project = await FindAsync(id);
        project.Update(request.Name.Trim(), TrimmedDescription(request));
        await db.SaveChangesAsync();
        return project.ToResponse();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        var project = await FindAsync(id);
        db.Projects.Remove(project);
        await db.SaveChangesAsync();
    }

    private async Task<Project> FindAsync(int id) =>
        await db.Projects.SingleOrDefaultAsync(p => p.Id == id)
        ?? throw new NotFoundException("Project not found.");

    // A description of only spaces is stored as no description.
    private static string? TrimmedDescription(ProjectRequest request) =>
        string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
}
