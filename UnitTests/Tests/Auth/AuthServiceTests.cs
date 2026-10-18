using Education.Domain.DTO.Request.Auth;
using Education.Domain.Entity;
using Education.Domain.Enums;
using Education.Service.Auth.Implementations;
using Education.Service.Auth.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Education.Domain.Extensions;
using UnitTests.Helpers;

namespace UnitTests.Tests.Auth
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<UserEntity>> _userManagerMock;
        private readonly Mock<ISignInService> _signInServiceMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<ILinkGenerator> _linkGeneratorMock;
        private readonly Mock<IEmailTemplateBuilder> _emailTemplateBuilderMock;
        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            _userManagerMock = MockUserManager.Create();
            _signInServiceMock = new Mock<ISignInService>();
            _emailServiceMock = new Mock<IEmailService>();
            _linkGeneratorMock = new Mock<ILinkGenerator>();
            _emailTemplateBuilderMock = new Mock<IEmailTemplateBuilder>();

            _sut = new AuthService(
                _userManagerMock.Object,
                _signInServiceMock.Object,
                _emailServiceMock.Object,
                Mock.Of<ILogger<AuthService>>(),
                _linkGeneratorMock.Object,
                _emailTemplateBuilderMock.Object);
        }

        #region RegisterAsync

        [Fact]
        public async Task RegisterAsync_LoginExists_ReturnsConflict()
        {
            var request = new RegisterRequest { Email = "e@test.com", Login = "login", Password = "pass", ConfirmPassword = "pass" };

            _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync((UserEntity?)null);
            _userManagerMock.Setup(m => m.FindByNameAsync(request.Login)).ReturnsAsync(new UserEntity { UserName = request.Login });

            var result = await _sut.RegisterAsync(request);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.Conflict);
        }

        [Fact]
        public async Task RegisterAsync_CreateFails_ReturnsBadRequest()
        {
            var request = new RegisterRequest { Email = "e@test.com", Login = "login", Password = "pass", ConfirmPassword = "pass" };

            _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync((UserEntity?)null);
            _userManagerMock.Setup(m => m.FindByNameAsync(request.Login)).ReturnsAsync((UserEntity?)null);
            _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<UserEntity>(), request.Password))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "err" }));

            var result = await _sut.RegisterAsync(request);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.BadRequest);
        }

        [Fact]
        public async Task RegisterAsync_Success_ReturnsOk()
        {
            var request = new RegisterRequest { Email = "e@test.com", Login = "login", FirstName = "F", LastName = "L", Password = "passpass", ConfirmPassword = "passpass" };

            _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync((UserEntity?)null);
            _userManagerMock.Setup(m => m.FindByNameAsync(request.Login)).ReturnsAsync((UserEntity?)null);
            _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<UserEntity>(), request.Password)).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<UserEntity>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<UserEntity>())).ReturnsAsync("token");
            _emailTemplateBuilderMock.Setup(b => b.BuildConfirmEmail(It.IsAny<string>(), It.IsAny<string>())).Returns("body");
            _emailServiceMock.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var result = await _sut.RegisterAsync(request);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(StatusCode.OK);
            result.Data.Should().BeTrue();
        }

        #endregion

        #region LoginAsync

        [Fact]
        public async Task LoginAsync_UserNotFound_ReturnsUnauthorized()
        {
            var request = new LoginRequest { LoginOrEmail = "noone", Password = "p" };

            _userManagerMock.Setup(m => m.FindByNameAsync(request.LoginOrEmail)).ReturnsAsync((UserEntity?)null);

            var result = await _sut.LoginAsync(request);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.Unauthorized);
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ReturnsUnauthorized()
        {
            var request = new LoginRequest { LoginOrEmail = "user", Password = "p" };
            var user = UserFactory.Create(userName: "user");

            _userManagerMock.Setup(m => m.FindByNameAsync(request.LoginOrEmail)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.CheckPasswordAsync(user, request.Password)).ReturnsAsync(false);

            var result = await _sut.LoginAsync(request);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.Unauthorized);
        }

        [Fact]
        public async Task LoginAsync_EmailNotConfirmed_ReturnsForbidden()
        {
            var request = new LoginRequest { LoginOrEmail = "user", Password = "p" };
            var user = UserFactory.Create(userName: "user", emailConfirmed: false);

            _userManagerMock.Setup(m => m.FindByNameAsync(request.LoginOrEmail)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.CheckPasswordAsync(user, request.Password)).ReturnsAsync(true);

            var result = await _sut.LoginAsync(request);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.Forbidden);
        }

        [Fact]
        public async Task LoginAsync_Success_ReturnsOkWithData()
        {
            var request = new LoginRequest { LoginOrEmail = "user@test.com", Password = "p", RememberMe = true };
            var user = UserFactory.Create(email: request.LoginOrEmail, userName: "user");

            _userManagerMock.Setup(m => m.FindByEmailAsync(request.LoginOrEmail)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.CheckPasswordAsync(user, request.Password)).ReturnsAsync(true);
            _signInServiceMock.Setup(s => s.SignInAsync(user, true)).Returns(Task.CompletedTask);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { UserRole.User.RoleToDbString() });

            var result = await _sut.LoginAsync(request);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(StatusCode.OK);
            result.Data!.Email.Should().Be(request.LoginOrEmail);
            result.Data.Login.Should().Be(user.UserName);
            result.Data.Role.Should().Be(UserRole.User);
        }

        #endregion

        #region ConfirmEmailAsync

        [Fact]
        public async Task ConfirmEmailAsync_UserNotFound_ReturnsNotFound()
        {
            var req = new ConfirmEmailRequest { UserId = Guid.NewGuid(), Token = "t" };

            _userManagerMock.Setup(m => m.FindByIdAsync(req.UserId.ToString())).ReturnsAsync((UserEntity?)null);

            var result = await _sut.ConfirmEmailAsync(req);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.NotFound);
        }

        [Fact]
        public async Task ConfirmEmailAsync_AlreadyConfirmed_ReturnsConflict()
        {
            var req = new ConfirmEmailRequest { UserId = Guid.NewGuid(), Token = "t" };
            var user = UserFactory.Create();
            user.EmailConfirmed = true;

            _userManagerMock.Setup(m => m.FindByIdAsync(req.UserId.ToString())).ReturnsAsync(user);

            var result = await _sut.ConfirmEmailAsync(req);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.Conflict);
        }

        [Fact]
        public async Task ConfirmEmailAsync_ConfirmFails_ReturnsConflict()
        {
            var req = new ConfirmEmailRequest { UserId = Guid.NewGuid(), Token = "t" };
            var user = UserFactory.Create();

            _userManagerMock.Setup(m => m.FindByIdAsync(req.UserId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.ConfirmEmailAsync(user, req.Token)).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "err" }));

            var result = await _sut.ConfirmEmailAsync(req);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.Conflict);
        }

        [Fact]
        public async Task ConfirmEmailAsync_Success_ReturnsOkWithLoginResponse()
        {
            var req = new ConfirmEmailRequest { UserId = Guid.NewGuid(), Token = "t" };
            var user = UserFactory.Create(emailConfirmed: false);

            _userManagerMock.Setup(m => m.FindByIdAsync(req.UserId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.ConfirmEmailAsync(user, req.Token)).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { UserRole.User.RoleToDbString() });

            var result = await _sut.ConfirmEmailAsync(req);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(StatusCode.OK);
            result.Data!.Email.Should().Be(user.Email);
        }

        #endregion

        #region ForgotPasswordAsync

        [Fact]
        public async Task ForgotPasswordAsync_UserNotFound_ReturnsNotFound()
        {
            var req = new ForgotPasswordRequest { Email = "no@test.com" };

            _userManagerMock.Setup(m => m.FindByEmailAsync(req.Email)).ReturnsAsync((UserEntity?)null);

            var result = await _sut.ForgotPasswordAsync(req);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.NotFound);
        }

        [Fact]
        public async Task ForgotPasswordAsync_EmailNotConfirmed_ReturnsConflict()
        {
            var req = new ForgotPasswordRequest { Email = "u@test.com" };
            var user = UserFactory.Create(email: req.Email, emailConfirmed: false);

            _userManagerMock.Setup(m => m.FindByEmailAsync(req.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(false);

            var result = await _sut.ForgotPasswordAsync(req);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.Conflict);
        }

        [Fact]
        public async Task ForgotPasswordAsync_EmailSendFails_ReturnsInternalError()
        {
            var req = new ForgotPasswordRequest { Email = "u@test.com" };
            var user = UserFactory.Create(email: req.Email, emailConfirmed: true);

            _userManagerMock.Setup(m => m.FindByEmailAsync(req.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
            _userManagerMock.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("token");
            _linkGeneratorMock.Setup(l => l.GetResetPasswordLink(user.Id, "token")).Returns("link");
            _emailTemplateBuilderMock.Setup(b => b.BuildResetPasswordEmail(user.FirstName, "link")).Returns("body");
            _emailServiceMock.Setup(s => s.SendEmailAsync(user.Email!, It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new Exception("fail"));

            var result = await _sut.ForgotPasswordAsync(req);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.InternalServerError);
        }

        [Fact]
        public async Task ForgotPasswordAsync_Success_ReturnsOk()
        {
            var req = new ForgotPasswordRequest { Email = "u@test.com" };
            var user = UserFactory.Create(email: req.Email, emailConfirmed: true);

            _userManagerMock.Setup(m => m.FindByEmailAsync(req.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
            _userManagerMock.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("token");
            _linkGeneratorMock.Setup(l => l.GetResetPasswordLink(user.Id, "token")).Returns("link");
            _emailTemplateBuilderMock.Setup(b => b.BuildResetPasswordEmail(user.FirstName, "link")).Returns("body");
            _emailServiceMock.Setup(s => s.SendEmailAsync(user.Email!, It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var result = await _sut.ForgotPasswordAsync(req);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(StatusCode.OK);
        }

        #endregion

        #region ResetPasswordAsync

        [Fact]
        public async Task ResetPasswordAsync_UserNotFound_ReturnsNotFound()
        {
            var req = new ResetPasswordRequest { UserId = Guid.NewGuid(), Token = "t", NewPassword = "p1", ConfirmNewPassword = "p1" };

            _userManagerMock.Setup(m => m.FindByIdAsync(req.UserId.ToString())).ReturnsAsync((UserEntity?)null);

            var result = await _sut.ResetPasswordAsync(req);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.NotFound);
        }

        [Fact]
        public async Task ResetPasswordAsync_ResetFails_ReturnsConfilict()
        {
            var req = new ResetPasswordRequest { UserId = Guid.NewGuid(), Token = "t", NewPassword = "p1", ConfirmNewPassword = "p1" };
            var user = UserFactory.Create();

            _userManagerMock.Setup(m => m.FindByIdAsync(req.UserId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.ResetPasswordAsync(user, req.Token, req.NewPassword)).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "err" }));

            var result = await _sut.ResetPasswordAsync(req);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.Conflict);
        }

        [Fact]
        public async Task ResetPasswordAsync_Success_ReturnsOk()
        {
            var req = new ResetPasswordRequest { UserId = Guid.NewGuid(), Token = "t", NewPassword = "p1", ConfirmNewPassword = "p1" };
            var user = UserFactory.Create();

            _userManagerMock.Setup(m => m.FindByIdAsync(req.UserId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.ResetPasswordAsync(user, req.Token, req.NewPassword)).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(true);

            var result = await _sut.ResetPasswordAsync(req);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(StatusCode.OK);
        }

        #endregion

        #region ResendConfirmationAsync

        [Fact]
        public async Task ResendConfirmationAsync_UserNotFound_ReturnsNotFound()
        {
            var req = new ResendConfirmationRequest { Email = "no@test.com" };

            _userManagerMock.Setup(m => m.FindByEmailAsync(req.Email)).ReturnsAsync((UserEntity?)null);

            var result = await _sut.ResendConfirmationAsync(req);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.NotFound);
        }

        [Fact]
        public async Task ResendConfirmationAsync_AlreadyConfirmed_ReturnsConflict()
        {
            var req = new ResendConfirmationRequest { Email = "u@test.com" };
            var user = UserFactory.Create(email: req.Email, emailConfirmed: true);

            _userManagerMock.Setup(m => m.FindByEmailAsync(req.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(true);

            var result = await _sut.ResendConfirmationAsync(req);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.Conflict);
        }

        [Fact]
        public async Task ResendConfirmationAsync_EmailSendFails_ReturnsInternalError()
        {
            var req = new ResendConfirmationRequest { Email = "u@test.com" };
            var user = UserFactory.Create(email: req.Email, emailConfirmed: false);

            _userManagerMock.Setup(m => m.FindByEmailAsync(req.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(false);
            _userManagerMock.Setup(m => m.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync("t");
            _linkGeneratorMock.Setup(l => l.GetConfirmEmailLink(user.Id, "t")).Returns("link");
            _emailTemplateBuilderMock.Setup(b => b.BuildConfirmEmail(user.FirstName, "link")).Returns("body");
            _emailServiceMock.Setup(s => s.SendEmailAsync(user.Email!, It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new Exception("err"));

            var result = await _sut.ResendConfirmationAsync(req);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.InternalServerError);
        }

        [Fact]
        public async Task ResendConfirmationAsync_Success_ReturnsOk()
        {
            var req = new ResendConfirmationRequest { Email = "u@test.com" };
            var user = UserFactory.Create(email: req.Email, emailConfirmed: false);

            _userManagerMock.Setup(m => m.FindByEmailAsync(req.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(false);
            _userManagerMock.Setup(m => m.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync("t");
            _linkGeneratorMock.Setup(l => l.GetConfirmEmailLink(user.Id, "t")).Returns("link");
            _emailTemplateBuilderMock.Setup(b => b.BuildConfirmEmail(user.FirstName, "link")).Returns("body");
            _emailServiceMock.Setup(s => s.SendEmailAsync(user.Email!, It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var result = await _sut.ResendConfirmationAsync(req);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(StatusCode.OK);
        }

        #endregion

        #region LogoutAsync

        [Fact]
        public async Task LogoutAsync_CallsSignOutAndReturnsOk()
        {
            _signInServiceMock.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask);

            var result = await _sut.LogoutAsync();

            _signInServiceMock.Verify(s => s.SignOutAsync(), Times.Once);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(StatusCode.OK);
        }

        #endregion
    }
}