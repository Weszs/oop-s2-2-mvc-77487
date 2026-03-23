namespace oop_s2_2_mvc_77487.Middleware
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "text/html";

                var errorId = Guid.NewGuid().ToString();
                var response = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>An Error Occurred</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; background-color: #f8f9fa; margin: 0; padding: 20px; }}
                        .container {{ max-width: 600px; margin: 50px auto; background: white; padding: 30px; border-radius: 5px; box-shadow: 0 0 10px rgba(0,0,0,0.1); }}
                        h1 {{ color: #dc3545; }}
                        p {{ color: #666; line-height: 1.6; }}
                        .error-id {{ background: #f8f9fa; padding: 10px; border-radius: 3px; font-family: monospace; margin: 20px 0; }}
                        a {{ color: #007bff; text-decoration: none; }}
                        a:hover {{ text-decoration: underline; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h1>Oops! Something went wrong</h1>
                        <p>We're sorry, but something went wrong while processing your request.</p>
                        <p>Our team has been notified of the issue. Please try again later or contact support if the problem persists.</p>
                        <div class='error-id'>
                            <strong>Error ID:</strong> {errorId}
                        </div>
                        <p><a href='/'>Return to Home</a></p>
                    </div>
                </body>
                </html>";

                await context.Response.WriteAsync(response);
            }
        }
    }
}
