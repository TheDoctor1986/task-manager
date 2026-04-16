namespace TaskManagerApi.Middleware
{
    namespace TaskManagerApi.Middleware
    {
        public class CorrelationIdMiddleware
        {
            private readonly RequestDelegate _next;

            public CorrelationIdMiddleware(RequestDelegate next)
            {
                _next = next;
            }

            public async Task InvokeAsync(HttpContext context)
            {
                var correlationId = Guid.NewGuid().ToString();

                context.Items["CorrelationId"] = correlationId;

                context.Response.Headers.Add("X-Correlation-Id", correlationId);

                await _next(context);
            }
        }
    }
}
