using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Products.ProductTags.Create;

internal sealed class CreateProductTagCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dtProvider,
    ILogger<CreateProductTagCommandHandler> logger
) : ICommandHandler<CreateProductTagCommand, CreateProductTagResponse>
{
    public async Task<Result<CreateProductTagResponse>> HandleAsync(CreateProductTagCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var dtNow = dtProvider.UtcNow;

            var productTag = ProductTag.CreateNew(command.Name);

            var existing = await dbContext.ProductTags
                .FirstOrDefaultAsync(pu => pu.Name.Normalized == productTag.Name.Normalized,
                    cancellationToken);

            if (existing != null)
            {
                return ProductErrors.TagAlreadyExists(command.Name);
            }

            dbContext.ProductTags.Add(productTag);
            await dbContext.SaveChangesAsync(cancellationToken);

            return productTag.ToCreateResponse();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while adding new product tag '{command.Name}' to DB",
                command.Name);
            return ApplicationErrors.DBOperationError(nameof(CreateProductTagCommandHandler),
                $"DB error has occurred while adding new product tag '{command.Name}' to DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(CreateProductTagCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while adding new product tag '{command.Name}' to DB",
                command.Name);
            return ApplicationErrors.UnexpectedError(nameof(CreateProductTagCommandHandler),
                $"Unexpected error has occurred while adding new product tag '{command.Name}' to DB");
        }
    }
}