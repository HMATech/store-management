using Application.Common;
using Application.Roles;
using Application.Roles.AssignUser;
using Application.Roles.Create;
using Application.Roles.DeleteById;
using Application.Roles.Get;
using Application.Roles.GetById;
using Application.Roles.GetRolesByUser;
using Application.Roles.GetUsersByName;
using Application.Roles.UnassignUser;
using Application.Roles.UpdateById;
using Application.Tests.TestSupport;
using Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared;

namespace Application.Tests.Roles;

public class RoleHandlerTests
{
    private readonly TestDatabase database = new();

    [Fact]
    public async Task AssignUser_ById_CreatesUserRole()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await AssignAsync(new AssignUserCommand(graph.OtherRole.Id, graph.User.Id.ToString()));

        Assert.Equal(new AssignUserResponse(graph.OtherRole.Id, "cashier", graph.User.Id),
            ResultAssert.Succeeded(result));
        var saved = await database.ReadAsync(c => c.UserRoles.SingleAsync(ur => ur.RoleId == graph.OtherRole.Id));
        Assert.Equal(graph.User.Id, saved.UserId);
        Assert.Equal(FixedClock.Now, saved.AssignedTime);
    }

    [Fact]
    public async Task AssignUser_ByEmail_CreatesUserRole()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await AssignAsync(new AssignUserCommand(graph.OtherRole.Id, graph.User.Email));

        ResultAssert.Succeeded(result);
    }

    [Fact]
    public async Task AssignUser_WithUnknownRole_ReturnsNotFound()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await AssignAsync(new AssignUserCommand(999, graph.User.Email));

        ResultAssert.Failed(result, "Roles.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task AssignUser_WithUnknownUserId_ReturnsNotFound()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await AssignAsync(new AssignUserCommand(graph.Role.Id, Guid.NewGuid().ToString()));

        ResultAssert.Failed(result, "Users.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task AssignUser_WithUnknownEmail_ReturnsNotFound()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await AssignAsync(new AssignUserCommand(graph.Role.Id, "missing@example.com"));

        ResultAssert.Failed(result, "Users.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task AssignUser_WhenAlreadyAssigned_ReturnsConflict()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await AssignAsync(new AssignUserCommand(graph.Role.Id, graph.User.Email));

        ResultAssert.Failed(result, "Roles.AlreadyAssigned", ErrorType.Conflict);
    }

    [Fact]
    public async Task Create_WithNewName_PersistsRole()
    {
        await using var context = database.CreateContext();
        var handler = new CreateRoleCommandHandler(context, new FixedClock(),
            NullLogger<CreateRoleCommandHandler>.Instance);

        var response = ResultAssert.Succeeded(
            await handler.HandleAsync(new CreateRoleCommand("Admin", "Administrators"), default));

        var role = await database.ReadAsync(c => c.Roles.SingleAsync());
        Assert.Equal(response.Id, role.Id);
        Assert.Equal("admin", role.Name.Normalized);
        Assert.Equal("Administrators", role.Description);
        Assert.Equal(FixedClock.Now, role.CreatedTime);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("  ADMIN ")]
    public async Task Create_WithSameNormalizedName_ReturnsConflict(string name)
    {
        await database.SeedAsync(Role.CreateNew("Admin", null, FixedClock.Now));
        await using var context = database.CreateContext();
        var handler = new CreateRoleCommandHandler(context, new FixedClock(),
            NullLogger<CreateRoleCommandHandler>.Instance);

        var result = await handler.HandleAsync(new CreateRoleCommand(name, null), default);

        ResultAssert.Failed(result, "Roles.AlreadyExist", ErrorType.Conflict);
    }

    [Fact]
    public async Task DeleteById_WithExistingRole_RemovesIt()
    {
        var role = Role.CreateNew("Admin", null, FixedClock.Now);
        await database.SeedAsync(role);
        await using var context = database.CreateContext();
        var handler = new DeleteRoleByIdCommandHandler(context, NullLogger<DeleteRoleByIdCommandHandler>.Instance);

        ResultAssert.Succeeded(await handler.HandleAsync(new DeleteRoleByIdCommand(role.Id), default));

        Assert.False(await database.ReadAsync(c => c.Roles.AnyAsync()));
    }

    [Fact]
    public async Task DeleteById_WithUnknownId_ReturnsNotFound()
    {
        await using var context = database.CreateContext();
        var handler = new DeleteRoleByIdCommandHandler(context, NullLogger<DeleteRoleByIdCommandHandler>.Instance);

        var result = await handler.HandleAsync(new DeleteRoleByIdCommand(99), default);

        ResultAssert.Failed(result, "Roles.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task Get_FiltersBySearch()
    {
        await SeededGraph.CreateAsync(database);

        var value = await GetAsync(new GetRoleQuery("cash"));

        Assert.Equal("Cashier", Assert.Single(value).Name);
    }

    [Fact]
    public async Task Get_SearchIgnoresCase()
    {
        await SeededGraph.CreateAsync(database);

        var value = await GetAsync(new GetRoleQuery("CASH"));

        Assert.Equal("Cashier", Assert.Single(value).Name);
    }

    [Fact]
    public async Task Get_SearchIgnoresSurroundingSpaces()
    {
        await SeededGraph.CreateAsync(database);

        var value = await GetAsync(new GetRoleQuery("  cash "));

        Assert.Equal("Cashier", Assert.Single(value).Name);
    }

    [Theory]
    [InlineData(SortOrder.ASC, new[] { "Admin", "Cashier" })]
    [InlineData(SortOrder.DESC, new[] { "Cashier", "Admin" })]
    public async Task Get_SortsByName(SortOrder order, string[] expected)
    {
        await SeededGraph.CreateAsync(database);

        var value = await GetAsync(new GetRoleQuery(null, order));

        Assert.Equal(expected, value.Select(r => r.Name));
    }

    [Fact]
    public async Task GetById_WithExistingRole_ReturnsIt()
    {
        var graph = await SeededGraph.CreateAsync(database);
        await using var context = database.CreateContext();
        var handler = new GetRoleByIdQueryHandler(context, NullLogger<GetRoleByIdQueryHandler>.Instance);

        var response = ResultAssert.Succeeded(await handler.HandleAsync(new GetRoleByIdQuery(graph.Role.Id), default));

        Assert.Equal(new RoleResponse(graph.Role.Id, "Admin", "admin", "Administrators", graph.Role.CreatedTime),
            response);
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        await using var context = database.CreateContext();
        var handler = new GetRoleByIdQueryHandler(context, NullLogger<GetRoleByIdQueryHandler>.Instance);

        var result = await handler.HandleAsync(new GetRoleByIdQuery(99), default);

        ResultAssert.Failed(result, "Roles.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task GetRolesByUser_ById_ReturnsAssignedRoles()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var value = ResultAssert.Succeeded(await GetRolesByUserAsync(graph.User.Id.ToString()));

        var role = Assert.Single(value);
        Assert.Equal(graph.Role.Id, role.RoleId);
        Assert.Equal("Admin", role.RoleName);
        Assert.Equal("admin", role.RoleNormalizedName);
    }

    [Fact]
    public async Task GetRolesByUser_ByEmail_ReturnsAssignedRoles()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var value = ResultAssert.Succeeded(await GetRolesByUserAsync(graph.User.Email));

        Assert.Equal(graph.Role.Id, Assert.Single(value).RoleId);
    }

    [Fact]
    public async Task GetRolesByUser_WithUnknownEmail_ReturnsNotFound()
    {
        var result = await GetRolesByUserAsync("missing@example.com");

        ResultAssert.Failed(result, "Users.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task GetRolesByUser_WithUnknownId_ReturnsNotFound()
    {
        var result = await GetRolesByUserAsync(Guid.NewGuid().ToString());

        ResultAssert.Failed(result, "Users.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task GetUsersByName_ReturnsUsersOfRole()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var value = ResultAssert.Succeeded(await GetUsersByNameAsync("admin"));

        Assert.Equal(graph.User.Id, Assert.Single(value).UserId);
    }

    [Fact]
    public async Task GetUsersByName_WithUnknownRole_ReturnsNotFound()
    {
        var result = await GetUsersByNameAsync("missing");

        ResultAssert.Failed(result, "Roles.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task GetUsersByName_IgnoresCase()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var value = ResultAssert.Succeeded(await GetUsersByNameAsync("Admin"));

        Assert.Equal(graph.User.Id, Assert.Single(value).UserId);
    }

    [Fact]
    public async Task UnassignUser_ById_RemovesUserRole()
    {
        var graph = await SeededGraph.CreateAsync(database);

        ResultAssert.Succeeded(await UnassignAsync(new UnassignUserCommand(graph.Role.Id, graph.User.Id.ToString())));

        Assert.False(await database.ReadAsync(c => c.UserRoles.AnyAsync()));
    }

    [Fact]
    public async Task UnassignUser_ByEmail_RemovesUserRole()
    {
        var graph = await SeededGraph.CreateAsync(database);

        ResultAssert.Succeeded(await UnassignAsync(new UnassignUserCommand(graph.Role.Id, graph.User.Email)));

        Assert.False(await database.ReadAsync(c => c.UserRoles.AnyAsync()));
    }

    [Fact]
    public async Task UnassignUser_WithUnknownRole_ReturnsNotFound()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UnassignAsync(new UnassignUserCommand(999, graph.User.Email));

        ResultAssert.Failed(result, "Roles.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task UnassignUser_WithUnknownUserId_ReturnsNotFound()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UnassignAsync(new UnassignUserCommand(graph.Role.Id, Guid.NewGuid().ToString()));

        ResultAssert.Failed(result, "Users.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task UnassignUser_WithUnknownEmail_ReturnsNotFound()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UnassignAsync(new UnassignUserCommand(graph.Role.Id, "missing@example.com"));

        ResultAssert.Failed(result, "Users.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task UnassignUser_WhenNotAssigned_ReturnsProblem()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UnassignAsync(new UnassignUserCommand(graph.OtherRole.Id, graph.User.Email));

        ResultAssert.Failed(result, "Roles.NotAssigned", ErrorType.Problem);
    }

    [Fact]
    public async Task UpdateById_WithNewName_UpdatesRole()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UpdateAsync(new UpdateRoleByIdCommand(graph.Role.Id, "Owner", "New description"));

        Assert.Equal(new UpdateRoleByIdResponse("Owner", "owner", "New description"), ResultAssert.Succeeded(result));
    }

    [Fact]
    public async Task UpdateById_WithUnknownId_ReturnsNotFound()
    {
        var result = await UpdateAsync(new UpdateRoleByIdCommand(99, "Owner", null));

        ResultAssert.Failed(result, "Roles.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateById_WithNameOfAnotherRole_ReturnsConflict()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UpdateAsync(new UpdateRoleByIdCommand(graph.Role.Id, "CASHIER", null));

        ResultAssert.Failed(result, "Roles.AlreadyExist", ErrorType.Conflict);
    }

    [Fact]
    public async Task UpdateById_KeepingNameButChangingDescription_Succeeds()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UpdateAsync(new UpdateRoleByIdCommand(graph.Role.Id, "Admin", "New description"));

        Assert.Equal("New description", ResultAssert.Succeeded(result).Description);
        var saved = await database.ReadAsync(c => c.Roles.SingleAsync(r => r.Id == graph.Role.Id));
        Assert.Equal("New description", saved.Description);
    }

    private async Task<Result<AssignUserResponse>> AssignAsync(AssignUserCommand command)
    {
        await using var context = database.CreateContext();
        var handler = new AssignUserCommandHandler(context, new FixedClock(),
            NullLogger<AssignUserCommandHandler>.Instance);
        return await handler.HandleAsync(command, default);
    }

    private async Task<List<RoleResponse>> GetAsync(GetRoleQuery query)
    {
        await using var context = database.CreateContext();
        var handler = new GetRoleQueryHandler(context, NullLogger<GetRoleQuery>.Instance);
        return ResultAssert.Succeeded(await handler.HandleAsync(query, default));
    }

    private async Task<Result<List<UserRoleResponse>>> GetRolesByUserAsync(string user)
    {
        await using var context = database.CreateContext();
        var handler = new GetRolesByUserQueryHandler(context, NullLogger<GetRolesByUserQueryHandler>.Instance);
        return await handler.HandleAsync(new GetRolesByUserQuery(user), default);
    }

    private async Task<Result<List<Application.Users.UserResponse>>> GetUsersByNameAsync(string name)
    {
        await using var context = database.CreateContext();
        var handler = new GetUsersByNameQueryHandler(context, NullLogger<GetUsersByNameQueryHandler>.Instance);
        return await handler.HandleAsync(new GetUsersByNameQuery(name), default);
    }

    private async Task<Result> UnassignAsync(UnassignUserCommand command)
    {
        await using var context = database.CreateContext();
        var handler = new UnassignUserCommandHandler(context, NullLogger<UnassignUserCommandHandler>.Instance);
        return await handler.HandleAsync(command, default);
    }

    private async Task<Result<UpdateRoleByIdResponse>> UpdateAsync(UpdateRoleByIdCommand command)
    {
        await using var context = database.CreateContext();
        var handler = new UpdateRoleByIdCommandHandler(context, NullLogger<UpdateRoleByIdCommandHandler>.Instance);
        return await handler.HandleAsync(command, default);
    }
}
