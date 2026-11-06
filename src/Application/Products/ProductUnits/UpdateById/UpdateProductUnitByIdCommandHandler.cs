using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Products.ProductUnits.UpdateById;

internal sealed class UpdateProductUnitByIdCommandHandler(
    IApplicationDbContext dbContext,
    ILogger<UpdateProductUnitByIdCommandHandler> logger
) : ICommandHandler<UpdateProductUnitByIdCommand, UpdateProductUnitByIdResponse>
{
    public async Task<Result<UpdateProductUnitByIdResponse>> HandleAsync(UpdateProductUnitByIdCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var productUnit = await dbContext.ProductUnits
                .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

            if (productUnit == null)
            {
                return ProductErrors.UnitNotFound(command.Id);
            }

            var inputUnit = ProductUnit.CreateNew(command.Name);

            var existingProductUnit = await dbContext.ProductUnits
                .Select(p => new { p.Id, p.Name.Normalized })
                .FirstOrDefaultAsync(p => p.Normalized == inputUnit.Name.Normalized, cancellationToken);

            if (existingProductUnit != null)
            {
                if (existingProductUnit.Id != productUnit.Id)
                {
                    return ProductErrors.UnitAlreadyExists(command.Name);
                }

                if (productUnit.Name.Value == command.Name)
                {
                    return productUnit.ToUpdateByIdResponse();
                }
            }

            productUnit.UpdateName(command.Name);

            await dbContext.SaveChangesAsync(cancellationToken);

            return productUnit.ToUpdateByIdResponse();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while updating product unit with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.DBOperationError(nameof(UpdateProductUnitByIdCommandHandler),
                $"DB error has occurred while updating product unit with id '{command.Id}' to DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(UpdateProductUnitByIdCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while updating product unit " +
                "with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.UnexpectedError(nameof(UpdateProductUnitByIdCommandHandler),
                "Unexpected error has occurred while updating product unit " +
                $"with id '{command.Id}' to DB");
        }
    }
}