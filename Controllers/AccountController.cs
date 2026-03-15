using DataAccess.NetCore.DO;
using DataAccess.NetCore.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace BE_2722026_NetCoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private IAcountRepository _accountServices;
        private IConfiguration _configuration;
        private readonly IDistributedCache _cache;
        public AccountController(IAcountRepository accountServices, IConfiguration configuration,IDistributedCache cache)
        {
            _accountServices = accountServices;
            _configuration = configuration;
            _cache = cache;
        }
        [HttpPost("AccountLogin")]
        public async Task<ActionResult> AccountLogin(AccountLoginRequestData requestData)
        {
            var returnData = new LoginResponseData();
            try
            {

                // bước 1: Gọi login để lấy thông tin tài khoản
                var result = await _accountServices.CreateAccount(requestData);
                if (result.ReturnCode < 0)
                {
                    return Ok(result);
                }

                //bước 2: Tạo token
                    //bước 2.1: tạo claims để lưu thông tin người dùng vào token
                var user = result.userSuDung;

                var authClaims = new List<Claim> { 
                    new Claim(ClaimTypes.Name, user.UserName) ,
                    new Claim(ClaimTypes.PrimarySid, user.UserID.ToString()) };
                    //bước 2.2: tạo token với các claims đã tạo ở bước 2.1
                var newToken = CreateToken(authClaims);
                //bước 2.3:Tạo refesh token
                var expriredDay = Convert.ToInt32(_configuration["JWT:RefreshTokenValidityInDays"]);
                var refeshTokenExprired = DateTime.Now.AddDays(expriredDay);

                var refeshToken = GenerateRefreshToken();
                var req = new Account_UpdateRefeshTokenRequestData
                {
                    Exprired = refeshTokenExprired,
                    RefeshToken = refeshToken,
                    UserID = user.UserID
                };

                var rs = await _accountServices.Account_UpdateRefeshToken(req);
               
                //Lưu token, IP, vị trí, thiết bị vào Redis caching
                var remoteIPAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                var req_ss = new User_Sessions
                {
                    Token = new JwtSecurityTokenHandler().WriteToken(newToken),
                    CreatedTime = DateTime.Now,
                    DeviceID = requestData.DeviceID,
                    UserId = user.UserID,
                };
                await _accountServices.User_Session_Insert(req_ss);


                var cacheKey = "USER_LOGIN_TOKEN_" + user.UserID + "_" + requestData.DeviceID;
                //Set data vào caching
                var user_Session = new User_Sessions();
                user.UserID = user.UserID;
                user_Session.Token = new JwtSecurityTokenHandler().WriteToken(newToken);
                user_Session.CreatedTime = DateTime.Now;
                user_Session.DeviceID = requestData.DeviceID;

                var dataCacheJson = JsonConvert.SerializeObject(user_Session); //chuyển sang Json
                var datatoCache = Encoding.UTF8.GetBytes(dataCacheJson); //lưu vào cache phải chuyển sang byte
                DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(DateTime.Now.AddMinutes(1))
                    /*.SetSlidingExpiration(TimeSpan.FromMinutes(3))*/; //thời gian sống của cache

                _cache.Set(cacheKey, datatoCache, options);

                //bước 3: Trả về token cho client
                returnData.ReturnCode = result.ReturnCode;
                returnData.ReturnMessage = result.ReturnMessage;
                returnData.token = new JwtSecurityTokenHandler().WriteToken(newToken); //giải mã token ra chuỗi để trả về cho client

                return Ok(returnData);
                
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ReturnData { ReturnCode = -1, ReturnMessage = "Có lỗi xảy ra!!" });
            }
            
        }

        [HttpPost("RefeshToken")]
        public async Task<IActionResult> RefeshToken(TokenModel tokenModel)
        {
           
            var responseData = new UserLoginResponseData();
            try
            {
                if(tokenModel == null || string.IsNullOrEmpty(tokenModel.RefeshToken) || string.IsNullOrEmpty(tokenModel.AccessToken))
                {
                    responseData.ReturnCode = -1;
                    responseData.ReturnMessage = "Không có thông tin về tài khoản! Vui lòng kiểm tra lại tài khoản và mật khẩu";
                    return Ok(responseData);
                }

                //Bước 1: giải mã token truyền lên để lấy claims
                var principal = GetPrincipalFromExpiredToken(tokenModel.AccessToken);
                if(principal == null)
                {
                    responseData.ReturnCode = -1;
                    responseData.ReturnMessage = "Token không hợp lệ!!";
                    return Ok(responseData);
                }

                //Bước 2: Check refeshToken và ngày hết hạn
                var exp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(principal.FindFirst("exp").Value));
                //or as Datetime
                DateTime result = exp.UtcDateTime.AddHours(7);

                string userName = principal.Identity.Name;

                //Gọi db để lấy theo userName
                var user = await _accountServices.GetUser_ByUsername(userName);

                //Ngày hết hạn < thời gian hiện tại
                //refeshToken truyền lên khác với refeshToken trong db => sai token
                if(user == null || user.RefreshToken != tokenModel.RefeshToken || user.Expired <= DateTime.Now)
                {
                    responseData.ReturnCode = -1;
                    responseData.ReturnMessage = "Token không hợp lệ!!";
                    return Ok(responseData);
                }

                var newToken = CreateToken(principal.Claims.ToList());
                var newRefeshToken = GenerateRefreshToken();

                //Bước 3: Tạo token  mới và refeshToken mới
                //Lưuu refeshToken 
                _ = int.TryParse(_configuration["JWT:RefreshTokenValidityInDays"], out int refreshTokenValidityInDays);

                await _accountServices.User_UpdateRefeshToken(user.UserID, newRefeshToken, DateTime.Now.AddDays(refreshTokenValidityInDays));

                responseData.ReturnCode = 1;
                responseData.ReturnMessage = "Đăng nhập thành công!";
                responseData.token = new JwtSecurityTokenHandler().WriteToken(newToken);
                responseData.refreshToken = newRefeshToken;
                return Ok(responseData);
            }
            catch (Exception ex)
            {

            }
            return Ok();
        }

        [HttpPost("LogOut_2")]
        public async Task<IActionResult> LogOut_2(AccountLogoutRequestData requestData)
        {
            try
            {
                var rs = await _accountServices.Account_LogOut(requestData.Token);
                return Ok(rs);
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        [HttpPost("Logout")]
        public async Task<IActionResult> LogOut(TokenLogOutModel tokenModelLogOut)
        {
            var responseData = new LogoutResponseData();
            try
            {
                if(tokenModelLogOut == null || string.IsNullOrEmpty(tokenModelLogOut.AccessToken) || string.IsNullOrEmpty(tokenModelLogOut.DeviceID))
                {
                    responseData.ReturnCode = -1;
                    responseData.ReturnMessage = "Không có thông tin về tài khoản! Vui lòng kiểm tra lại tài khoản và mật khẩu";
                    return Ok(responseData);
                }
                //thực hiện xoá token trong cache
                
                //Bước 1: giải mã token truyền lên để lấy claims
                var principal = GetPrincipalFromExpiredToken(tokenModelLogOut.AccessToken);
                if (principal == null)
                {
                    responseData.ReturnCode = -1;
                    responseData.ReturnMessage = "Token không hợp lệ!!";
                    return Ok(responseData);
                }

                //Bước 2: Check refeshToken và ngày hết hạn
                var exp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(principal.FindFirst("exp").Value));
                //or as Datetime
                DateTime result = exp.UtcDateTime.AddHours(7);

                string userName = principal.Identity.Name;

                var user = await _accountServices.GetUser_ByUsername(userName);

                if (user == null)
                {
                    responseData.ReturnCode = -1;
                    responseData.ReturnMessage = "Không lấy được thông tin token không hợp lệ!!";
                    return Ok(responseData);
                }
                //Lấy dữ liệu từ redis = keyCache
                var cacheKey = "USER_LOGIN_TOKEN_" + user.UserID + "_" +tokenModelLogOut.DeviceID;

                //thực hiện xoá token trong cache của thiết bị này trong redis caching
                _cache.Remove(cacheKey);

                responseData.ReturnCode = 1;
                responseData.ReturnMessage = "Đăng xuất thành công!";
                return Ok(responseData);
             
            }
            catch (Exception ex)
            {

            }
            return Ok();
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
        private JwtSecurityToken CreateToken(List<Claim> authClaims)
        {
            //claims là một đối tượng lưu trữ thông tin người dùng, có thể lưu tên, id, email,... của người dùng
            //key là gi
            //thời gian sống bao lâu
            //thuật toán sử dụng

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"])); 
            _ = int.TryParse(_configuration["JWT:TokenValidityInMinutes"], out int tokenValidityInMinutes);

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddMinutes(tokenValidityInMinutes),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256) //thuật toán mã hoá 
                );
            return token;
        }
        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"])),
                ValidateLifetime = false
            };
            var tokenHander = new JwtSecurityTokenHandler();
            var principal = tokenHander.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if(securityToken is not JwtSecurityToken jwtSecurityToken /*|| !jwtSecurityToken.Header.Alg.Equals(SecurityToken.)*/)
            {
                throw new SecurityTokenException("Invalid token");
            }
            return principal;
        }
    }
}
