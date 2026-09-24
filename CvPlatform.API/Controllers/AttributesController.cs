using CvPlatform.API.Security;
using CvPlatform.Application.DTOs;
using CvPlatform.Application.Security;
using CvPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvPlatform.API.Controllers;

[ApiController, Route("api/[controller]")]
public sealed class AttributesController(IAttributeDefinitionService service) : ControllerBase
{
    [HttpGet, Authorize]
    public Task<IReadOnlyList<AttributeDefinitionDto>> Search([FromQuery] string? namePrefix, [FromQuery] Guid? categoryId, [FromQuery] int take = 50, CancellationToken cancellationToken = default) => service.SearchAsync(namePrefix, categoryId, take, cancellationToken);

    [HttpGet("categories"), Authorize]
    public Task<IReadOnlyList<AttributeCategoryDto>> Categories(CancellationToken cancellationToken) => service.CategoriesAsync(cancellationToken);

    [HttpPost, Authorize(Roles = RoleNames.StaffRoles)]
    public Task<AttributeDefinitionDto> Create(SaveAttributeDefinitionRequest request, CancellationToken cancellationToken) => service.CreateAsync(User.ToActor(), request, cancellationToken);

    [HttpPut("{id:guid}"), Authorize(Roles = RoleNames.StaffRoles)]
    public Task<AttributeDefinitionDto> Update(Guid id, SaveAttributeDefinitionRequest request, CancellationToken cancellationToken) => service.UpdateAsync(User.ToActor(), id, request, cancellationToken);

    [HttpDelete, Authorize(Roles = RoleNames.StaffRoles)]
    public async Task<IActionResult> Delete(IdsRequest request, CancellationToken cancellationToken) { await service.DeleteAsync(User.ToActor(), request.Ids, cancellationToken); return NoContent(); }
}
