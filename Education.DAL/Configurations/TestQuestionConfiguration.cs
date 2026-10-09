using System.Text.Json;
using Education.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Education.DAL.Configurations
{
    public class TestQuestionConfiguration : IEntityTypeConfiguration<TestQuestionEntity>
    {
        public void Configure(EntityTypeBuilder<TestQuestionEntity> builder)
        {
            builder.HasKey(tq => tq.Id);

            builder.Property(tq => tq.QuestionText)
                .HasMaxLength(2048);

            builder.Property(tq => tq.Options)
                .HasColumnType("jsonb")
                .HasConversion(
                    tq => ConvertListOptionsToString(tq),   // List<string> -> БД
                    tq => ConvertStringToListOptions(tq))   // БД -> List<string>
                .Metadata.SetValueComparer(GetListComparer<string>());

            builder.Property(tq => tq.CorrectAnswers)
                .HasColumnType("jsonb")
                .HasConversion(
                    tq => ConvertListCorrectAnswerToString(tq), // List<int> -> БД
                    tq => ConvertStringToListCorrectAnswer(tq)) // БД -> List<int>
                .Metadata.SetValueComparer(GetListComparer<int>());

            builder.Property(tq => tq.Explanation)
                .HasMaxLength(1024);

            builder.HasOne(tq => tq.QuizLesson)
                .WithMany(ql => ql.Questions)
                .HasForeignKey(tq => tq.QuizLessonId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasIndex(tq => new { tq.QuizLessonId, tq.OrderNumber })
                .IsUnique();
        }

        private static string ConvertListOptionsToString(List<string> options)
        {
            return JsonSerializer.Serialize(options);
        }

        private static List<string> ConvertStringToListOptions(string options)
        {
            return JsonSerializer.Deserialize<List<string>>(options) ??
                throw new InvalidOperationException(
                "Ошибка десериализации JSON в List<string> для поля Options. " +
                $"Данные в БД повреждены или имеют неверный формат. Значение: '{options}'");
        }

        private static string ConvertListCorrectAnswerToString(List<int> correctAnswer)
        {
            return JsonSerializer.Serialize(correctAnswer);
        }

        private static List<int> ConvertStringToListCorrectAnswer(string correctAnswer)
        {
            return JsonSerializer.Deserialize<List<int>>(correctAnswer) ??
                throw new InvalidOperationException(
                "Ошибка десериализации JSON в List<int> для поля CorrectAnswers. " +
                $"Данные в БД повреждены или имеют неверный формат. Значение: '{correctAnswer}'");
        }

        private static ValueComparer<List<T>> GetListComparer<T>()
        {
            return new ValueComparer<List<T>>(
                (c1, c2) => c1!.SequenceEqual(c2!),                                     // Как сравнивать 2 значения
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v!.GetHashCode())),   // Как вычислять хеш-код
                c => c.ToList()                                                         // Как копировать значение
            );
        }
    }
}