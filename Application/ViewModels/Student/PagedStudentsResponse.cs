using System;
using System.Collections.Generic;

namespace Application.ViewModels.Student
{
    public class PagedStudentsResponse
    {
        public IReadOnlyList<StudentResponse> Items { get; init; } = Array.Empty<StudentResponse>();
        public int TotalCount { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    }
}
