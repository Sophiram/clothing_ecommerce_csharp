namespace ClothingEcommerce.Shared.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
        public int StatusCode { get; set; } = 200;

        public static ApiResponse<T> Ok(T? data, string? message = null) => new()
        {
            Success = true,
            Message = message,
            Data = data,
            StatusCode = 200
        };

        public static ApiResponse<T> Created(T? data, string? message = null) => new()
        {
            Success = true,
            Message = message,
            Data = data,
            StatusCode = 201
        };

        public static ApiResponse<T> Fail(string error, int statusCode = 400) => new()
        {
            Success = false,
            Message = error,
            Errors = new List<string> { error },
            StatusCode = statusCode
        };

        public static ApiResponse<T> Fail(List<string> errors, string? message = null, int statusCode = 400) => new()
        {
            Success = false,
            Message = message ?? "One or more validation errors occurred.",
            Errors = errors,
            StatusCode = statusCode
        };
    }

    public class ApiResponse : ApiResponse<object>
    {
        public static ApiResponse Ok(string? message = null) => new()
        {
            Success = true,
            Message = message,
            StatusCode = 200
        };

        public static new ApiResponse Fail(string error, int statusCode = 400) => new()
        {
            Success = false,
            Message = error,
            Errors = new List<string> { error },
            StatusCode = statusCode
        };
    }
}
