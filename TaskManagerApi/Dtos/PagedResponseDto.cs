namespace TaskManagerApi.Dtos
{
    public class PagedResponseDto<T>
    {
        public int TotalCount { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }

        public List<T> Data { get; set; } = new();
    }
}
