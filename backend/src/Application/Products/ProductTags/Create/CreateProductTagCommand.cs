using Application.Abstractions.Messaging;

namespace Application.Products.ProductTags.Create;

public sealed record CreateProductTagCommand(string Name) : ICommand<CreateProductTagResponse>;