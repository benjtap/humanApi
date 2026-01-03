using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

using Microsoft.Extensions.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using SelfApiproj.Repository;
using SelfApiproj.Services;
using SelfApiproj.settings;

using System.Text;
using Webhttp.Models;
using static System.Net.WebRequestMethods;

namespace ConsoleAppToWebAPI
{
   
    public class Startup
    {
      

        public void ConfigureServices(IServiceCollection services)
        {


            var jwtSettings = new JwtSettings
            {
                SecretKey = "votre-cle-secrete-tres-longue-et-securisee-au-moins-256-bits",
                Issuer = "MonAPI",
                Audience = "MonClient",
                ExpirationMinutes = 60
            };

            services.AddSingleton(jwtSettings);


            //services.AddAuthorization();

            services.AddControllers();

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    policy =>
                    {
                        policy.WithOrigins("http://localhost:8080") // Your frontend origin
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    });
                // You can add other named policies here if needed
                // options.AddPolicy("MySpecificCorsPolicy", builder => { ... });
            });


            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
                    };
                });



           services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

            services.AddScoped<IBaserowService, BaserowService>();
            // Ajouter la configuration
           
           




        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            app.UseHsts(); // Enforces HTTP Strict Transport Security (for production)


            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors(); // <-- Correct usage
            app.UseAuthentication();
            app.UseAuthorization();


          // Redirects HTTP to HTTPS

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers(); // Maps incoming requests to controller actions
            });
        }


      



    }
}






// Services



