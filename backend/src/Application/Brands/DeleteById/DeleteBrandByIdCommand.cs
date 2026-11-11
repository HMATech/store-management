using Application.Abstractions.Messaging;

namespace Application.Brands.DeleteById;

public sealed record DeleteBrandByIdCommand(int Id)
    : ICommand;