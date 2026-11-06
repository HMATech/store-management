using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Brands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shared;

namespace Application.Brands.DeleteById;

internal sealed class DeleteBrandByIdCommandHandler(
    IApplicationDbContext dbContext,
    ILogger<DeleteBrandByIdCommandHandler> logger
) : ICommandHandler<DeleteBrandByIdCommand>
{
    public async Task<Result> HandleAsync(DeleteBrandByIdCommand command,
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

            dbContext.Brands.Remove(brand);

            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Succeed();
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is PostgresException pex
                && pex.SqlState == ApplicationErrorDefaults.PostgreSQLOnDeleteRestrictViolationCode)
            {
                logger.LogWarning("Attempting to delete brand with id '{command.Id}' " +
                    "that is still referenced by other entities",
                    command.Id);
                return BrandErrors.InUse(command.Id);
            }

            logger.LogError(ex, "DB error has occurred while deleting brand with id '{command.Id}' from DB",
                command.Id);
            return ApplicationErrors.DBOperationError(nameof(DeleteBrandByIdCommandHandler),
                $"DB error has occurred while deleting brand with id '{command.Id}' from DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(DeleteBrandByIdCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while deleting brand with id '{command.Id}' from DB",
                command.Id);
            return ApplicationErrors.UnexpectedError(nameof(DeleteBrandByIdCommandHandler),
                $"Unexpected error has occurred while deleting brand with id '{command.Id}' from DB");
        }
    }
}