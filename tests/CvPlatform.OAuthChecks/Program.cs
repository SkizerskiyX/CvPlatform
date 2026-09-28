using System.Reflection;
using System.Security.Claims;
using CvPlatform.API.Controllers;
using CvPlatform.API.Security;
using CvPlatform.Application.Services;
using CvPlatform.Application.Common;
using CvPlatform.Application.DTOs;
using CvPlatform.Application.Security;
using CvPlatform.Domain.Enums;
using CvPlatform.Domain.Entities;
using CvPlatform.Infrastructure.Identity;
using CvPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

await Check("missing external cookie", f => f.SignIn.Info = null, "external");
await Check("missing provider email", f => f.SignIn.Info = Info(null), "missing_email");
await Check("email collision never signs into existing account", f => f.Users.EmailUser = new(), "account_exists");
await Check("returning identity works after provider email changes", f =>
{
    f.Users.LinkedUser = new() { Email = "original@example.com" };
    f.Users.EmailLookupForbidden = true;
    f.SignIn.Info = Info("changed@example.com");
}, null);
await Check("returning identity does not require provider email again", f =>
{
    f.Users.LinkedUser = new() { Email = "original@example.com" };
    f.SignIn.Info = Info(null);
}, null);
await Check("locked identity gets no token", f =>
{
    f.Users.LinkedUser = new();
    f.Users.Locked = true;
}, "locked");
await Check("new identity is created as Candidate", _ => { }, null);
await Check("creation failure gets no token", f => f.Users.FailAt = "create", "external");
await Check("role failure rolls back", f => f.Users.FailAt = "role", "external");
await Check("external link failure rolls back", f => f.Users.FailAt = "link", "external");
foreach (var recruiter in new[] { false, true })
{
    using var f = new Fixture(Info("new@example.com"));
    var result = await f.Controller.Register(new("new@example.com", "Password1!", recruiter), default);
    Assert(result.Result is OkObjectResult, "registration succeeds");
    var role = recruiter ? "Recruiter" : "Candidate";
    Assert(f.Users.AssignedRole == role && f.Tokens.Roles.SequenceEqual([role]), "registration role matches token");
    Console.WriteLine("PASS registration as " + role);
}
using (var f = new Fixture(Info("new@example.com")))
{
    f.SignIn.Enabled = false;
    var result = (RedirectResult)await f.Controller.ExternalLoginCallback(default);
    Assert(result.Url!.EndsWith("error=unavailable") && !f.ClearedCookie && !f.Tokens.Issued, "disabled OAuth callback");
    var providers = (OkObjectResult)(await f.Controller.ExternalProviders()).Result!;
    Assert(((string[])providers.Value!).Length == 0, "disabled provider list");
    var login = (RedirectResult)await f.Controller.ExternalLogin("Google");
    Assert(login.Url!.EndsWith("error=unavailable"), "disabled OAuth challenge");
    Console.WriteLine("PASS disabled OAuth routes");
}
foreach (var state in new[] { "deleted", "blocked", "changed-roles" })
{
    var profileId = Guid.NewGuid();
    var directory = Stub.Create<IUserDirectory>((_, _) => Task.FromResult<UserSnapshot?>(state == "deleted" ? null : new("u1", "user@example.com", profileId, state == "blocked", ["Candidate"], null, null)));
    using var services = new ServiceCollection().AddSingleton(directory).BuildServiceProvider();
    var context = new TokenValidatedContext(new DefaultHttpContext { RequestServices = services }, new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler)), new JwtBearerOptions())
    {
        Principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "u1"), new Claim(ClaimTypes.Role, "Admin"), new Claim("profileId", Guid.NewGuid().ToString())], "Bearer"))
    };
    await CurrentUserValidation.Validate(context);
    if (state == "changed-roles") Assert(!context.Principal.IsInRole("Admin") && context.Principal.IsInRole("Candidate") && context.Principal.FindFirstValue("profileId") == profileId.ToString(), "stale JWT roles replaced");
    else Assert(context.Result?.Failure is not null, "invalid account rejected");
    Console.WriteLine("PASS JWT " + state);
}
Console.WriteLine("16 authentication checks passed. No database or provider requests made.");

var numeric = new AttributeDefinition("Score", null, AttributeDataType.Numeric, Guid.NewGuid());
Assert(AccessRuleEvaluator.Matches(numeric, AttributeValueInput.Empty with { NumericValue = 8 }, ComparisonOperator.GreaterThan, "7"), "numeric access rule");
Assert(!AccessRuleEvaluator.Matches(numeric, AttributeValueInput.Empty with { NumericValue = 7 }, ComparisonOperator.GreaterThan, "7"), "numeric boundary");
Assert(!AccessRuleEvaluator.Matches(numeric, AttributeValueInput.Empty, ComparisonOperator.GreaterThan, "7"), "missing access value");
var boolean = new AttributeDefinition("Remote", null, AttributeDataType.Boolean, Guid.NewGuid());
Assert(AccessRuleEvaluator.Matches(boolean, AttributeValueInput.Empty with { BoolValue = false }, ComparisonOperator.EqualTo, "false"), "false is a filled boolean");
var dropdown = new AttributeDefinition("English", null, AttributeDataType.Dropdown, Guid.NewGuid());
dropdown.SetOptions(["Advanced", "Beginner"]);
var optionId = dropdown.Options.First().Id;
dropdown.SetOptions(["Advanced", "Intermediate"]);
Assert(dropdown.Options.First(x => x.Value == "Advanced").Id == optionId, "existing dropdown IDs survive editing");
Assert(AccessRuleEvaluator.Matches(dropdown, AttributeValueInput.Empty with { SelectedOptionId = optionId }, ComparisonOperator.In, optionId.ToString()), "dropdown access rule");
var template = new Position("Engineer", null, true);
template.UpdateBasics("Engineer", null, null, null, true, 1, ["react"]);
var owner = Guid.NewGuid();
var projects = new[] { new Project(owner, "Relevant", DateTime.UtcNow, null, null, ["react"]), new Project(owner, "Unrelated", DateTime.UtcNow.AddDays(1), null, null, ["java"]) };
Assert(CvComposer.SelectProjects(template, projects).Single().Name == "Relevant", "CV project tag filtering and limit");
Console.WriteLine("7 domain requirement checks passed.");

static ExternalLoginInfo Info(string? email)
{
    var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "provider-user-123") };
    if (email is not null) claims.Add(new(ClaimTypes.Email, email));
    return new(new ClaimsPrincipal(new ClaimsIdentity(claims, "Google")), "Google", "provider-user-123", "Google");
}

static async Task Check(string name, Action<Fixture> arrange, string? error)
{
    using var f = new Fixture(Info("new@example.com"));
    arrange(f);
    var result = (RedirectResult)await f.Controller.ExternalLoginCallback(default);
    var expected = "https://cvplatform-h1rj.onrender.com" +
        (error is null ? "/oauth-callback#token=test-token" : "/login?error=" + error);
    Assert(result.Url == expected, name + ": redirect");
    Assert(f.Tokens.Issued == (error is null), name + ": token issuance");
    Assert(f.ClearedCookie, name + ": external cookie cleared");
    Assert(f.Controller.Response.Headers.CacheControl == "no-store", name + ": response not cached");
    if (f.Users.AssignedRole is not null) Assert(f.Users.AssignedRole == "Candidate", name + ": role");
    if (f.Users.Created is not null) Assert(!f.Users.Created.EmailConfirmed, name + ": no fabricated email verification");
    var manager = (TransactionManager)f.Db.GetService<IDbContextTransactionManager>();
    if (manager.Transaction is not null)
    {
        Assert(manager.Transaction.Committed == (error is null), name + ": transaction commit");
        Assert(manager.Transaction.Disposed, name + ": transaction disposed");
    }
    Console.WriteLine("PASS " + name);
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

sealed class Fixture : IDisposable
{
    public FakeUsers Users { get; } = new();
    public FakeSignIn SignIn { get; }
    public FakeTokens Tokens { get; } = new();
    public AppDbContext Db { get; }
    public AccountController Controller { get; }
    public bool ClearedCookie { get; private set; }
    private readonly ServiceProvider services;

    public Fixture(ExternalLoginInfo info)
    {
        SignIn = new(Users) { Info = info };
        Db = new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=unused;Database=unused;Username=unused")
            .ReplaceService<IDbContextTransactionManager, TransactionManager>().Options);
        var auth = Stub.Create<IAuthenticationService>((method, args) =>
        {
            if (method.Name != "SignOutAsync" || (string?)args![1] != IdentityConstants.ExternalScheme)
                throw new Exception("Unexpected authentication call");
            ClearedCookie = true;
            return Task.CompletedTask;
        });
        services = new ServiceCollection().AddSingleton(auth).BuildServiceProvider();
        var profiles = Stub.Create<IProfileService>((method, args) =>
            method.Name == "EnsureProfileAsync"
                ? Task.FromResult(new UserProfile((string)args![0]!, "Test", "User", null))
                : throw new Exception("Unexpected profile call"));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Frontend:BaseUrl"] = "https://cvplatform-h1rj.onrender.com"
        }).Build();
        Controller = new(Users, SignIn, profiles, Tokens, config, Db)
        {
            ControllerContext = new() { HttpContext = new DefaultHttpContext { RequestServices = services } }
        };
    }
    public void Dispose() { Db.Dispose(); Users.Dispose(); services.Dispose(); }
}

sealed class FakeUsers : UserManager<ApplicationUser>
{
    public ApplicationUser? LinkedUser, EmailUser, Created;
    public bool Locked, EmailLookupForbidden;
    public string? FailAt, AssignedRole;
    public FakeUsers() : base(Stub.Create<IUserStore<ApplicationUser>>((_, _) => null),
        Microsoft.Extensions.Options.Options.Create(new IdentityOptions()), new PasswordHasher<ApplicationUser>(), [], [],
        new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(),
        new ServiceCollection().BuildServiceProvider(), NullLogger<UserManager<ApplicationUser>>.Instance) { }
    public override bool SupportsUserLockout => true;
    public override Task<ApplicationUser?> FindByLoginAsync(string provider, string key) =>
        provider == "Google" && key == "provider-user-123" ? Task.FromResult(LinkedUser) : throw new Exception("Incorrect identity lookup");
    public override Task<ApplicationUser?> FindByEmailAsync(string email) =>
        EmailLookupForbidden ? throw new Exception("Returning login must not use email") : Task.FromResult(EmailUser);
    public override Task<bool> IsLockedOutAsync(ApplicationUser user) => Task.FromResult(Locked);
    public override Task<IdentityResult> CreateAsync(ApplicationUser user) { Created = user; return Result("create"); }
    public override Task<IdentityResult> CreateAsync(ApplicationUser user, string password) => CreateAsync(user);
    public override Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role) { AssignedRole = role; return Result("role"); }
    public override Task<IdentityResult> AddLoginAsync(ApplicationUser user, UserLoginInfo login) => Result("link");
    public override Task<IList<string>> GetRolesAsync(ApplicationUser user) => Task.FromResult<IList<string>>(["Candidate"]);
    private Task<IdentityResult> Result(string step) => Task.FromResult(FailAt == step
        ? IdentityResult.Failed(new IdentityError { Description = "Simulated failure" }) : IdentityResult.Success);
}

sealed class FakeSignIn(FakeUsers users) : SignInManager<ApplicationUser>(users,
    new HttpContextAccessor(), new UserClaimsPrincipalFactory<ApplicationUser>(users, Microsoft.Extensions.Options.Options.Create(new IdentityOptions())),
    Microsoft.Extensions.Options.Options.Create(new IdentityOptions()), NullLogger<SignInManager<ApplicationUser>>.Instance,
    new AuthenticationSchemeProvider(Microsoft.Extensions.Options.Options.Create(new AuthenticationOptions())), new DefaultUserConfirmation<ApplicationUser>())
{
    public ExternalLoginInfo? Info;
    public bool Enabled = true;
    public override Task<IEnumerable<AuthenticationScheme>> GetExternalAuthenticationSchemesAsync() =>
        Task.FromResult<IEnumerable<AuthenticationScheme>>(Enabled
            ? [new AuthenticationScheme("Google", "Google", typeof(Microsoft.AspNetCore.Authentication.Google.GoogleHandler))] : []);
    public override Task<ExternalLoginInfo?> GetExternalLoginInfoAsync(string? expectedXsrf = null) => Task.FromResult(Info);
    public override Task<bool> CanSignInAsync(ApplicationUser user) => Task.FromResult(true);
}

sealed class FakeTokens : ITokenService
{
    public bool Issued;
    public string[] Roles = [];
    public string CreateToken(string id, string email, Guid profileId, IEnumerable<string> roles)
    { Issued = true; Roles = roles.ToArray(); return "test-token"; }
}

public class Stub : DispatchProxy
{
    private Func<MethodInfo, object?[]?, object?> call = null!;
    public static T Create<T>(Func<MethodInfo, object?[]?, object?> call) where T : class
    {
        var proxy = Create<T, Stub>();
        ((Stub)(object)proxy).call = call;
        return proxy;
    }
    protected override object? Invoke(MethodInfo? method, object?[]? args) => call(method!, args);
}

public sealed class TransactionManager : IDbContextTransactionManager
{
    public FakeTransaction? Transaction;
    public IDbContextTransaction? CurrentTransaction => Transaction;
    public IDbContextTransaction BeginTransaction() => Transaction = new();
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.FromResult(BeginTransaction());
    public void CommitTransaction() => Transaction!.Commit();
    public Task CommitTransactionAsync(CancellationToken cancellationToken = default) { CommitTransaction(); return Task.CompletedTask; }
    public void RollbackTransaction() => Transaction!.Rollback();
    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) { RollbackTransaction(); return Task.CompletedTask; }
    public void ResetState() => Transaction = null;
    public Task ResetStateAsync(CancellationToken cancellationToken = default) { ResetState(); return Task.CompletedTask; }
}

public sealed class FakeTransaction : IDbContextTransaction
{
    public Guid TransactionId { get; } = Guid.NewGuid();
    public bool Committed, Disposed;
    public void Commit() => Committed = true;
    public Task CommitAsync(CancellationToken cancellationToken = default) { Commit(); return Task.CompletedTask; }
    public void Rollback() => Committed = false;
    public Task RollbackAsync(CancellationToken cancellationToken = default) { Rollback(); return Task.CompletedTask; }
    public void Dispose() => Disposed = true;
    public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
}
