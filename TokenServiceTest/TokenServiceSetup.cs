using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

public class TokenServiceSetup
{
    public IServiceProvider ServiceProvider { get; private set; }

    public TokenServiceSetup()
    {
        var builder = WebApplication.CreateBuilder();

        // Assuming Startup is your class that configures services  
        var startup = new Startup(builder.Configuration);
        startup.ConfigureServices(builder.Services);
        builder.Services.AddTransient<TokenService.Controllers.TokenServiceController>();

        var app = builder.Build();
        startup.Configure(app);

        // No need to call app.Run() for tests  
        // Just build the ServiceProvider from the app's services  
        ServiceProvider = app.Services;
    }
}
