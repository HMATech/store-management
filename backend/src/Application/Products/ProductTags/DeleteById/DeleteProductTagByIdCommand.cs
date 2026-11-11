using Application.Abstractions.Messaging;

namespace Application.Products.ProductTags.DeleteById;

public sealed record DeleteProductTagByIdCommand(int Id)
    : ICommand;