using CvPlatform.API.Security;
using CvPlatform.Application.Abstractions;
using CvPlatform.Application.DTOs;
using CvPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvPlatform.API.Controllers;

[ApiController, Route("api/[controller]"), Authorize]
public sealed class ProfilesController(IProfileQueries queries, IProfileService service) : ControllerBase
{
    [HttpGet("me")]
    public Task<ProfileEditorDto> Me(CancellationToken cancellationToken) { var actor = User.ToActor(); return queries.EditorAsync(actor, actor.RequireProfileId(), cancellationToken); }
    [HttpGet("{id:guid}/edit")]
    public Task<ProfileEditorDto> Editor(Guid id, CancellationToken cancellationToken) => queries.EditorAsync(User.ToActor(), id, cancellationToken);
    [HttpGet("{id:guid}")]
    public Task<PublicProfileDto> Public(Guid id, CancellationToken cancellationToken) => queries.PublicAsync(User.ToActor(), id, cancellationToken);
    [HttpPut("{id:guid}/autosave")]
    public Task<VersionResponse> Autosave(Guid id, ProfileAutosaveRequest request, CancellationToken cancellationToken) => service.AutosaveAsync(User.ToActor(), id, request, cancellationToken);
    [HttpPost("{id:guid}/projects")]
    public Task<ProjectDto> AddProject(Guid id, SaveProjectRequest request, CancellationToken cancellationToken) => service.AddProjectAsync(User.ToActor(), id, request, cancellationToken);
    [HttpPut("{id:guid}/projects/{projectId:guid}")]
    public Task<ProjectDto> UpdateProject(Guid id, Guid projectId, SaveProjectRequest request, CancellationToken cancellationToken) => service.UpdateProjectAsync(User.ToActor(), id, projectId, request, cancellationToken);
    [HttpDelete("{id:guid}/projects")]
    public async Task<IActionResult> DeleteProjects(Guid id, IdsRequest request, CancellationToken cancellationToken) { await service.DeleteProjectsAsync(User.ToActor(), id, request.Ids, cancellationToken); return NoContent(); }
}
