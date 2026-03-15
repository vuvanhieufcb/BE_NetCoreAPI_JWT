using System.Security.Cryptography.X509Certificates;

namespace BE_2722026_NetCoreAPI
{
    public static class MyMiddlewareExtensions
    {
        public static IApplicationBuilder UseMyMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MiddleWareCustom>();
        }
    }
}
