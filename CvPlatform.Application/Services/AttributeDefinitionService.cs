using CvPlatform.Application.Abstractions;
using CvPlatform.Application.Common;
using CvPlatform.Application.DTOs;
using CvPlatform.Application.Security;
using CvPlatform.Domain.Entities;
using CvPlatform.Domain.Exceptions;
using FluentValidation;

namespace CvPlatform.Application.Services;

public interface IAttributeDefinitionService
{
    Task<IReadOnlyList<AttributeCategoryDto>> CategoriesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AttributeDefinitionDto>> SearchAsync(string? namePrefix, Guid? categoryId, int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<AttributeDefinitionDto>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    Task<AttributeDefinitionDto> CreateAsync(Actor actor, SaveAttributeDefinitionRequest request, CancellationToken cancellationToken);
    Task<AttributeDefinitionDto> UpdateAsync(Actor actor, Guid id, SaveAttributeDefinitionRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Actor actor, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}

public sealed class AttributeDefinitionService(
    IAttributeDefinitionRepository definitions,
    IAttributeCategoryRepository categories,
    IUnitOfWork unitOfWork,
    IValidator<SaveAttributeDefinitionRequest> validator) : IAttributeDefinitionService
{
    public const int MaxPageSize = 50;

    public async Task<IReadOnlyList<AttributeCategoryDto>> CategoriesAsync(CancellationToken cancellationToken) =>
        (await categories.ListAsync(cancellationToken)).Select(x => new AttributeCategoryDto(x.Id, x.Name)).ToArray();

    public async Task<IReadOnlyList<AttributeDefinitionDto>> SearchAsync(string? namePrefix, Guid? categoryId, int take, CancellationToken cancellationToken) =>
        (await definitions.SearchAsync(namePrefix?.Trim(), categoryId, Math.Clamp(take, 1, MaxPageSize), cancellationToken)).Select(x => x.ToDto()).ToArray();

    public async Task<IReadOnlyList<AttributeDefinitionDto>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        ids.Count == 0 ? [] : (await definitions.GetByIdsAsync(ids.Take(MaxPageSize).ToArray(), cancellationToken)).Select(x => x.ToDto()).ToArray();

    public async Task<AttributeDefinitionDto> CreateAsync(Actor actor, SaveAttributeDefinitionRequest request, CancellationToken cancellationToken)
    {
        actor.RequireStaff();
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureUniqueNameAsync(request.Name, null, cancellationToken);
        await EnsureCategoryAsync(request.CategoryId, cancellationToken);

        var definition = new AttributeDefinition(request.Name, request.Description, request.DataType, request.CategoryId);
        definition.SetOptions(request.DropdownOptions ?? []);
        await definitions.AddAsync(definition, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await definitions.GetByIdAsync(definition.Id, cancellationToken))!.ToDto();
    }

    public async Task<AttributeDefinitionDto> UpdateAsync(Actor actor, Guid id, SaveAttributeDefinitionRequest request, CancellationToken cancellationToken)
    {
        actor.RequireStaff();
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var definition = await definitions.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Attribute '{id}' does not exist.");
        if (request.Version is not { } version)
        {
            throw new InvalidOperationException("Version is required to update an attribute.");
        }

        unitOfWork.ExpectVersion(definition, version);
        await EnsureUniqueNameAsync(request.Name, id, cancellationToken);
        await EnsureCategoryAsync(request.CategoryId, cancellationToken);

        definition.Update(request.Name, request.Description, request.DataType, request.CategoryId);
        definition.SetOptions(request.DropdownOptions ?? []);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await definitions.GetByIdAsync(id, cancellationToken))!.ToDto();
    }

    public async Task DeleteAsync(Actor actor, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        actor.RequireStaff();
        var found = await definitions.GetByIdsAsync(ids, cancellationToken);
        foreach (var definition in found)
        {
            definition.EnsureCanBeDeleted();
            definitions.Remove(definition);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureUniqueNameAsync(string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        if (await definitions.NameExistsAsync(name.Trim(), exceptId, cancellationToken))
        {
            throw new InvalidOperationException($"Attribute '{name.Trim()}' already exists. Attribute names are globally unique.");
        }
    }

    private async Task EnsureCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        _ = await categories.GetByIdAsync(categoryId, cancellationToken)
            ?? throw new InvalidOperationException("Unknown attribute category.");
    }
}
