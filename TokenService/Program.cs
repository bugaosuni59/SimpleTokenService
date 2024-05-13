var builder = WebApplication.CreateBuilder(args);
var startup = new Startup(builder.Configuration);

// Configure Services  
startup.ConfigureServices(builder.Services);

var app = builder.Build();

// Configure Middleware  
startup.Configure(app);

app.Run();
