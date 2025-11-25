namespace TodoList.API.Models.Todos
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
        public int Page { get; set; }
        public int Limit { get; set; }
        public int Total { get; set; }
    }
}
