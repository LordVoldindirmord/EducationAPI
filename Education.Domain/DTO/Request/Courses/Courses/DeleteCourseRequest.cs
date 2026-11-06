namespace Education.Domain.DTO.Request.Courses.Courses
{
    using Education.Domain.Validation;

    public record DeleteCourseRequest
    {
        [NotEmptyGuid(ErrorMessage = "Id курса не должен быть пустым")]
        public Guid CourseId { get; init; }
    }
}
