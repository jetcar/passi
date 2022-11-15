namespace WebApiDto
{
    public class ApiResponseDto<T>
    {
        public T Data { get; set; }
        public bool Succeeded { get; set; }
        public string Message { get; set; }

        public static ApiResponseDto<T> Fail(string errorMessage)
        {
            return new ApiResponseDto<T> { Succeeded = false, Message = errorMessage };
        }

        public static ApiResponseDto<T> Success(T data)
        {
            return new ApiResponseDto<T> { Succeeded = true, Data = data };
        }

        public object errors { get; set; }
    }
}