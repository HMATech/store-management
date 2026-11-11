using Application.Abstractions.Messaging;

namespace Application.Brands.Create;

public sealed record CreateBrandCommand(string Name)
    : ICommand<CreateBrandResponse>;