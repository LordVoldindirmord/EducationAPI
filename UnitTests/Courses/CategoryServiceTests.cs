using Education.DAL.Interfaces;
using Education.Domain.DTO.Request.Courses.Categories;
using Education.Service.Common.Interfaces;
using Education.Service.Courses.Implementations;

namespace UnitTests.Courses;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _repo = new();
    private readonly Mock<IPermissionService> _permissions = new();
    private readonly Guid _user = Guid.NewGuid(), _id = Guid.NewGuid();
    private CategoryService Sut => new(_repo.Object, _permissions.Object);
    public CategoryServiceTests()
    {
        _permissions.Setup(p => p.IsActiveAsync(_user)).ReturnsAsync(true);
        _permissions.Setup(p => p.HasAnyRoleAsync(_user, It.IsAny<IEnumerable<UserRole>>())).ReturnsAsync(true);
    }

    [Theory]
    [InlineData("create", false)] [InlineData("update", false)] [InlineData("delete", false)]
    [InlineData("create", true)] [InlineData("update", true)] [InlineData("delete", true)]
    public async Task Mutations_RequireActiveAccountAndAllowedRole(string operation, bool active)
    {
        _permissions.Setup(p => p.IsActiveAsync(_user)).ReturnsAsync(active);
        _permissions.Setup(p => p.HasAnyRoleAsync(_user, It.IsAny<IEnumerable<UserRole>>())).ReturnsAsync(false);
        var result = operation switch
        {
            "create" => await Sut.CreateCategoryAsync(new() { Name = "Category" }, _user),
            "update" => await Sut.UpdateCategoryAsync(new() { CategoryId = _id, NewName = "Changed" }, _user),
            _ => await Sut.DeleteCategoryAsync(new() { CategoryId = _id }, _user),
        };
        result.StatusCode.Should().Be(StatusCode.Forbidden);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_PersistsMappedCategoryWithAdminOrModeratorPermission()
    {
        (await Sut.CreateCategoryAsync(new() { Name = "Category", Description = "Description" }, _user)).StatusCode.Should().Be(StatusCode.Created);
        _repo.Verify(r => r.CreateAsync(It.Is<CategoryEntity>(c => c.Name == "Category" && c.Description == "Description")), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _permissions.Verify(p => p.HasAnyRoleAsync(_user, It.Is<IEnumerable<UserRole>>(roles => roles.SequenceEqual(new[] { UserRole.Admin, UserRole.Moderator }))), Times.Once);
    }

    [Fact]
    public async Task Create_DuplicateName_DoesNotWrite()
    {
        _repo.Setup(r => r.ExistByName("Category")).ReturnsAsync(true);
        (await Sut.CreateCategoryAsync(new() { Name = "Category" }, _user)).StatusCode.Should().Be(StatusCode.Conflict);
        _repo.Verify(r => r.CreateAsync(It.IsAny<CategoryEntity>()), Times.Never);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData("update")] [InlineData("delete")]
    public async Task MissingCategory_ReturnsNotFoundWithoutSave(string operation)
    {
        var result = operation == "update"
            ? await Sut.UpdateCategoryAsync(new() { CategoryId = _id, NewName = "New" }, _user)
            : await Sut.DeleteCategoryAsync(new() { CategoryId = _id }, _user);
        result.StatusCode.Should().Be(StatusCode.NotFound);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task Delete_RequiresEmptyCategory(bool hasCourses)
    {
        var category = new CategoryEntity { Id = _id, Name = "Category" };
        _repo.Setup(r => r.GetByIdAsync(_id)).ReturnsAsync(category);
        _repo.Setup(r => r.HasCoursesAsync(_id)).ReturnsAsync(hasCourses);
        (await Sut.DeleteCategoryAsync(new() { CategoryId = _id }, _user)).StatusCode.Should().Be(hasCourses ? StatusCode.Conflict : StatusCode.OK);
        _repo.Verify(r => r.Delete(category), hasCourses ? Times.Never : Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), hasCourses ? Times.Never : Times.Once);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Update_ChangesFieldsUnlessNewNameIsTaken(bool duplicate)
    {
        var category = new CategoryEntity { Id = _id, Name = "Old", Description = "Old description" };
        _repo.Setup(r => r.GetByIdAsync(_id)).ReturnsAsync(category);
        _repo.Setup(r => r.ExistByName("New")).ReturnsAsync(duplicate);
        (await Sut.UpdateCategoryAsync(new() { CategoryId = _id, NewName = "New", NewDescription = "New description" }, _user)).StatusCode
            .Should().Be(duplicate ? StatusCode.Conflict : StatusCode.OK);
        category.Name.Should().Be(duplicate ? "Old" : "New");
        category.Description.Should().Be(duplicate ? "Old description" : "New description");
        _repo.Verify(r => r.SaveChangesAsync(), duplicate ? Times.Never : Times.Once);
    }

    [Fact]
    public async Task Update_Unchanged_DoesNotSaveOrCheckOwnNameForDuplicates()
    {
        _repo.Setup(r => r.GetByIdAsync(_id)).ReturnsAsync(new CategoryEntity { Id = _id, Name = "Same" });
        (await Sut.UpdateCategoryAsync(new() { CategoryId = _id, NewName = "Same" }, _user)).IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
        _repo.Verify(r => r.ExistByName(It.IsAny<string>()), Times.Never);
    }
}
