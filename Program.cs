using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Timepro.Timesheet.Commands.Revision;
using Timepro.Timesheet.Shared;
using Timepro.Timesheet.Shared.Infra;

IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.local.json", true)
    .Build();
var settings = configuration.GetSection("ApiSettings").Get<ApiConfig>();

IServiceCollection registrations = new ServiceCollection()
    .AddLogging()
    .AddSingleton(settings);

registrations.AddHttpClient("api", client =>
    {
        // Set the base address and default headers for the client parameter
        client.BaseAddress = new Uri(settings.BaseUrl);
        client.DefaultRequestHeaders.Add("Authorization", $"Basic {settings.ApiKey}");
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();

        // Create a new CookieContainer
        var cookieContainer = new CookieContainer();

        // Add a cookie to the container
        cookieContainer.Add(new Uri(settings.BaseUrl),
            new Cookie(settings.CookieName, settings.CookieValue));

        // Assign the cookie container to the handler
        handler.CookieContainer = cookieContainer;

        return handler;
    });

CommandApp app = new(new TypeRegistrar(registrations));
app.Configure(config =>
{
    config.SetApplicationName("TimePro.Cli");
    // Register the Conversion command
    config.AddCommand<RevisionCommand>("fix");
});

app.Run(args); // Build configuration