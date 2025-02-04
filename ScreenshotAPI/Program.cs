using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using ScreenshotAPI;
using System;
using System.Globalization;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<WebDriverManager>();

builder.Logging.ClearProviders();
builder.Logging.AddFilter("Microsoft", LogLevel.None);
builder.Logging.AddFilter("System", LogLevel.None);
builder.Logging.AddFilter("Default", LogLevel.None);
builder.WebHost.UseUrls("http://0.0.0.0:5000");

var app = builder.Build();

app.MapGet("/screenshot", async (WebDriverManager webDriverManager, HttpContext context) =>
{
    string url = context.Request.Query["url"]!;
    string lang = context.Request.Query["lang"]!;
    string optimizeString = context.Request.Query["optimize"]!;

    
    if (string.IsNullOrEmpty(url))
    {
        return Results.BadRequest("Missing 'url' parameter.");
    }

    if (string.IsNullOrEmpty(lang))
    {
        lang = "en-US";
    }

    var optimize = false;
    if(!string.IsNullOrEmpty(optimizeString))
    {
        optimize = optimizeString == "true";
    }

    try
    {        
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            url = "http://" + url;
        }

        Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture)} Capturing screenshot for URL: {url}");

        var driver = webDriverManager.GetDriver(lang, optimize);
        driver.Navigate().GoToUrl(url);

        var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                       
        return Results.File(new MemoryStream(screenshot.AsByteArray), "image/png");
       
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture)} Error: {url}\n{ex.Message}");
        Console.WriteLine($"{ex.ToString()}");
        Console.WriteLine($"********************************************");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
    finally
    {        
        webDriverManager.DisposeDriver();
    }
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    var webDriverManager = app.Services.GetRequiredService<WebDriverManager>();
    webDriverManager.Dispose();
});

app.Run();
