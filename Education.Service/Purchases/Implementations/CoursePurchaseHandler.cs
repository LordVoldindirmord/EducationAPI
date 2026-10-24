using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;
using Education.Domain.Enums;
using Education.Domain.ResponseModel;
using Education.Service.Courses.Interfaces;
using Education.Service.Purchases.Interfaces;

namespace Education.Service.Purchases.Implementations
{
    public class CoursePurchaseHandler : ICoursePurchaseHandler
    {
        private readonly ICourseRepository _courseRepository;
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IEnrollmentService _enrollmentService;
        private readonly ICourseAccessService _courseAccessService;
        private readonly EducationDbContext _context;

        public CoursePurchaseHandler(
            ICourseRepository courseRepository,
            IPurchaseRepository purchaseRepository,
            IEnrollmentService enrollmentService,
            ICourseAccessService courseAccessService,
            EducationDbContext context)
        {
            _courseRepository = courseRepository;
            _purchaseRepository = purchaseRepository;
            _enrollmentService = enrollmentService;
            _courseAccessService = courseAccessService;
            _context = context;
        }

        public async Task<InternalResponse<bool>> CompleteCoursePurchaseAsync(PurchaseEntity purchase)
        {
            // Проверка, что это точно сущность покупки курсов
            if (purchase is not CoursePurchaseEntity coursePurchase)
                return CreatorResponse.InternalError<bool>("Неверный тип покупки (ожидался курс)");

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Меняем статус
                purchase.Status = PurchaseStatus.Completed;
                purchase.CompletedAt = DateTime.UtcNow;

                _purchaseRepository.Update(purchase);
                await _context.SaveChangesAsync();

                // Выдаем доступ
                var enrollResult = await _enrollmentService.EnrollAsync(coursePurchase.UserId, coursePurchase.CourseId);

                if (!enrollResult.IsSuccess && enrollResult.StatusCode != StatusCode.Conflict)
                {
                    await dbTransaction.RollbackAsync();

                    return CreatorResponse.InternalError<bool>($"Не удалось выдать доступ: {enrollResult.Description}");
                }

                await dbTransaction.CommitAsync();

                return CreatorResponse.Ok(true, "Оплата прошла успешно, доступ к курсу открыт");
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();

                return CreatorResponse.InternalError<bool>("Ошибка при завершении покупки курса");
            }
        }

        public async Task<InternalResponse<CourseEntity>> GetCourseForPurchaseAsync(Guid userId, Guid courseId)
        {
            var course = await _courseRepository.GetByIdAsync(courseId);

            if (course == null)
                return CreatorResponse.NotFound<CourseEntity>("Курс не найден");

            if (!course.IsPublished)
                return CreatorResponse.Conflict<CourseEntity>("Курс не опубликован");

            if (course.Price <= 0)
                return CreatorResponse.Conflict<CourseEntity>("Курс бесплатный, покупка не требуется");

            if (await _purchaseRepository.ExistsCompletedCourseAsync(userId, courseId))
                return CreatorResponse.Conflict<CourseEntity>("Вы уже купили этот курс");

            var accessType = await _courseAccessService.GetAccessTypeAsync(userId, courseId);

            if (accessType != CourseAccessType.None)
                return CreatorResponse.Conflict<CourseEntity>("У вас уже есть доступ к данному курсу");

            return CreatorResponse.Ok(course);
        }

        public async Task<InternalResponse<bool>> RevokeCourseAccessAsync(PurchaseEntity purchase)
        {
            // Проверяем, что это точно сущность покупки курсов
            if (purchase is not CoursePurchaseEntity coursePurchase)
                return CreatorResponse.InternalError<bool>("Неверный тип покупки");

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Меняем статус оплаты
                purchase.Status = PurchaseStatus.Refunded;

                _context.Purchase.Update(purchase);
                await _context.SaveChangesAsync();

                // Отписываем пользователя от курса
                var unenrollResult = await _enrollmentService.UnenrollAsync(coursePurchase.UserId, coursePurchase.CourseId);

                if (!unenrollResult.IsSuccess && unenrollResult.StatusCode != StatusCode.NotFound)
                {
                    await dbTransaction.RollbackAsync();
                    return CreatorResponse.InternalError<bool>($"Не удалось забрать доступ: {unenrollResult.Description}");
                }

                await dbTransaction.CommitAsync();

                return CreatorResponse.Ok(true, "Возврат оформлен, доступ закрыт");
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                return CreatorResponse.InternalError<bool>("Ошибка при возврате");
            }
        }
    }
}