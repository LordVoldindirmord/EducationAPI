using Education.DAL.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace UnitTests.Helpers;

public sealed class MockTransactionContext
{
    public Mock<EducationDbContext> Context { get; } = new(new DbContextOptionsBuilder<EducationDbContext>().Options);
    public Mock<IDbContextTransaction> Transaction { get; } = new();
    public Mock<DatabaseFacade> Database { get; }
    public Mock<DbSet<PurchaseEntity>> Purchases { get; } = new();

    public MockTransactionContext()
    {
        Database = new Mock<DatabaseFacade>(Context.Object);
        Context.SetupGet(c => c.Database).Returns(Database.Object);
        Context.Object.Purchase = Purchases.Object;
        Database.Setup(d => d.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Transaction.Object);
        Context.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    public void Committed()
    {
        Transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        Transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    public void RolledBack()
    {
        Transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        Transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
