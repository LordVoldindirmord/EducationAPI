using Education.DAL.Seeder.Seeders;
using Education.Domain.DTO.Request.Auth;
using Education.Domain.Entity;
using Education.Domain.Enums;
using Education.Domain.Extensions;
using FluentAssertions;
using IntegrationTests.Helpers;

namespace IntegrationTests.Tests.Auth
{
    [Collection(AuthIntegrationCollection.Name)]
    public class AuthServiceIntegrationTests
    {
        private readonly PostgresFixture _postgres;

        public AuthServiceIntegrationTests(PostgresFixture postgres)
        {
            _postgres = postgres;
        }

        private AuthTestHost CreateHost() => AuthTestHost.Create(_postgres.ConnectionString);

        #region RegisterAsync

        [Fact]
        public async Task RegisterAsync_ValidData_CreatesUserWithDefaultRoleAndSendsEmail()
        {
            await using var host = CreateHost();
            var user = TestUserData.New();

            var result = await host.Auth.RegisterAsync(user.ToRegisterRequest());

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(StatusCode.OK);
            result.Data.Should().BeTrue();
            result.Description.Should().Contain("Регистрация успешна");

            var created = await host.Users.FindByEmailAsync(user.Email);
            created.Should().NotBeNull();
            created!.UserName.Should().Be(user.Login);
            created.FirstName.Should().Be(user.FirstName);
            created.LastName.Should().Be(user.LastName);
            created.EmailConfirmed.Should().BeFalse();
            (await host.Users.IsInRoleAsync(created, UserRole.User.RoleToDbString())).Should().BeTrue();

            host.Email.Sent.Should().ContainSingle();
            host.Email.Last.To.Should().Be(user.Email);
            host.Email.Last.Subject.Should().Be("Подтверждение регистрации");
            host.Email.Last.Body.Should().Contain(user.FirstName);
            host.Email.Last.Body.Should().Contain("token=");
        }

        [Fact]
        public async Task RegisterAsync_DuplicateEmail_ReturnsConflict()
        {
            await using var host = CreateHost();
            var first = TestUserData.New();
            await host.Auth.RegisterAsync(first.ToRegisterRequest());

            var duplicate = first with { Login = $"other_{Guid.NewGuid():N}"[..16] };

            var result = await host.Auth.RegisterAsync(duplicate.ToRegisterRequest());

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.Conflict);
            result.Description.Should().Contain("Email");
        }

        [Fact]
        public async Task RegisterAsync_DuplicateLogin_ReturnsConflict()
        {
            await using var host = CreateHost();
            var first = TestUserData.New();
            await host.Auth.RegisterAsync(first.ToRegisterRequest());

            var duplicate = first with { Email = $"other_{Guid.NewGuid():N}@test.com" };

            var result = await host.Auth.RegisterAsync(duplicate.ToRegisterRequest());

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.Conflict);
            result.Description.Should().Contain("Login");
        }

        [Fact]
        public async Task RegisterAsync_WeakPassword_ReturnsBadRequest()
        {
            await using var host = CreateHost();
            var user = TestUserData.New(password: "123");

            var result = await host.Auth.RegisterAsync(user.ToRegisterRequest());

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.BadRequest);
            result.Description.Should().NotBeNullOrWhiteSpace();
            (await host.Users.FindByEmailAsync(user.Email)).Should().BeNull();
        }

        [Fact]
        public async Task RegisterAsync_EmailSendFails_StillCreatesUser()
        {
            await using var host = CreateHost();
            host.Email.ThrowOnSend = true;
            var user = TestUserData.New();

            var result = await host.Auth.RegisterAsync(user.ToRegisterRequest());

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(StatusCode.OK);
            (await host.Users.FindByEmailAsync(user.Email)).Should().NotBeNull();
            host.Email.Sent.Should().BeEmpty();
        }

        [Fact]
        public async Task RegisterAsync_MissingDefaultRole_ThrowsAndLeavesCreatedUser()
        {
            await using var host = CreateHost();
            var userRole = await host.Roles.FindByNameAsync(UserRole.User.RoleToDbString());
            userRole.Should().NotBeNull();

            try
            {
                (await host.Roles.DeleteAsync(userRole!)).Succeeded.Should().BeTrue();
                var user = TestUserData.New();

                var act = async () => await host.Auth.RegisterAsync(user.ToRegisterRequest());

                await act.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("*does not exist*");

                var created = await host.Users.FindByEmailAsync(user.Email);
                created.Should().NotBeNull("пользователь уже создан до AddToRoleAsync, а Identity кидает исключение вместо IdentityResult");
            }
            finally
            {
                await new RoleSeeder(host.Roles).SeedRoleAsync();
            }
        }

        #endregion

        #region ConfirmEmailAsync

        [Fact]
        public async Task ConfirmEmailAsync_UserNotFound_ReturnsNotFound()
        {
            await using var host = CreateHost();

            var result = await host.Auth.ConfirmEmailAsync(new ConfirmEmailRequest
            {
                UserId = Guid.NewGuid(),
                Token = "token",
            });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.NotFound);
        }

        [Fact]
        public async Task ConfirmEmailAsync_AlreadyConfirmed_ReturnsConflict()
        {
            await using var host = CreateHost();
            var created = await RegisterAndConfirmAsync(host, TestUserData.New());

            var result = await host.Auth.ConfirmEmailAsync(new ConfirmEmailRequest
            {
                UserId = created.Id,
                Token = "any",
            });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.Conflict);
        }

        [Fact]
        public async Task ConfirmEmailAsync_InvalidToken_ReturnsBadRequest()
        {
            await using var host = CreateHost();
            var user = TestUserData.New();
            await host.Auth.RegisterAsync(user.ToRegisterRequest());
            var created = await host.Users.FindByEmailAsync(user.Email);

            var result = await host.Auth.ConfirmEmailAsync(new ConfirmEmailRequest
            {
                UserId = created!.Id,
                Token = "invalid-token",
            });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.BadRequest);
            created.EmailConfirmed.Should().BeFalse();
        }

        [Fact]
        public async Task ConfirmEmailAsync_ValidToken_ConfirmsEmailAndReturnsLoginData()
        {
            await using var host = CreateHost();
            var user = TestUserData.New();
            await host.Auth.RegisterAsync(user.ToRegisterRequest());
            var created = await host.Users.FindByEmailAsync(user.Email);
            var token = EmailLinkParser.ExtractQueryParam(host.Email.Last.Body, "token");

            var result = await host.Auth.ConfirmEmailAsync(new ConfirmEmailRequest
            {
                UserId = created!.Id,
                Token = token,
            });

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(StatusCode.OK);
            result.Data.Should().NotBeNull();
            result.Data!.Email.Should().Be(user.Email);
            result.Data.Login.Should().Be(user.Login);
            result.Data.UserId.Should().Be(created.Id);
            result.Data.Role.Should().Be(UserRole.User);

            (await host.Users.FindByIdAsync(created.Id.ToString()))!.EmailConfirmed.Should().BeTrue();
        }

        #endregion

        #region LoginAsync

        [Fact]
        public async Task LoginAsync_UnknownLogin_ReturnsUnauthorized()
        {
            await using var host = CreateHost();

            var result = await host.Auth.LoginAsync(new LoginRequest
            {
                LoginOrEmail = "unknown_login",
                Password = "Password1!",
            });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.Unauthorized);
            host.SignIn.SignIns.Should().BeEmpty();
        }

        [Fact]
        public async Task LoginAsync_UnknownEmail_ReturnsUnauthorized()
        {
            await using var host = CreateHost();

            var result = await host.Auth.LoginAsync(new LoginRequest
            {
                LoginOrEmail = "unknown@test.com",
                Password = "Password1!",
            });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.Unauthorized);
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ReturnsUnauthorized()
        {
            await using var host = CreateHost();
            var user = TestUserData.New();
            await RegisterAndConfirmAsync(host, user);

            var result = await host.Auth.LoginAsync(new LoginRequest
            {
                LoginOrEmail = user.Login,
                Password = "WrongPass1!",
            });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.Unauthorized);
            host.SignIn.SignIns.Should().BeEmpty();
        }

        [Fact]
        public async Task LoginAsync_EmailNotConfirmed_ReturnsForbidden()
        {
            await using var host = CreateHost();
            var user = TestUserData.New();
            await host.Auth.RegisterAsync(user.ToRegisterRequest());

            var result = await host.Auth.LoginAsync(new LoginRequest
            {
                LoginOrEmail = user.Login,
                Password = user.Password,
            });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.Forbidden);
            host.SignIn.SignIns.Should().BeEmpty();
        }

        [Fact]
        public async Task LoginAsync_ByLogin_SignsInAndReturnsUser()
        {
            await using var host = CreateHost();
            var user = TestUserData.New();
            var created = await RegisterAndConfirmAsync(host, user);

            var result = await host.Auth.LoginAsync(new LoginRequest
            {
                LoginOrEmail = user.Login,
                Password = user.Password,
                RememberMe = true,
            });

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(StatusCode.OK);
            result.Data!.UserId.Should().Be(created.Id);
            result.Data.Email.Should().Be(user.Email);
            result.Data.Login.Should().Be(user.Login);
            result.Data.Role.Should().Be(UserRole.User);
            host.SignIn.SignIns.Should().ContainSingle(x => x.UserId == created.Id && x.IsPersistent);
        }

        [Fact]
        public async Task LoginAsync_ByEmail_SignsInWithoutRememberMe()
        {
            await using var host = CreateHost();
            var user = TestUserData.New();
            var created = await RegisterAndConfirmAsync(host, user);

            var result = await host.Auth.LoginAsync(new LoginRequest
            {
                LoginOrEmail = user.Email,
                Password = user.Password,
                RememberMe = false,
            });

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(StatusCode.OK);
            host.SignIn.SignIns.Should().ContainSingle(x => x.UserId == created.Id && !x.IsPersistent);
        }

        #endregion

        #region ResendConfirmationAsync

        [Fact]
        public async Task ResendConfirmationAsync_UserNotFound_ReturnsNotFound()
        {
            await using var host = CreateHost();

            var result = await host.Auth.ResendConfirmationAsync(new ResendConfirmationRequest
            {
                Email = "missing@test.com",
            });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.NotFound);
        }

        [Fact]
        public async Task ResendConfirmationAsync_AlreadyConfirmed_ReturnsConflict()
        {
            await using var host = CreateHost();
            var user = TestUserData.New();
            await RegisterAndConfirmAsync(host, user);

            var result = await host.Auth.ResendConfirmationAsync(new ResendConfirmationRequest
            {
                Email = user.Email,
            });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.Conflict);
        }

        [Fact]
        public async Task ResendConfirmationAsync_EmailSendFails_ReturnsInternalError()
        {
            await using var host = CreateHost();
            var user = TestUserData.New();
            await host.Auth.RegisterAsync(user.ToRegisterRequest());
            host.Email.ThrowOnSend = true;

            var result = await host.Auth.ResendConfirmationAsync(new ResendConfirmationRequest
            {
                Email = user.Email,
            });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.InternalServerError);
        }

        [Fact]
        public async Task ResendConfirmationAsync_UnconfirmedUser_SendsNewLetter()
        {
            await using var host = CreateHost();
            var user = TestUserData.New();
            await host.Auth.RegisterAsync(user.ToRegisterRequest());

            var result = await host.Auth.ResendConfirmationAsync(new ResendConfirmationRequest
            {
                Email = user.Email,
            });

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(StatusCode.OK);
            host.Email.Sent.Should().HaveCount(2);
            host.Email.Last.Subject.Should().Be("Подтверждение регистрации");
        }

        #endregion

        #region ForgotPasswordAsync

        [Fact]
        public async Task ForgotPasswordAsync_UserNotFound_ReturnsNotFound()
        {
            await using var host = CreateHost();

            var result = await host.Auth.ForgotPasswordAsync(new ForgotPasswordRequest
            {
                Email = "missing@test.com",
            });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.NotFound);
        }

        [Fact]
        public async Task ForgotPasswordAsync_EmailNotConfirmed_ReturnsConflict()
        {
            await using var host = CreateHost();
            var user = TestUserData.New();
            await host.Auth.RegisterAsync(user.ToRegisterRequest());

            var result = await host.Auth.ForgotPasswordAsync(new ForgotPasswordRequest
            {
                Email = user.Email,
            });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.Conflict);
        }

        [Fact]
        public async Task ForgotPasswordAsync_EmailSendFails_ReturnsInternalError()
        {
            await using var host = CreateHost();
            var user = TestUserData.New();
            await RegisterAndConfirmAsync(host, user);
            host.Email.ThrowOnSend = true;

            var result = await host.Auth.ForgotPasswordAsync(new ForgotPasswordRequest
            {
                Email = user.Email,
            });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.InternalServerError);
        }

        [Fact]
        public async Task ForgotPasswordAsync_ConfirmedUser_SendsResetLink()
        {
            await using var host = CreateHost();
            var user = TestUserData.New();
            await RegisterAndConfirmAsync(host, user);

            var result = await host.Auth.ForgotPasswordAsync(new ForgotPasswordRequest
            {
                Email = user.Email,
            });

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(StatusCode.OK);
            host.Email.Last.Subject.Should().Be("Сброс пароля");
            host.Email.Last.Body.Should().Contain("token=");
        }

        #endregion

        #region ResetPasswordAsync

        [Fact]
        public async Task ResetPasswordAsync_UserNotFound_ReturnsNotFound()
        {
            await using var host = CreateHost();

            var result = await host.Auth.ResetPasswordAsync(new ResetPasswordRequest
            {
                UserId = Guid.NewGuid(),
                Token = "token",
                NewPassword = "NewPassword1!",
                ConfirmNewPassword = "NewPassword1!",
            });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.NotFound);
        }

        [Fact]
        public async Task ResetPasswordAsync_EmailNotConfirmed_ReturnsConflict()
        {
            await using var host = CreateHost();
            var user = TestUserData.New();
            await host.Auth.RegisterAsync(user.ToRegisterRequest());
            var created = await host.Users.FindByEmailAsync(user.Email);

            var result = await host.Auth.ResetPasswordAsync(new ResetPasswordRequest
            {
                UserId = created!.Id,
                Token = "token",
                NewPassword = "NewPassword1!",
                ConfirmNewPassword = "NewPassword1!",
            });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.Conflict);
        }

        [Fact]
        public async Task ResetPasswordAsync_InvalidToken_ReturnsBadRequest()
        {
            await using var host = CreateHost();
            var user = TestUserData.New();
            var created = await RegisterAndConfirmAsync(host, user);

            var result = await host.Auth.ResetPasswordAsync(new ResetPasswordRequest
            {
                UserId = created.Id,
                Token = "invalid-token",
                NewPassword = "NewPassword1!",
                ConfirmNewPassword = "NewPassword1!",
            });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.BadRequest);
        }

        [Fact]
        public async Task ResetPasswordAsync_WeakPassword_ReturnsBadRequest()
        {
            await using var host = CreateHost();
            var user = TestUserData.New();
            var created = await RegisterAndConfirmAsync(host, user);
            await host.Auth.ForgotPasswordAsync(new ForgotPasswordRequest { Email = user.Email });
            var token = EmailLinkParser.ExtractQueryParam(host.Email.Last.Body, "token");

            var result = await host.Auth.ResetPasswordAsync(new ResetPasswordRequest
            {
                UserId = created.Id,
                Token = token,
                NewPassword = "123",
                ConfirmNewPassword = "123",
            });

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCode.BadRequest);
        }

        [Fact]
        public async Task ResetPasswordAsync_ValidToken_ChangesPassword()
        {
            await using var host = CreateHost();
            var user = TestUserData.New();
            var created = await RegisterAndConfirmAsync(host, user);
            await host.Auth.ForgotPasswordAsync(new ForgotPasswordRequest { Email = user.Email });
            var token = EmailLinkParser.ExtractQueryParam(host.Email.Last.Body, "token");
            const string newPassword = "NewPassword1!";

            var result = await host.Auth.ResetPasswordAsync(new ResetPasswordRequest
            {
                UserId = created.Id,
                Token = token,
                NewPassword = newPassword,
                ConfirmNewPassword = newPassword,
            });

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(StatusCode.OK);

            var oldPasswordLogin = await host.Auth.LoginAsync(new LoginRequest
            {
                LoginOrEmail = user.Login,
                Password = user.Password,
            });
            oldPasswordLogin.StatusCode.Should().Be(StatusCode.Unauthorized);

            var newPasswordLogin = await host.Auth.LoginAsync(new LoginRequest
            {
                LoginOrEmail = user.Login,
                Password = newPassword,
            });
            newPasswordLogin.IsSuccess.Should().BeTrue();
        }

        #endregion

        #region LogoutAsync

        [Fact]
        public async Task LogoutAsync_CallsSignOutAndReturnsOk()
        {
            await using var host = CreateHost();

            var result = await host.Auth.LogoutAsync();

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(StatusCode.OK);
            host.SignIn.SignOutCount.Should().Be(1);
        }

        #endregion

        #region Full flow

        [Fact]
        public async Task FullAuthFlow_RegisterConfirmLoginLogoutForgotReset()
        {
            await using var host = CreateHost();
            var user = TestUserData.New();

            (await host.Auth.RegisterAsync(user.ToRegisterRequest())).IsSuccess.Should().BeTrue();

            (await host.Auth.LoginAsync(new LoginRequest
            {
                LoginOrEmail = user.Email,
                Password = user.Password,
            })).StatusCode.Should().Be(StatusCode.Forbidden);

            (await host.Auth.ResendConfirmationAsync(new ResendConfirmationRequest { Email = user.Email }))
                .IsSuccess.Should().BeTrue();

            var created = await host.Users.FindByEmailAsync(user.Email);
            var confirmToken = EmailLinkParser.ExtractQueryParam(host.Email.Last.Body, "token");
            var confirm = await host.Auth.ConfirmEmailAsync(new ConfirmEmailRequest
            {
                UserId = created!.Id,
                Token = confirmToken,
            });
            confirm.IsSuccess.Should().BeTrue();

            var login = await host.Auth.LoginAsync(new LoginRequest
            {
                LoginOrEmail = user.Login,
                Password = user.Password,
                RememberMe = true,
            });
            login.IsSuccess.Should().BeTrue();
            host.SignIn.SignIns.Should().Contain(x => x.IsPersistent);

            (await host.Auth.LogoutAsync()).IsSuccess.Should().BeTrue();
            host.SignIn.SignOutCount.Should().Be(1);

            (await host.Auth.ForgotPasswordAsync(new ForgotPasswordRequest { Email = user.Email }))
                .IsSuccess.Should().BeTrue();

            const string newPassword = "ResetPassword1!";
            var resetToken = EmailLinkParser.ExtractQueryParam(host.Email.Last.Body, "token");
            (await host.Auth.ResetPasswordAsync(new ResetPasswordRequest
            {
                UserId = created.Id,
                Token = resetToken,
                NewPassword = newPassword,
                ConfirmNewPassword = newPassword,
            })).IsSuccess.Should().BeTrue();

            (await host.Auth.LoginAsync(new LoginRequest
            {
                LoginOrEmail = user.Email,
                Password = newPassword,
            })).IsSuccess.Should().BeTrue();
        }

        #endregion

        private static async Task<UserEntity> RegisterAndConfirmAsync(AuthTestHost host, TestUserData user)
        {
            var register = await host.Auth.RegisterAsync(user.ToRegisterRequest());
            register.IsSuccess.Should().BeTrue(register.Description);

            var created = await host.Users.FindByEmailAsync(user.Email);
            created.Should().NotBeNull();

            var token = EmailLinkParser.ExtractQueryParam(host.Email.Last.Body, "token");
            var confirm = await host.Auth.ConfirmEmailAsync(new ConfirmEmailRequest
            {
                UserId = created!.Id,
                Token = token,
            });
            confirm.IsSuccess.Should().BeTrue(confirm.Description);

            return (await host.Users.FindByIdAsync(created.Id.ToString()))!;
        }
    }
}