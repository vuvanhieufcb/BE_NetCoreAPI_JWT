using DataAccess.NetCore.IServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace BE_2722026_NetCoreAPI.Filter
{
    public class BE_143026AuthorizeAttribute : TypeFilterAttribute
    {
        public BE_143026AuthorizeAttribute(string functionCode = "DEFAULT", string _permission = "VIEW") : base(typeof(DemoAuthorizeActionFilter))
        {
            Arguments = new object[] { functionCode, _permission };
        }
    }
    public class DemoAuthorizeActionFilter : IAsyncAuthorizationFilter
    {
        private readonly string _functionCode;
        private readonly string _permission;
        private readonly IAcountRepository _acountRepository;
        public DemoAuthorizeActionFilter(string functionCode, string permission,IAcountRepository acountRepository)
        {
            _functionCode = functionCode;
            _permission = permission;
            _acountRepository = acountRepository;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var identity = context.HttpContext.User.Identity as ClaimsIdentity;
            if (identity != null)
            {
                var userClaims = identity.Claims;

                var userID = userClaims.FirstOrDefault(a => a.Type == ClaimTypes.PrimarySid)?.Value != null ? Convert.ToInt32(userClaims.FirstOrDefault(a => a.Type == ClaimTypes.PrimarySid)?.Value) : 0;

                if (userID == 0)
                {
                    context.HttpContext.Response.ContentType = "application/json";
                    context.HttpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
                    context.Result = new JsonResult(new
                    {
                        status = System.Net.HttpStatusCode.Unauthorized,
                        message = "Vui lòng đăng nhập để thực hiện chức năng này!"
                    });
                    return;
                }
                //Kiểm tra trong bảng User_session còn hạn token không?

                //check quyền
                var function = await _acountRepository.GetFunctionByCode(_functionCode);
                if (function == null || function.FunctionID <= 0) {
                    context.HttpContext.Response.ContentType = "application/json";
                    context.HttpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
                    context.Result = new JsonResult(new
                    {
                        status = System.Net.HttpStatusCode.Unauthorized,
                        message = "Chức năng không hợp lệ!"
                    });
                    return;
                }

                var userPermission = await _acountRepository.GetPermissionByUserID(userID, function.FunctionID);
                if (userPermission == null || userPermission.PermissionID <= 0)
                {
                    context.HttpContext.Response.ContentType = "application/json";
                    context.HttpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
                    context.Result = new JsonResult(new
                    {
                        status = System.Net.HttpStatusCode.Unauthorized,
                        message = "Bạn không có quyền truy cập chức năng này!"
                    });
                    return;
                }

                switch (_permission)
                {
                    case "VIEW":
                        {
                            if(userPermission.IsView == 0)
                            {
                                context.HttpContext.Response.ContentType = "application/json";
                                context.HttpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
                                context.Result = new JsonResult(new
                                {
                                    status = System.Net.HttpStatusCode.Unauthorized,
                                    message = "Bạn không có quyền truy cập chức năng này!"
                                });
                                return;
                            }
                        }
                        break;
                    case "INSERT":
                        if (userPermission.IsInsert == 0)
                        {
                            context.HttpContext.Response.ContentType = "application/json";
                            context.HttpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
                            context.Result = new JsonResult(new
                            {
                                status = System.Net.HttpStatusCode.Unauthorized,
                                message = "Bạn không có quyền truy cập chức năng này!"
                            });
                            return;
                        }
                        break;
                }    
            }

            //var isAuthorized = true; // logic kiểm tra quyền truy cập
            //if (!isAuthorized)
            //{
            //    context.Result = new ForbidResult(); // trả về lỗi 403 nếu không có quyền truy cập
            //}
        }
    }
}
