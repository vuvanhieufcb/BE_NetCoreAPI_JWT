namespace BE_2722026_NetCoreAPI
{
    public class MiddleWareCustom
    {
        private readonly RequestDelegate _next;
        public MiddleWareCustom(RequestDelegate next)
        {
            _next = next;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            // Logic before the next middleware
            Console.WriteLine("Before invoking the next middleware.");
            // Call the next middleware in the pipeline
            context.Response.Headers.Add("X-Custom-Header", "This is a custom header added by the middleware.");    
            await _next(context);
            // Logic after the next middleware
            Console.WriteLine("After invoking the next middleware.");
        }
    }
}
