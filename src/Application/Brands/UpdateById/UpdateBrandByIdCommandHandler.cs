using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Brands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Brands.UpdateById;

internal sealed class UpdateBrandCommandHandler(
    IApplicationDbContext dbContext,
    ILogger<UpdateBrandCommandHandler> logger
) : ICommandHandler<UpdateBrandByIdCommand, UpdateBrandByIdResponse>
{
    public async Task<Result<UpdateBrandByIdResponse>> HandleAsync(UpdateBrandByIdCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var brand = await dbContext.Brands
                .FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken);

            if (brand == null)
            {
                return BrandErrors.NotFound(command.Id);
            }

            var existingBrand = await dbContext.Brands
                .FirstOrDefaultAsync(b => b.Name == command.Name, cancellationToken);

            if (existingBrand != null
                && existingBrand.Id != brand.Id)
            {
                return BrandErrors.AlreadyExists(command.Name);
            }

            brand.UpdateName(command.Name);
            dbContext.Brands.Update(brand);

            await dbContext.SaveChangesAsync(cancellationToken);
            return new UpdateBrandByIdResponse(brand.Name);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while updating brand with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.DBOperationError(nameof(UpdateBrandCommandHandler),
                $"DB error has occurred while updating brand with id '{command.Id}' to DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(UpdateBrandCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while updating brand with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.UnexpectedError(nameof(UpdateBrandCommandHandler),
                $"Unexpected error has occurred while updating brand with id '{command.Id}' to DB");
        }
    }
}