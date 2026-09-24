using CvPlatform.Application.Abstractions;
using CvPlatform.Application.DTOs;
using CvPlatform.Application.Security;
using CvPlatform.Domain.Entities;
using CvPlatform.Domain.Enums;
using CvPlatform.Domain.Exceptions;
using FluentValidation;

namespace CvPlatform.Application.Services;

public interface IPositionService
{
    Task<VersionResponse> CreateAsync(Actor actor, SavePositionRequest request, CancellationToken cancellationToken);
    Task<VersionResponse> SaveAsync(Actor actor, Guid id, SavePositionRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Actor actor, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    Task<VersionResponse> DuplicateAsync(Actor actor, Guid id, CancellationToken cancellationToken);
}

/// <summary>All recruiters share all positions: there is no ownership, any recruiter (or admin) may change any position.</summary>
public sealed class PositionService(
    IPositionRepository positions,
    IAttributeDefinitionRepository definitions,
    IUnitOfWork unitOfWork,
    IValidator<SavePositionRequest> validator) : IPositionService
{
    public async Task<VersionResponse> CreateAsync(Actor actor, SavePositionRequest request, CancellationToken cancellationToken)
    {
        actor.RequireStaff();
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var position = new Position(request.Title, request.ShortDescription, request.IsPublic);
        await ApplyAsync(position, request, cancellationToken);
        await positions.AddAsync(position, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new VersionResponse(position.Id, position.Version);
    }

    public async Task<VersionResponse> SaveAsync(Actor actor, Guid id, SavePositionRequest request, CancellationToken cancellationToken)
    {
        actor.RequireStaff();
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var position = await positions.GetByIdWithDetailsAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Position '{id}' does not exist.");
        if (request.Version is not { } version)
        {
            throw new InvalidOperationException("Version is required to update a position.");
        }

        unitOfWork.ExpectVersion(position, version);
        await ApplyAsync(position, request, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new VersionResponse(position.Id, position.Version);
    }

    public async Task DeleteAsync(Actor actor, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        actor.RequireStaff();
        foreach (var position in await positions.GetByIdsAsync(ids, cancellationToken))
        {
            positions.Remove(position);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<VersionResponse> DuplicateAsync(Actor actor, Guid id, CancellationToken cancellationToken)
    {
        actor.RequireStaff();
        var position = await positions.GetByIdWithDetailsAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Position '{id}' does not exist.");
        var copy = position.Duplicate();
        await positions.AddAsync(copy, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new VersionResponse(copy.Id, copy.Version);
    }

    private async Task ApplyAsync(Position position, SavePositionRequest request, CancellationToken cancellationToken)
    {
        var attributeIds = request.AttributeIds ?? [];
        var rules = request.AccessRules ?? [];
        var referenced = attributeIds.Concat(rules.Select(x => x.AttributeDefinitionId)).Distinct().ToArray();
        var found = (await definitions.GetByIdsAsync(referenced, cancellationToken)).ToDictionary(x => x.Id);
        var unknown = referenced.Where(x => !found.ContainsKey(x)).ToArray();
        if (unknown.Length > 0)
        {
            throw new InvalidOperationException("Some selected attributes no longer exist. Reload the page.");
        }

        var ruleSpecs = rules.Select(rule =>
        {
            var definition = found[rule.AttributeDefinitionId];
            ValidateRuleValue(definition, rule);
            return new PositionRuleSpec(definition, rule.Operator, rule.ComparisonValue);
        }).ToArray();

        position.UpdateBasics(request.Title, request.ShortDescription, request.Company, request.Level, request.IsPublic, request.MaxProjects, request.ProjectTags ?? []);
        position.SetAttributes(attributeIds);
        position.SetAccessRules(ruleSpecs);
    }

    private static void ValidateRuleValue(AttributeDefinition definition, AccessRuleInput rule)
    {
        var valid = definition.DataType switch
        {
            AttributeDataType.Numeric => decimal.TryParse(rule.ComparisonValue, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out _),
            AttributeDataType.Date or AttributeDataType.Period => DateTime.TryParse(rule.ComparisonValue, System.Globalization.CultureInfo.InvariantCulture, out _),
            AttributeDataType.Boolean => bool.TryParse(rule.ComparisonValue, out _),
            AttributeDataType.Dropdown => rule.ComparisonValue
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .All(id => definition.Options.Any(o => o.Id.ToString() == id)),
            _ => true
        };

        if (!valid)
        {
            throw new InvalidOperationException($"Invalid comparison value for '{definition.Name}'.");
        }
    }
}
