namespace WebApiDto
{
    public class ApiResponseDto<T> : ApiResponseDto
    {
        public T Data { get; set; }
        public bool Succeeded { get; set; }

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

    public class ApiResponseDto
    {
        public string Message { get; set; }
    }

}