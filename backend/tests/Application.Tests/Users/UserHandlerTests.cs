using Application.Abstractions.Authentication;
using Application.Common;
using Application.Tests.TestSupport;
using Application.Users;
using Application.Users.DeleteById;
using Application.Users.Get;
using Application.Users.GetById;
using Application.Users.Login;
using Application.Users.Register;
using Application.Users.UpdateDetailById;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared;

namespace Application.Tests.Users;

public class UserHandlerTests
{
    private readonly TestDatabase database = new();

    [Fact]
    public async Task DeleteById_WithExistingUser_RemovesIt()
    {
        var user = NewUser();
        await database.SeedAsync(user);
        await using var context = database.CreateContext();
        var handler = new DeleteUserByIdCommandHandler(context, NullLogger<DeleteUserByIdCommandHandler>.Instance);

        ResultAssert.Succeeded(await handler.HandleAsync(new DeleteUserByIdCommand(user.Id), default));

        Assert.False(await database.ReadAsync(c => c.Users.AnyAsync()));
    }

    [Fact]
    public async Task DeleteById_WithUnknownId_ReturnsNotFound()
    {
        await using var context = database.CreateContext();
        var handler = new DeleteUserByIdCommandHandler(context, NullLogger<DeleteUserByIdCommandHandler>.Instance);

        var result = await handler.HandleAsync(new DeleteUserByIdCommand(Guid.NewGuid()), default);

        ResultAssert.Failed(result, "Users.NotFound", ErrorType.NotFound);
    }

    [Theory]
    [InlineData("Jan", 1)]
    [InlineData("Doe", 1)]
    [InlineData("jan", 1)]
    [InlineData("DOE", 1)]
    [InlineData("  bOb ", 1)]
    [InlineData("missing", 0)]
    [InlineData(null, 2)]
    public async Task Get_SearchesFirstAndLastName(string? search, int expected)
    {
        await database.SeedAsync(NewUser(), NewUser("other@example.com", "Bob", null));

        var value = await GetAsync(new GetUserQuery(search));

        Assert.Equal(expected, value.TotalCount);
    }

    [Fact]
    public async Task Get_PagesResultsByCreationOrder()
    {
        await database.SeedAsync(
            User.CreateNew("b@example.com", "h", "Second", null, FixedClock.Now.AddDays(1)),
            User.CreateNew("a@example.com", "h", "First", null, FixedClock.Now));

        var value = await GetAsync(new GetUserQuery(null, Page: 1, PageSize: 1));

        Assert.Equal("First", Assert.Single(value.Responses).FirstName);
        Assert.True(value.HasNextPage);
    }

    [Fact]
    public async Task GetById_WithExistingUser_ReturnsIt()
    {
        var user = NewUser();
        await database.SeedAsync(user);
        await using var context = database.CreateContext();
        var handler = new GetUserByIdQueryHandler(context, NullLogger<GetUserByIdQueryHandler>.Instance);

        var response = ResultAssert.Succeeded(await handler.HandleAsync(new GetUserByIdQuery(user.Id), default));

        Assert.Equal(new UserResponse(user.Id, "jane@example.com", "Jane", "Doe"), response);
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        await using var context = database.CreateContext();
        var handler = new GetUserByIdQueryHandler(context, NullLogger<GetUserByIdQueryHandler>.Instance);

        var result = await handler.HandleAsync(new GetUserByIdQuery(Guid.NewGuid()), default);

        ResultAssert.Failed(result, "Users.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsToken()
    {
        var user = NewUser();
        await database.SeedAsync(user);

        var response = ResultAssert.Succeeded(await LoginAsync(new FakePasswordHasher()));

        Assert.Equal(user.Id, response.Id);
        Assert.Equal($"token-for-{user.Id}", response.Jwt);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsIncorrectCredential()
    {
        var result = await LoginAsync(new FakePasswordHasher());

        ResultAssert.Failed(result, "Users.IncorrectCredential", ErrorType.Problem);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsIncorrectCredential()
    {
        await database.SeedAsync(NewUser());
        var hasher = new FakePasswordHasher
        {
            VerifyResult = Error.Problem(IPasswordHasherExtensions.IncorrectPasswordCode, "Wrong password")
        };

        var result = await LoginAsync(hasher);

        ResultAssert.Failed(result, "Users.IncorrectCredential", ErrorType.Problem);
    }

    [Fact]
    public async Task Login_WhenHasherFailsOtherwise_ReturnsApplicationError()
    {
        await database.SeedAsync(NewUser());
        var hasher = new FakePasswordHasher
        {
            VerifyResult = Error.Problem(IPasswordHasherExtensions.InvalidInputCode, "Invalid input")
        };

        var result = await LoginAsync(hasher);

        ResultAssert.Failed(result, $"Users.ApplicationError:{IPasswordHasherExtensions.InvalidInputCode}",
            ErrorType.System);
    }

    [Fact]
    public async Task Register_WithNewEmail_StoresHashedPassword()
    {
        var response = ResultAssert.Succeeded(await RegisterAsync("jane@example.com"));

        Assert.Equal("jane@example.com", response.Email);
        Assert.Equal("Jane", response.FirstName);
        Assert.Equal("Doe", response.LastName);

        var saved = await database.ReadAsync(c => c.Users.SingleAsync());
        Assert.Equal(response.Id, saved.Id);
        Assert.Equal("hashed:Passw0rd!", saved.PasswordHash);
        Assert.Equal(FixedClock.Now, saved.CreatedTime);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsConflict()
    {
        await database.SeedAsync(NewUser());

        var result = await RegisterAsync("jane@example.com");

        ResultAssert.Failed(result, "Users.EmailAlreadyRegistered", ErrorType.Conflict);
    }

    [Fact]
    public async Task UpdateDetailById_WithExistingUser_UpdatesName()
    {
        var user = NewUser();
        await database.SeedAsync(user);
        await using var context = database.CreateContext();
        var handler = new UpdateUserDetailByIdCommandHandler(context,
            NullLogger<UpdateUserDetailByIdCommandHandler>.Instance);

        var response = ResultAssert.Succeeded(await handler.HandleAsync(
            new UpdateUserDetailByIdCommand(user.Id, "Janet", null), default));

        Assert.Equal(new UpdateUserDetailByIdResponse("Janet", null), response);
        var saved = await database.ReadAsync(c => c.Users.SingleAsync());
        Assert.Equal("Janet", saved.Name.First);
        Assert.Null(saved.Name.Last);
    }

    [Fact]
    public async Task UpdateDetailById_WithUnknownId_ReturnsNotFound()
    {
        await using var context = database.CreateContext();
        var handler = new UpdateUserDetailByIdCommandHandler(context,
            NullLogger<UpdateUserDetailByIdCommandHandler>.Instance);

        var result = await handler.HandleAsync(new UpdateUserDetailByIdCommand(Guid.NewGuid(), "Janet", null), default);

        ResultAssert.Failed(result, "Users.NotFound", ErrorType.NotFound);
    }

    private static User NewUser(string email = "jane@example.com", string first = "Jane", string? last = "Doe")
    {
        return User.CreateNew(email, "hash", first, last, FixedClock.Now);
    }

    private async Task<PagedResponse<UserResponse>> GetAsync(GetUserQuery query)
    {
        await using var context = database.CreateContext();
        var handler = new GetUserQueryHandler(context, NullLogger<GetUserByIdQueryHandler>.Instance);
        return ResultAssert.Succeeded(await handler.HandleAsync(query, default));
    }

    private async Task<Result<LoginResponse>> LoginAsync(IPasswordHasher hasher)
    {
        await using var context = database.CreateContext();
        var handler = new LoginUserCommandHandler(hasher, context, new FakeTokenProvider(),
            NullLogger<LoginUserCommandHandler>.Instance);
        return await handler.HandleAsync(new LoginUserCommand("jane@example.com", "Passw0rd!"), default);
    }

    private async Task<Result<RegisterResponse>> RegisterAsync(string email)
    {
        await using var context = database.CreateContext();
        var handler = new RegisterUserCommandHandler(new FakePasswordHasher(), context, new FixedClock(),
            NullLogger<RegisterUserCommandHandler>.Instance);
        return await handler.HandleAsync(new RegisterUserCommand(email, "Passw0rd!", "Jane", "Doe"), default);
    }
}
