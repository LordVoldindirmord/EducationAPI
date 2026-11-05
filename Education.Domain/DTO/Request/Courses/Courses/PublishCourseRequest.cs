namespace Education.Domain.DTO.Request.Courses.Courses
{
    using Education.Domain.Validation;

    public record PublishCourseRequest
    {
        [NotEmptyGuid(ErrorMessage = "Id курса не должен быть пустым")]
        public Guid CourseId { get; init; }

        public bool IsPublished { get; init; }
    }
}
