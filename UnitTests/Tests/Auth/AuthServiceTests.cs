using Education.Domain.DTO.Request.Auth;
using Education.Service.Auth.Implementations;
using Education.Service.Auth.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests.Tests.Auth;

public class AuthServiceTests
{
    private readonly Mock<UserManager<UserEntity>> _users = MockUserManager.Create();
    private readonly Mock<ISignInService> _signIn = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<ILinkGenerator> _links = new();
    private readonly Mock<IEmailTemplateBuilder> _templates = new();
    private readonly UserEntity _user = UserFactory.Create();
    private AuthService Sut => new(_users.Object, _signIn.Object, _email.Object, NullLogger<AuthService>.Instance, _links.Object, _templates.Object);
    private static RegisterRequest Registration => new() { Email = "new@test.com", Login = "newuser", FirstName = "First", LastName = "Last", Password = "Password1!", ConfirmPassword = "Password1!" };
    private static IdentityResult Error => IdentityResult.Failed(new IdentityError { Description = "Identity failure" });

    public AuthServiceTests()
    {
        _users.Setup(u => u.FindByIdAsync(_user.Id.ToString())).ReturnsAsync(_user);
        _users.Setup(u => u.FindByEmailAsync(_user.Email!)).ReturnsAsync(_user);
        _users.Setup(u => u.FindByNameAsync(_user.UserName!)).ReturnsAsync(_user);
        _users.Setup(u => u.GetRolesAsync(It.IsAny<UserEntity>())).ReturnsAsync(new[] { "user" });
        _users.Setup(u => u.IsEmailConfirmedAsync(_user)).ReturnsAsync(true);
        _users.Setup(u => u.GenerateEmailConfirmationTokenAsync(It.IsAny<UserEntity>())).ReturnsAsync("confirm-token");
        _users.Setup(u => u.GeneratePasswordResetTokenAsync(_user)).ReturnsAsync("reset-token");
        _links.Setup(l => l.GetConfirmEmailLink(It.IsAny<Guid>(), "confirm-token")).Returns("confirm-link");
        _links.Setup(l => l.GetResetPasswordLink(_user.Id, "reset-token")).Returns("reset-link");
        _templates.Setup(t => t.BuildConfirmEmail(It.IsAny<string>(), "confirm-link")).Returns("confirm-body");
        _templates.Setup(t => t.BuildResetPasswordEmail(_user.FirstName, "reset-link")).Returns("reset-body");
        _users.Setup(u => u.CreateAsync(It.IsAny<UserEntity>(), Registration.Password)).ReturnsAsync(IdentityResult.Success);
        _users.Setup(u => u.AddToRoleAsync(It.IsAny<UserEntity>(), "user")).ReturnsAsync(IdentityResult.Success);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Register_DuplicateIdentity_DoesNotCreate(bool duplicateEmail)
    {
        if (duplicateEmail) _users.Setup(u => u.FindByEmailAsync(Registration.Email)).ReturnsAsync(_user);
        else _users.Setup(u => u.FindByNameAsync(Registration.Login)).ReturnsAsync(_user);
        (await Sut.RegisterAsync(Registration)).StatusCode.Should().Be(StatusCode.Conflict);
        _users.Verify(u => u.CreateAsync(It.IsAny<UserEntity>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Register_MapsProfileAssignsDefaultRoleAndSendsGeneratedEmail()
    {
        (await Sut.RegisterAsync(Registration)).StatusCode.Should().Be(StatusCode.OK);
        _users.Verify(u => u.CreateAsync(It.Is<UserEntity>(x => x.Email == Registration.Email && x.UserName == Registration.Login
            && x.FirstName == Registration.FirstName && x.LastName == Registration.LastName), Registration.Password), Times.Once);
        _users.Verify(u => u.AddToRoleAsync(It.IsAny<UserEntity>(), "user"), Times.Once);
        _email.Verify(e => e.SendEmailAsync(Registration.Email, "Подтверждение регистрации", "confirm-body"), Times.Once);
    }

    [Fact]
    public async Task Register_CreateFailure_DoesNotAssignRoleOrSend()
    {
        _users.Setup(u => u.CreateAsync(It.IsAny<UserEntity>(), Registration.Password)).ReturnsAsync(Error);
        var result = await Sut.RegisterAsync(Registration);
        result.StatusCode.Should().Be(StatusCode.BadRequest);
        result.Description.Should().Contain("Identity failure");
        _users.Verify(u => u.AddToRoleAsync(It.IsAny<UserEntity>(), It.IsAny<string>()), Times.Never);
        _email.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Register_RoleFailure_CompensatesCreatedUser()
    {
        _users.Setup(u => u.AddToRoleAsync(It.IsAny<UserEntity>(), "user")).ReturnsAsync(Error);
        (await Sut.RegisterAsync(Registration)).StatusCode.Should().Be(StatusCode.InternalServerError);
        _users.Verify(u => u.DeleteAsync(It.IsAny<UserEntity>()), Times.Once);
        _email.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Register_SmtpFailure_PreservesAccountForResend()
    {
        _email.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new IOException("SMTP"));
        (await Sut.RegisterAsync(Registration)).IsSuccess.Should().BeTrue();
        _users.Verify(u => u.DeleteAsync(It.IsAny<UserEntity>()), Times.Never);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task Login_ResolvesIdentityAndForwardsRememberMe(bool byEmail, bool remember)
    {
        _users.Setup(u => u.CheckPasswordAsync(_user, "password")).ReturnsAsync(true);
        var response = await Sut.LoginAsync(new LoginRequest { LoginOrEmail = byEmail ? _user.Email! : _user.UserName!, Password = "password", RememberMe = remember });
        response.StatusCode.Should().Be(StatusCode.OK);
        response.Data!.UserId.Should().Be(_user.Id);
        response.Data.Login.Should().Be(_user.UserName);
        response.Data.Role.Should().Be(UserRole.User);
        _signIn.Verify(s => s.SignInAsync(_user, remember), Times.Once);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("missing@test.com")]
    public async Task Login_UnknownIdentity_DoesNotSignIn(string identity)
    {
        (await Sut.LoginAsync(new LoginRequest { LoginOrEmail = identity, Password = "password" })).StatusCode.Should().Be(StatusCode.Unauthorized);
        _signIn.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false, true, StatusCode.Unauthorized)]
    [InlineData(true, false, StatusCode.Forbidden)]
    public async Task Login_RejectsWrongPasswordAndUnconfirmedEmail(bool validPassword, bool confirmed, StatusCode status)
    {
        _user.EmailConfirmed = confirmed;
        _users.Setup(u => u.CheckPasswordAsync(_user, "password")).ReturnsAsync(validPassword);
        (await Sut.LoginAsync(new LoginRequest { LoginOrEmail = _user.UserName!, Password = "password" })).StatusCode.Should().Be(status);
        _signIn.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ConfirmEmail_ValidToken_ReturnsProfileWithoutSigningIn()
    {
        _user.EmailConfirmed = false;
        _users.Setup(u => u.ConfirmEmailAsync(_user, "token")).ReturnsAsync(IdentityResult.Success);
        var response = await Sut.ConfirmEmailAsync(new ConfirmEmailRequest { UserId = _user.Id, Token = "token" });
        response.StatusCode.Should().Be(StatusCode.OK);
        response.Data!.UserId.Should().Be(_user.Id);
        _signIn.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false, false, StatusCode.NotFound)]
    [InlineData(true, true, StatusCode.Conflict)]
    [InlineData(true, false, StatusCode.BadRequest)]
    public async Task ConfirmEmail_RejectsMissingConfirmedOrInvalidToken(bool exists, bool confirmed, StatusCode status)
    {
        _user.EmailConfirmed = confirmed;
        _users.Setup(u => u.FindByIdAsync(_user.Id.ToString())).ReturnsAsync(exists ? _user : null);
        _users.Setup(u => u.ConfirmEmailAsync(_user, "bad")).ReturnsAsync(Error);
        (await Sut.ConfirmEmailAsync(new ConfirmEmailRequest { UserId = _user.Id, Token = "bad" })).StatusCode.Should().Be(status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ForgotPassword_SendsGeneratedResetEmailOrReportsSmtpFailure(bool smtpFails)
    {
        if (smtpFails) _email.Setup(e => e.SendEmailAsync(_user.Email!, "Сброс пароля", "reset-body")).ThrowsAsync(new IOException());
        (await Sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = _user.Email! })).StatusCode
            .Should().Be(smtpFails ? StatusCode.InternalServerError : StatusCode.OK);
        _email.Verify(e => e.SendEmailAsync(_user.Email!, "Сброс пароля", "reset-body"), Times.Once);
    }

    [Theory]
    [InlineData(false, false, StatusCode.NotFound)]
    [InlineData(true, false, StatusCode.Conflict)]
    public async Task ForgotPassword_RejectsUnavailableAccount(bool exists, bool confirmed, StatusCode status)
    {
        _users.Setup(u => u.FindByEmailAsync(_user.Email!)).ReturnsAsync(exists ? _user : null);
        _users.Setup(u => u.IsEmailConfirmedAsync(_user)).ReturnsAsync(confirmed);
        (await Sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = _user.Email! })).StatusCode.Should().Be(status);
        _email.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResetPassword_PassesTokenAndNewPasswordToIdentity(bool success)
    {
        _users.Setup(u => u.ResetPasswordAsync(_user, "token", "new-password")).ReturnsAsync(success ? IdentityResult.Success : Error);
        (await Sut.ResetPasswordAsync(new ResetPasswordRequest { UserId = _user.Id, Token = "token", NewPassword = "new-password" })).StatusCode
            .Should().Be(success ? StatusCode.OK : StatusCode.BadRequest);
        _users.Verify(u => u.ResetPasswordAsync(_user, "token", "new-password"), Times.Once);
    }

    [Theory]
    [InlineData(false, StatusCode.NotFound)]
    [InlineData(true, StatusCode.Conflict)]
    public async Task ResetPassword_RequiresExistingConfirmedAccount(bool exists, StatusCode status)
    {
        _users.Setup(u => u.FindByIdAsync(_user.Id.ToString())).ReturnsAsync(exists ? _user : null);
        _users.Setup(u => u.IsEmailConfirmedAsync(_user)).ReturnsAsync(false);
        (await Sut.ResetPasswordAsync(new ResetPasswordRequest { UserId = _user.Id, Token = "token", NewPassword = "new" })).StatusCode.Should().Be(status);
        _users.Verify(u => u.ResetPasswordAsync(It.IsAny<UserEntity>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResendConfirmation_SendsGeneratedLetterOrReportsSmtpFailure(bool smtpFails)
    {
        _user.EmailConfirmed = false;
        if (smtpFails) _email.Setup(e => e.SendEmailAsync(_user.Email!, "Подтверждение регистрации", "confirm-body")).ThrowsAsync(new IOException());
        (await Sut.ResendConfirmationAsync(new ResendConfirmationRequest { Email = _user.Email! })).StatusCode
            .Should().Be(smtpFails ? StatusCode.InternalServerError : StatusCode.OK);
        _email.Verify(e => e.SendEmailAsync(_user.Email!, "Подтверждение регистрации", "confirm-body"), Times.Once);
    }

    [Theory]
    [InlineData(false, StatusCode.NotFound)]
    [InlineData(true, StatusCode.Conflict)]
    public async Task ResendConfirmation_RejectsMissingOrConfirmedAccount(bool exists, StatusCode status)
    {
        _users.Setup(u => u.FindByEmailAsync(_user.Email!)).ReturnsAsync(exists ? _user : null);
        (await Sut.ResendConfirmationAsync(new ResendConfirmationRequest { Email = _user.Email! })).StatusCode.Should().Be(status);
        _email.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Logout_SignsOutOnce()
    {
        (await Sut.LogoutAsync()).StatusCode.Should().Be(StatusCode.OK);
        _signIn.Verify(s => s.SignOutAsync(), Times.Once);
    }
}
