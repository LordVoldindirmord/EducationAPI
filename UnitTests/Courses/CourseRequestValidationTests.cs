using System.ComponentModel.DataAnnotations;
using Education.Domain.DTO.Request.Courses.Courses;
using Microsoft.AspNetCore.Http;

namespace UnitTests.Courses
{
    public class CourseRequestValidationTests
    {
        private static List<ValidationResult> Validate(object request)
        {
            var errors = new List<ValidationResult>();
            Validator.TryValidateObject(request, new ValidationContext(request), errors, true);
            return errors;
        }

        [Fact]
        public void Create_RequiresImageAndNonEmptyCategory()
        {
            var request = new CreateCourseRequest { Title = "Основы C#" };
            var errors = Validate(request);
            Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.Image)));
            Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.CategoryId)));

            using var content = new MemoryStream(new byte[] { 1 });
            var valid = request with
            {
                CategoryId = Guid.NewGuid(),
                Image = new FormFile(content, 0, content.Length, "Image", "cover.jpg")
            };
            Assert.Empty(Validate(valid));
            Assert.NotEmpty(Validate(valid with { Price = -1 }));
            Assert.NotEmpty(Validate(valid with { Title = new string('a', 21) }));
        }

        [Fact]
        public void Update_AllowsMissingImage_ButRequiresIdentifiers()
        {
            var request = new UpdateCourseRequest
            {
                CourseId = Guid.NewGuid(), NewCategoryId = Guid.NewGuid(), NewTitle = "Основы C#"
            };
            Assert.Empty(Validate(request));
            Assert.NotEmpty(Validate(request with { CourseId = Guid.Empty }));
            Assert.NotEmpty(Validate(request with { NewCategoryId = Guid.Empty }));
        }

        [Fact]
        public void DeleteAndPublish_RejectEmptyIdentifiers()
        {
            Assert.NotEmpty(Validate(new DeleteCourseRequest()));
            Assert.NotEmpty(Validate(new PublishCourseRequest()));
            Assert.Empty(Validate(new DeleteCourseRequest { CourseId = Guid.NewGuid() }));
            Assert.Empty(Validate(new PublishCourseRequest { CourseId = Guid.NewGuid(), IsPublished = false }));
        }
    }
}
