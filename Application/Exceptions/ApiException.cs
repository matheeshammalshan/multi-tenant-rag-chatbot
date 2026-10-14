namespace Application.Exceptions
{
    public class ApiException : Exception
    {
        public ApiException(int statusCode, string message) : base(message)
        {
            this.StatusCode = statusCode;
        }

        public int StatusCode { get; }

        public static ApiException NotFound(string resource)
        {
            return new ApiException(404, $"{resource} not found.");
        }
    }
}
