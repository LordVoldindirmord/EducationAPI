namespace Education.Domain.DTO.Response.Courses.Progress
{
    public record QuizResultResponse
    {
        public int Score { get; init; }
        public bool IsPassed { get; init; }
        public int PassingScore { get; init; }
    }
}
