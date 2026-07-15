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
}
