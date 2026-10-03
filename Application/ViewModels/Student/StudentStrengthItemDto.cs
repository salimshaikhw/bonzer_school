using System;

namespace Application.ViewModels.Student
{
    /// <summary>Strength count by class or division (Id = ClassId or DivisionId).</summary>
    public class StudentStrengthItemDto
    {
        public Guid? Id { get; set; }
        public int Count { get; set; }
    }
}
