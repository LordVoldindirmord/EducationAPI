using Education.Domain.DTO.Request.Auth;
using Education.Domain.DTO.Response.Auth;
using Education.Domain.ResponseModel;

namespace Education.Service.Auth.Interfaces
{
    /// <summary>
    /// Сервис регистрации и авторизации пользователей
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Зарегистрировать нового пользователя
        /// </summary>
        /// <param name="request">Данные для регистрации пользователя</param>
        /// <returns>Флаг успешности операции (true - операция успешна, false - что-то пошло не так)</returns>
        Task<InternalResponse<bool>> RegisterAsync(RegisterRequest request);

        /// <summary>
        /// Авторизовать пользователя по email и паролю
        /// </summary>
        /// <param name="request">Данные для входа</param>
        /// <returns>Аккаунт авторизованного пользователя</returns>
        Task<InternalResponse<LoginResponse>> LoginAsync(LoginRequest request);

        /// <summary>
        /// Подтвердить email пользователя по токену из письма
        /// </summary>
        /// <param name="request">Токен подтверждения из ссылки</param>
        /// <returns>Аккаунт авторизованного пользователя</returns>
        Task<InternalResponse<LoginResponse>> ConfirmEmailAsync(ConfirmEmailRequest request);

        /// <summary>
        /// Отправить письмо со ссылкой для сброса пароля
        /// </summary>
        /// <param name="request">Данные пользователя, забывшего пароль</param>
        /// <returns>Флаг успешности операции (true - операция успешна, false - что-то пошло не так)</returns>
        Task<InternalResponse<bool>> ForgotPasswordAsync(ForgotPasswordRequest request);

        /// <summary>
        /// Сбросить пароль по токену из письма
        /// </summary>
        /// <param name="request">Токен сброса, новый пароль и подтверждение</param>
        /// <returns>Флаг успешности операции (true - операция успешна, false - что-то пошло не так)</returns>
        Task<InternalResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request);

        /// <summary>
        /// Повторно отправить письмо для подтверждения email
        /// </summary>
        /// <param name="request">Email пользователя</param>
        /// <returns>Флаг успешности операции (true - операция успешна, false - что-то пошло не так)</returns>
        Task<InternalResponse<bool>> ResendConfirmationAsync(ResendConfirmationRequest request);

        /// <summary>
        /// Выход из системы (удаление cookie)
        /// </summary>
        /// <returns>Флаг успешности операции</returns>
        Task<InternalResponse<bool>> LogoutAsync();
    }
}