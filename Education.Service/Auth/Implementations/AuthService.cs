using Education.Domain.DTO.Request.Auth;
using Education.Domain.DTO.Response.Auth;
using Education.Domain.Entity;
using Education.Domain.Enums;
using Education.Domain.Extensions;
using Education.Domain.ResponseModel;
using Education.Service.Auth.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Education.Service.Auth.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<UserEntity> _userManager;
        private readonly ISignInService _signInService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;
        private readonly ILinkGenerator _linkGenerator;
        private readonly IEmailTemplateBuilder _emailTemplateBuilder;

        public AuthService(
            UserManager<UserEntity> userManager,
            ISignInService signInService,
            IEmailService emailService,
            ILogger<AuthService> logger,
            ILinkGenerator linkGenerator,
            IEmailTemplateBuilder emailTemplateBuilder)
        {
            _userManager = userManager;
            _signInService = signInService;
            _emailService = emailService;
            _logger = logger;
            _linkGenerator = linkGenerator;
            _emailTemplateBuilder = emailTemplateBuilder;
        }

        public async Task<InternalResponse<LoginResponse>> ConfirmEmailAsync(ConfirmEmailRequest request)
        {
            // Поиск пользователя по Id
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());

            if (user == null)
                return CreatorResponse.NotFound<LoginResponse>("Пользователь не найден");

            // Проверка, подтвержден ли уже Email
            if (user.EmailConfirmed)
                return CreatorResponse.Conflict<LoginResponse>("Email уже подтвержден");

            // Подтверждение самого Email
            var confirmResult = await _userManager.ConfirmEmailAsync(user, request.Token);

            if (!confirmResult.Succeeded)
            {
                var errors = string.Join("; ", confirmResult.Errors.Select(e => e.Description));
                return CreatorResponse.BadRequest<LoginResponse>(errors);
            }

            // Получить роль пользователя
            var role = await GetUserRole(user);

            // Формирование ответа
            var response = BuildLoginResponse(user, role);

            return CreatorResponse.Ok(response, "Email успешно подтвержден");
        }

        public async Task<InternalResponse<bool>> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            // Ищем ползователя по Email
            var user = await _userManager.FindByEmailAsync(request.Email);

            // Проверяем, что ползователь существует
            if (user == null)
                return CreatorResponse.NotFound<bool>("Пользователь с таким Email не найден");

            // Проверяем, не подтвержден ли уже Email
            if (!await _userManager.IsEmailConfirmedAsync(user))
                return CreatorResponse.Conflict<bool>("Email не подтвержден, сначала подтвердите почту");

            // Формирование письма
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var link = _linkGenerator.GetResetPasswordLink(user.Id, token);
            var body = _emailTemplateBuilder.BuildResetPasswordEmail(user.FirstName, link);

            // Отправка письма
            try
            {
                await _emailService.SendEmailAsync(user.Email!, "Сброс пароля", body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка отправки письма для сброса пароля: {user.Email}");

                return CreatorResponse.InternalError<bool>("Не удалось отправить письмо. Попробуйте позже");
            }

            return CreatorResponse.Ok(true, "Ссылка для сброса пароля отправлена на Email");
        }

        public async Task<InternalResponse<LoginResponse>> LoginAsync(LoginRequest request)
        {
            UserEntity? user;

            // Поиск самого пользователя
            if (request.LoginOrEmail.Contains('@'))
                user = await _userManager.FindByEmailAsync(request.LoginOrEmail);
            else
                user = await _userManager.FindByNameAsync(request.LoginOrEmail);

            if (user == null)
                return CreatorResponse.Unauthorized<LoginResponse>("Неверный Login/Email или пароль");

            // Проверка пароля
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);

            if (!isPasswordValid)
                return CreatorResponse.Unauthorized<LoginResponse>("Неверный Login/Email или пароль");

            // Проверка подтверждения Email
            if (!user.EmailConfirmed)
                return CreatorResponse.Forbidden<LoginResponse>("Email не подтвержден. Проверьте почту");

            // Cookie
            await _signInService.SignInAsync(user, isPersistent: request.RememberMe);

            // Получаем роль
            var role = await GetUserRole(user);

            // Ответ
            var response = BuildLoginResponse(user, role);

            return CreatorResponse.Ok(response, "Вход выполнен успешно");
        }

        public async Task<InternalResponse<bool>> RegisterAsync(RegisterRequest request)
        {
            // Проверка занятости Email
            var existingByEmail = await _userManager.FindByEmailAsync(request.Email);

            if (existingByEmail != null)
                return CreatorResponse.Conflict<bool>("Пользователь с таким Email уже существует");

            // Проверка занятости Login
            var existingByLogin = await _userManager.FindByNameAsync(request.Login);

            if (existingByLogin != null)
                return CreatorResponse.Conflict<bool>("Пользователь с таким Login уже существует");

            // Создание пользователя
            var user = new UserEntity
            {
                Email = request.Email,
                UserName = request.Login,
                FirstName = request.FirstName,
                LastName = request.LastName,
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);

            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                return CreatorResponse.BadRequest<bool>(errors);
            }

            // Назначение роли по умолчанию
            var addRoleResult = await _userManager.AddToRoleAsync(user, UserRole.User.RoleToDbString());

            if (!addRoleResult.Succeeded)
            {
                // Откатываем создание пользователя
                await _userManager.DeleteAsync(user);

                var errors = string.Join("; ", addRoleResult.Errors.Select(e => e.Description));
                return CreatorResponse.InternalError<bool>($"Не удалось назначить роль: {errors}");
            }

            // Формирование письма
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = _linkGenerator.GetConfirmEmailLink(user.Id, token);
            var body = _emailTemplateBuilder.BuildConfirmEmail(user.FirstName, link);

            // Отправка письма
            try
            {
                await _emailService.SendEmailAsync(user.Email!, "Подтверждение регистрации", body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка отправки письма подтверждения: {user.Email}");
                // Не откатываем — пользователь создан, письмо можно переотправить
            }

            return CreatorResponse.Ok(true, "Регистрация успешна. Проверьте Email для подтверждения");
        }

        public async Task<InternalResponse<bool>> ResendConfirmationAsync(ResendConfirmationRequest request)
        {
            // Поиск пользователя
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
                return CreatorResponse.NotFound<bool>("Пользователь с таким Email не найден");

            if (user.EmailConfirmed)
                return CreatorResponse.Conflict<bool>("Email уже подтвержден");

            // Создание тела письма
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = _linkGenerator.GetConfirmEmailLink(user.Id, token);
            var body = _emailTemplateBuilder.BuildConfirmEmail(user.FirstName, link);

            // Отправка письма
            try
            {
                await _emailService.SendEmailAsync(user.Email!, "Подтверждение регистрации", body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка повторной отправки письма подтверждения: {user.Email}");

                return CreatorResponse.InternalError<bool>("Не удалось отправить письмо. Попробуйте позже");
            }

            return CreatorResponse.Ok(true, "Письмо отправлено повторно");
        }

        public async Task<InternalResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request)
        {
            // Поиск самого пользователя
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());

            if (user == null)
                return CreatorResponse.NotFound<bool>("Пользователь с таким Id не найден");

            // Проверка, подтверждён ли Email
            if (!await _userManager.IsEmailConfirmedAsync(user))
                return CreatorResponse.Conflict<bool>("Email не подтвержден");

            // Сброс пароля
            var resetResult = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

            if (!resetResult.Succeeded)
            {
                var errors = string.Join("; ", resetResult.Errors.Select(e => e.Description));
                return CreatorResponse.BadRequest<bool>(errors);
            }

            return CreatorResponse.Ok(true, "Пароль успешно изменён");
        }

        public async Task<InternalResponse<bool>> LogoutAsync()
        {
            await _signInService.SignOutAsync();
            return CreatorResponse.Ok(true, "Выход выполнен успешно");
        }

        private async Task<UserRole> GetUserRole(UserEntity user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var primaryRole = roles.FirstOrDefault() ?? UserRole.User.RoleToDbString();

            var role = primaryRole.DbStringToRole();

            return role;
        }

        private LoginResponse BuildLoginResponse(UserEntity user, UserRole role) => new LoginResponse()
        {
            UserId = user.Id,
            Email = user.Email!,
            Login = user!.UserName!,
            Role = role,
        };
    }
}