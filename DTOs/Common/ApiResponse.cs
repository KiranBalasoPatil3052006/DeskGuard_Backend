namespace DeskGuardBackend.DTOs.Common
{
    /// <summary>
    /// Standardized API response wrapper matching the frontend's expectations.
    /// The Axios interceptor extracts the outer structure, so fields must be exactly:
    /// - success: boolean
    /// - message: string
    /// - data: T
    /// - errors: object? (validation or detailed errors)
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public object? Errors { get; set; }

        public static ApiResponse<T> Ok(T? data, string message = "Operation successful.")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> Fail(string message, object? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors
            };
        }
    }

    /// <summary>
    /// Overload of ApiResponse for endpoints with no return data.
    /// </summary>
    public class ApiResponse : ApiResponse<object>
    {
        public static ApiResponse Ok(string message = "Operation successful.")
        {
            return new ApiResponse
            {
                Success = true,
                Message = message,
                Data = null
            };
        }

        public new static ApiResponse Fail(string message, object? errors = null)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                Errors = errors
            };
        }
    }

    /// <summary>
    /// Generic container for paginated list responses.
    /// </summary>
    public class PaginatedResult<T>
    {
        public System.Collections.Generic.List<T> Data { get; set; } = new System.Collections.Generic.List<T>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PerPage { get; set; }
        public int TotalPages { get; set; }

        public PaginatedResult() { }

        public PaginatedResult(System.Collections.Generic.List<T> data, int total, int page, int perPage)
        {
            Data = data;
            Total = total;
            Page = page;
            PerPage = perPage;
            TotalPages = perPage > 0 ? (int)System.Math.Ceiling(total / (double)perPage) : 0;
        }
    }
}
