using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TokenService.Config;
using TokenService.Service;
using Microsoft.OpenApi.Models;
using TokenService.AuthFilter;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        //services.AddSwaggerGen();
        services.AddSwaggerGen(c => {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
            c.AddSecurityDefinition("S2SAuth", new OpenApiSecurityScheme {
                Description = "S2S Authorization header. Usage: \"Authorization: {s2sKey}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "S2SAuth"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement() {{
                new OpenApiSecurityScheme {
                    Reference = new OpenApiReference {
                        Type = ReferenceType.SecurityScheme,
                        Id = "S2SAuth"
                    },
                    Scheme = "S2SAuth",
                    Name = "S2SAuth",
                    In = ParameterLocation.Header,
                },
                new List<string>()
            }});

            c.OperationFilter<S2SAuthFilter>();
        });

        var tokenServiceConfig = Configuration.GetSection("TokenServiceConfig").Get<TokenServiceConfig>();

        services.AddSingleton(tokenServiceConfig);
        services.AddSingleton<TokenProvider, TokenProvider>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        services.AddCors(options => {
            options.AddPolicy("AllowAll", builder => {
                builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });
        });
    }

    public void Configure(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowAll");
        app.UseAuthorization();
        app.MapControllers();
    }
}
