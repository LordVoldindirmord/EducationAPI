using Education.DAL.Interfaces;
using Education.Domain.DTO.Response.Payment;
using Education.Service.Purchases.Implementations;
using Education.Service.Purchases.Interfaces;

namespace UnitTests.Purchases;

public class PurchaseServiceTests
{
    private readonly Mock<IPurchaseRepository> _repo = new();
    private readonly Mock<IPaymentService> _gateway = new();
    private readonly Mock<ICoursePurchaseHandler> _courses = new();
    private readonly Mock<IRolePurchaseHandler> _roles = new();
    private readonly Guid _user = Guid.NewGuid(), _target = Guid.NewGuid();
    private const string Tx = "transaction", ReturnUrl = "https://front.test/return";
    private PurchaseService Sut => new(_repo.Object, _gateway.Object, _courses.Object, _roles.Object);
    private PurchaseEntity Purchase(PurchaseType type, PurchaseStatus status = PurchaseStatus.Pending)
    {
        PurchaseEntity p = type == PurchaseType.Course ? new CoursePurchaseEntity { CourseId = _target } : new RolePurchaseEntity { RoleId = _target };
        p.PurchaseType = type; p.Status = status; p.TransactionId = Tx; p.UserId = _user;
        _repo.Setup(r => r.GetByTransactionIdTrackedAsync(Tx)).ReturnsAsync(p);
        return p;
    }
    private Task<InternalResponse<PaymentResult>> Initiate(bool role) => role
        ? Sut.InitiateRolePurchaseAsync(_user, "course_creator", ReturnUrl)
        : Sut.InitiateCoursePurchaseAsync(_user, _target, ReturnUrl);

    public PurchaseServiceTests()
    {
        _courses.Setup(c => c.GetCourseForPurchaseAsync(_user, _target)).ReturnsAsync(CreatorResponse.Ok(new CourseEntity { Id = _target, Price = 100m, Title = "Course" }));
        _roles.Setup(r => r.GetRoleForPurchaseAsync(_user, "course_creator")).ReturnsAsync(CreatorResponse.Ok(new AppRoleEntity { Id = _target, Price = 100m, Name = "course_creator" }));
        _gateway.Setup(g => g.CreatePaymentAsync(100m, "RUB", It.IsAny<string>(), ReturnUrl)).ReturnsAsync(CreatorResponse.Ok(new PaymentResult { TransactionId = Tx, ConfirmationUrl = "https://pay.test" }));
        _gateway.Setup(g => g.ConfirmPaymentAsync(Tx)).ReturnsAsync(CreatorResponse.Ok(true));
        _gateway.Setup(g => g.RefundPaymentAsync(Tx)).ReturnsAsync(CreatorResponse.Ok(true));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Initiate_CreatesTypedPendingPurchaseFromGatewayResult(bool role)
    {
        var result = await Initiate(role);
        result.StatusCode.Should().Be(StatusCode.OK);
        result.Data!.TransactionId.Should().Be(Tx);
        result.Data.ConfirmationUrl.Should().Be("https://pay.test");
        _repo.Verify(r => r.CreateAsync(It.Is<PurchaseEntity>(p => p.UserId == _user && p.TransactionId == Tx && p.PricePaid == 100m
            && p.Currency == "RUB" && p.Status == PurchaseStatus.Pending && p.PurchaseType == (role ? PurchaseType.Role : PurchaseType.Course)
            && (role ? p is RolePurchaseEntity && ((RolePurchaseEntity)p).RoleId == _target : p is CoursePurchaseEntity && ((CoursePurchaseEntity)p).CourseId == _target))), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _gateway.Verify(g => g.CreatePaymentAsync(100m, "RUB", It.IsAny<string>(), ReturnUrl), Times.Once);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Initiate_ValidationFailure_DoesNotContactGateway(bool role)
    {
        _courses.Setup(c => c.GetCourseForPurchaseAsync(_user, _target)).ReturnsAsync(CreatorResponse.Conflict<CourseEntity>("denied"));
        _roles.Setup(r => r.GetRoleForPurchaseAsync(_user, "course_creator")).ReturnsAsync(CreatorResponse.Conflict<AppRoleEntity>("denied"));
        var result = await Initiate(role);
        result.StatusCode.Should().Be(StatusCode.Conflict);
        result.Description.Should().Be("denied");
        _gateway.VerifyNoOtherCalls();
        _repo.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Initiate_GatewayFailure_DoesNotPersistPurchase(bool role)
    {
        _gateway.Setup(g => g.CreatePaymentAsync(100m, "RUB", It.IsAny<string>(), ReturnUrl)).ReturnsAsync(CreatorResponse.InternalError<PaymentResult>("gateway"));
        (await Initiate(role)).StatusCode.Should().Be(StatusCode.InternalServerError);
        _repo.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("complete")] [InlineData("fail")] [InlineData("refund")]
    public async Task MissingPurchase_ReturnsNotFoundWithoutExternalCalls(string operation)
    {
        var result = operation switch { "complete" => await Sut.CompletePurchaseAsync(Tx), "fail" => await Sut.FailPurchaseAsync(Tx), _ => await Sut.RefundPurchaseAsync(Tx) };
        result.StatusCode.Should().Be(StatusCode.NotFound);
        _gateway.VerifyNoOtherCalls();
        _courses.VerifyNoOtherCalls();
        _roles.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(PurchaseType.Course)] [InlineData(PurchaseType.Role)]
    public async Task Complete_ConfirmsPaymentThenDelegatesToCorrectHandler(PurchaseType type)
    {
        var purchase = Purchase(type);
        var confirmed = false;
        _gateway.Setup(g => g.ConfirmPaymentAsync(Tx)).Callback(() => confirmed = true).ReturnsAsync(CreatorResponse.Ok(true));
        _courses.Setup(c => c.CompleteCoursePurchaseAsync(purchase)).Callback(() => confirmed.Should().BeTrue()).ReturnsAsync(CreatorResponse.Ok(true));
        _roles.Setup(r => r.CompleteRolePurchaseAsync(purchase)).Callback(() => confirmed.Should().BeTrue()).ReturnsAsync(CreatorResponse.Ok(true));
        (await Sut.CompletePurchaseAsync(Tx)).IsSuccess.Should().BeTrue();
        _courses.Verify(c => c.CompleteCoursePurchaseAsync(purchase), type == PurchaseType.Course ? Times.Once : Times.Never);
        _roles.Verify(r => r.CompleteRolePurchaseAsync(purchase), type == PurchaseType.Role ? Times.Once : Times.Never);
    }

    [Theory]
    [InlineData(PurchaseStatus.Completed, StatusCode.OK)]
    [InlineData(PurchaseStatus.Failed, StatusCode.BadRequest)]
    [InlineData(PurchaseStatus.Refunded, StatusCode.BadRequest)]
    public async Task Complete_AlreadyProcessed_DoesNotConfirmOrGrantAccess(PurchaseStatus status, StatusCode expected)
    {
        Purchase(PurchaseType.Course, status);
        (await Sut.CompletePurchaseAsync(Tx)).StatusCode.Should().Be(expected);
        _gateway.VerifyNoOtherCalls(); _courses.VerifyNoOtherCalls(); _roles.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Complete_UnconfirmedPayment_DoesNotGrantAccess()
    {
        Purchase(PurchaseType.Course);
        _gateway.Setup(g => g.ConfirmPaymentAsync(Tx)).ReturnsAsync(CreatorResponse.BadRequest<bool>("unpaid"));
        (await Sut.CompletePurchaseAsync(Tx)).StatusCode.Should().Be(StatusCode.BadRequest);
        _courses.VerifyNoOtherCalls(); _roles.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Fail_PendingPurchase_SavesFailedStatusAndNotifiesGateway()
    {
        var purchase = Purchase(PurchaseType.Course);
        (await Sut.FailPurchaseAsync(Tx, "cancelled")).IsSuccess.Should().BeTrue();
        purchase.Status.Should().Be(PurchaseStatus.Failed);
        _repo.Verify(r => r.Update(purchase), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _gateway.Verify(g => g.FailPaymentAsync(Tx, "cancelled"), Times.Once);
    }

    [Theory]
    [InlineData(PurchaseStatus.Completed)] [InlineData(PurchaseStatus.Failed)] [InlineData(PurchaseStatus.Refunded)]
    public async Task Fail_ProcessedPurchase_IsIdempotent(PurchaseStatus status)
    {
        var purchase = Purchase(PurchaseType.Course, status);
        (await Sut.FailPurchaseAsync(Tx)).IsSuccess.Should().BeTrue();
        purchase.Status.Should().Be(status);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
        _gateway.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(PurchaseType.Course)] [InlineData(PurchaseType.Role)]
    public async Task Refund_RefundsMoneyBeforeRevokingCorrectAccess(PurchaseType type)
    {
        var purchase = Purchase(type, PurchaseStatus.Completed);
        var refunded = false;
        _gateway.Setup(g => g.RefundPaymentAsync(Tx)).Callback(() => refunded = true).ReturnsAsync(CreatorResponse.Ok(true));
        _courses.Setup(c => c.RevokeCourseAccessAsync(purchase)).Callback(() => refunded.Should().BeTrue()).ReturnsAsync(CreatorResponse.Ok(true));
        _roles.Setup(r => r.RevokeRoleAccessAsync(purchase)).Callback(() => refunded.Should().BeTrue()).ReturnsAsync(CreatorResponse.Ok(true));
        (await Sut.RefundPurchaseAsync(Tx)).IsSuccess.Should().BeTrue();
        _courses.Verify(c => c.RevokeCourseAccessAsync(purchase), type == PurchaseType.Course ? Times.Once : Times.Never);
        _roles.Verify(r => r.RevokeRoleAccessAsync(purchase), type == PurchaseType.Role ? Times.Once : Times.Never);
    }

    [Theory]
    [InlineData(PurchaseStatus.Pending)] [InlineData(PurchaseStatus.Failed)] [InlineData(PurchaseStatus.Refunded)]
    public async Task Refund_RequiresCompletedPurchase(PurchaseStatus status)
    {
        Purchase(PurchaseType.Course, status);
        (await Sut.RefundPurchaseAsync(Tx)).StatusCode.Should().Be(StatusCode.BadRequest);
        _gateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Refund_GatewayFailure_DoesNotRevokeAccess()
    {
        Purchase(PurchaseType.Course, PurchaseStatus.Completed);
        _gateway.Setup(g => g.RefundPaymentAsync(Tx)).ReturnsAsync(CreatorResponse.InternalError<bool>("gateway"));
        (await Sut.RefundPurchaseAsync(Tx)).StatusCode.Should().Be(StatusCode.InternalServerError);
        _courses.VerifyNoOtherCalls(); _roles.VerifyNoOtherCalls();
    }
}
