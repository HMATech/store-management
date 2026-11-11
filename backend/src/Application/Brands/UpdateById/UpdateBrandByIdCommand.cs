using Application.Abstractions.Messaging;

namespace Application.Brands.UpdateById;

public sealed record UpdateBrandByIdCommand(int Id, string Name)
    : ICommand<UpdateBrandByIdResponse>;