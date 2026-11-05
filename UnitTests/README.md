# Unit-тесты сервисов

В проекте есть отдельные наборы для всех 17 реализаций из Education.Service: AuthService, CurrentUserService, SignInService, EmailService, EmailTemplateBuilder, LinkGenerator, PermissionService, CategoryService, CourseService, CourseAccessService, CourseMembershipService, EnrollmentService, FileManager, PurchaseService, CoursePurchaseHandler, RolePurchaseHandler и YooKassaPaymentService.

Запуск из Backend: `dotnet test UnitTests/UnitTests.csproj`. Для unit-тестов не нужны PostgreSQL, SMTP, интернет или ключи ЮKassa. Репозитории, Identity, контекст БД и транзакции подменяются через Moq. Для EmailService подставляется ISmtpClient, для YooKassaPaymentService используется настоящий SDK с локальным HttpMessageHandler, который возвращает подготовленные ответы и никогда не отправляет запросы в сеть. Обычные конструкторы приложения сохранены.

Проверяются успешные операции, основные отказы и побочные действия: переданные данные, сохранения, выдача и отзыв доступа, отсутствие записи при отказе, фиксация или откат транзакции, очистка новой обложки при ошибке БД, сохранение старой обложки до успешного обновления, идемпотентные повторные события платежей. Тесты AuthService переписаны: проверяются фактические вызовы зависимостей и корректные статусы, а не только флаг успешности.

FileManager проверяется на локальных временных файлах в отдельных папках с уникальным именем. Существующие проверки чтения, загрузки, удаления, ограничения размера и отмены сохранены, дополнены проверками параметров и отсутствия файловых действий при раннем отказе. Эти проверки не обращаются к хранилищу приложения. Тесты валидации DTO также сохранены.

Транзакционные unit-тесты проверяют решение обработчика о Commit или Rollback. Фактическую атомарность PostgreSQL, сетевое взаимодействие с SMTP и реальной ЮKassa они не проверяют. HTTP-контракт остается в отдельном проекте IntegrationTests.

В результате тестирования исправлены проигнорированные IdentityResult в RolePurchaseHandler: ошибка удаления прежних ролей, отзыва купленной роли или возврата базовой роли теперь приводит к откату и ответу об ошибке. Соответствующие регрессионные тесты сначала воспроизвели ошибочное подтверждение операции, затем прошли после исправления.

Проверенный результат: 223 тестовых случая, включая сохраненные проверки DTO и файлов. Измерение Coverlet только для сборки Education.Service: 1082 из 1125 строк (96,17%), 95,64% ветвей. Не покрываются некоторые ошибки файловой системы, поврежденные внутренние enum и обычные конструкторы внешних клиентов, поскольку в unit-тестах используются их подмены.

Команда измерения покрытия: `dotnet test UnitTests/UnitTests.csproj --collect "XPlat Code Coverage" --results-directory UnitTests/TestResults -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Include=[Education.Service]*`. Отчет Cobertura записывается в подпапку TestResults с уникальным именем. Копия проверенного отчета сохранена в UnitTests/TestResults/coverage.cobertura.xml.
