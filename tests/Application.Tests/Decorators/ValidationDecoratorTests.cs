using Application.Abstractions.Messaging;
using Application.Decorators;
using FluentValidation;
using Shared;

namespace Application.Tests.Decorators;

public class ValidationDecoratorTests
{
    [Fact]
    public async Task Command_WithValidInput_CallsHandler()
    {
        var inner = new SpyHandler();
        var decorator = new ValidationDecorator.CommandHandler<NoResponseCommand>(inner, [new NameValidator()]);

        var result = await decorator.HandleAsync(new NoResponseCommand("ok"), default);

        Assert.True(result.IsSuccess);
        Assert.True(inner.WasCalled);
    }

    [Fact]
    public async Task Command_WithInvalidInput_ReturnsValidationErrorWithoutCallingHandler()
    {
        var inner = new SpyHandler();
        var decorator = new ValidationDecorator.CommandHandler<NoResponseCommand>(inner, [new NameValidator()]);

        var result = await decorator.HandleAsync(new NoResponseCommand(""), default);

        Assert.True(result.IsFailed);
        Assert.False(inner.WasCalled);
        var error = Assert.IsType<ValidationError>(result.Error);
        Assert.Equal(ErrorType.Problem, Assert.Single(error.FieldErrors["Name"]).ErrorType);
    }

    [Fact]
    public async Task Command_WithNoValidators_CallsHandler()
    {
        var inner = new SpyHandler();
        var decorator = new ValidationDecorator.CommandHandler<NoResponseCommand>(inner, []);

        var result = await decorator.HandleAsync(new NoResponseCommand(""), default);

        Assert.True(result.IsSuccess);
        Assert.True(inner.WasCalled);
    }

    [Fact]
    public async Task Command_WithSeveralFailingValidators_CombinesErrorsByField()
    {
        var inner = new SpyHandler();
        var decorator = new ValidationDecorator.CommandHandler<NoResponseCommand>(inner,
            [new NameValidator(), new NameValidator()]);

        var result = await decorator.HandleAsync(new NoResponseCommand(""), default);

        var error = Assert.IsType<ValidationError>(result.Error);
        Assert.Equal(2, error.FieldErrors["Name"].Count);
    }

    [Fact]
    public async Task Command_WithResponse_AndValidInput_ReturnsHandlerValue()
    {
        var inner = new SpyResponseHandler();
        var decorator = new ValidationDecorator.CommandHandler<ResponseCommand, string>(inner,
            [new ResponseNameValidator()]);

        var result = await decorator.HandleAsync(new ResponseCommand("ok"), default);

        Assert.Equal("handled", result.Value);
    }

    [Fact]
    public async Task Command_WithResponse_AndInvalidInput_ReturnsValidationErrorWithoutCallingHandler()
    {
        var inner = new SpyResponseHandler();
        var decorator = new ValidationDecorator.CommandHandler<ResponseCommand, string>(inner,
            [new ResponseNameValidator()]);

        var result = await decorator.HandleAsync(new ResponseCommand(""), default);

        Assert.True(result.IsFailed);
        Assert.False(inner.WasCalled);
        Assert.IsType<ValidationError>(result.Error);
    }

    [Fact]
    public async Task Query_WithValidInput_ReturnsHandlerValue()
    {
        var inner = new SpyQueryHandler();
        var decorator = new ValidationDecorator.QueryHandler<SomeQuery, string>(inner, [new PageValidator()]);

        var result = await decorator.HandleAsync(new SomeQuery(1), default);

        Assert.Equal("queried", result.Value);
    }

    [Fact]
    public async Task Query_WithInvalidInput_ReturnsValidationErrorWithoutCallingHandler()
    {
        var inner = new SpyQueryHandler();
        var decorator = new ValidationDecorator.QueryHandler<SomeQuery, string>(inner, [new PageValidator()]);

        var result = await decorator.HandleAsync(new SomeQuery(0), default);

        Assert.True(result.IsFailed);
        Assert.False(inner.WasCalled);
        Assert.IsType<ValidationError>(result.Error);
    }

    [Fact]
    public async Task Query_WithNoValidators_CallsHandler()
    {
        var inner = new SpyQueryHandler();
        var decorator = new ValidationDecorator.QueryHandler<SomeQuery, string>(inner, []);

        var result = await decorator.HandleAsync(new SomeQuery(0), default);

        Assert.True(result.IsSuccess);
        Assert.True(inner.WasCalled);
    }

    [Fact]
    public async Task Command_WhenHandlerFails_ReturnsHandlerError()
    {
        var error = Error.Conflict("Some.Code", "Conflict");
        var decorator = new ValidationDecorator.CommandHandler<NoResponseCommand>(new SpyHandler(error), []);

        var result = await decorator.HandleAsync(new NoResponseCommand("ok"), default);

        Assert.Equal(error, result.Error);
    }

    public sealed record SomeQuery(int Page) : IQuery<string>;

    public sealed record NoResponseCommand(string Name) : ICommand;

    public sealed record ResponseCommand(string Name) : ICommand<string>;

    private sealed class PageValidator : AbstractValidator<SomeQuery>
    {
        public PageValidator()
        {
            RuleFor(q => q.Page).GreaterThan(0);
        }
    }

    private sealed class SpyQueryHandler : IQueryHandler<SomeQuery, string>
    {
        public bool WasCalled { get; private set; }

        public Task<Result<string>> HandleAsync(SomeQuery query, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(Result.Succeed("queried"));
        }
    }

    private sealed class NameValidator : AbstractValidator<NoResponseCommand>
    {
        public NameValidator()
        {
            RuleFor(c => c.Name).NotEmpty();
        }
    }

    private sealed class ResponseNameValidator : AbstractValidator<ResponseCommand>
    {
        public ResponseNameValidator()
        {
            RuleFor(c => c.Name).NotEmpty();
        }
    }

    private sealed class SpyHandler(Error? error = null) : ICommandHandler<NoResponseCommand>
    {
        public bool WasCalled { get; private set; }

        public Task<Result> HandleAsync(NoResponseCommand command, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(error == null ? Result.Succeed() : Result.Fail(error));
        }
    }

    private sealed class SpyResponseHandler : ICommandHandler<ResponseCommand, string>
    {
        public bool WasCalled { get; private set; }

        public Task<Result<string>> HandleAsync(ResponseCommand command, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(Result.Succeed("handled"));
        }
    }
}
