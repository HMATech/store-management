using Application.Abstractions.Messaging;

namespace Application.Products.ProductTags.UpdateById;

public sealed record UpdateProductTagByIdCommand(int Id, string Name)
    : ICommand<UpdateProductTagByIdResponse>;