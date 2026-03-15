using DataAccess.NetCore.DbContext;
using DataAccess.NetCore.DO;
using DataAccess.NetCore.IRepository;
using DataAccess.NetCore.IServices;
using DataAccess.NetCore.Services;
using DataAccess.NetCore.UnitOfWork;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace BE_2722026_NetCoreAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration;

            builder.Services.AddDbContext<BE_2722026DbContext>(options =>
               options.UseSqlServer(configuration.GetConnectionString("ConnStr")));

            //jwt
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:ValidIssuer"],
                        ValidAudience = builder.Configuration["Jwt:ValidAudience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
                    };
                });

            // Add services to the container.
            builder.Services.AddAuthorization();

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration["RedisCacheUrl"];
            });

            builder.Services.AddControllers();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddScoped<IAcountRepository, AccountServices>();
            builder.Services.AddScoped<IRoomRepository, RoomRepository>();
            
            builder.Services.AddScoped<IRoomGenericRepository, RoomGenericRepository>();
            builder.Services.AddScoped<IHotelGenericRepository, HotelGenericRepository>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IGenericRepository<Rooms>, GenericRepository<Rooms>>();
          


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseMiddleware<MiddleWareCustom>();

            app.UseAuthentication();

            app.UseAuthorization();


            app.MapControllers();

            var summaries = new[]
            {
                "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
            };

            app.MapGet("/weatherforecast", (HttpContext httpContext) =>
            {
                var forecast = Enumerable.Range(1, 5).Select(index =>
                    new WeatherForecast
                    {
                        Date = DateTime.Now.AddDays(index),
                        TemperatureC = Random.Shared.Next(-20, 55),
                        Summary = summaries[Random.Shared.Next(summaries.Length)]
                    })
                    .ToArray();
                return forecast;
            })
            .WithName("GetWeatherForecast");

            app.Run();
        }
    }
}
