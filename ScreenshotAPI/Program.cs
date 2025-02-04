using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5000");
var app = builder.Build();

app.MapGet("/screenshot", async (HttpContext context) =>
{
    string url = context.Request.Query["url"];
    
    if (string.IsNullOrEmpty(url))
    {
        return Results.BadRequest("Missing 'url' parameter.");
    }

    try
    {
        // Ensure URL includes a scheme
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            url = "http://" + url;
        }

        Console.WriteLine($"Capturing screenshot for URL: {url}");

        var options = new ChromeOptions();
        options.AddArgument("--headless");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-setuid-sandbox");
        options.AddArgument("--ignore-certificate-errors");
        options.AddArgument("--window-size=1920,1080");

        var driverService = ChromeDriverService.CreateDefaultService();
        driverService.SuppressInitialDiagnosticInformation = true;
        driverService.HideCommandPromptWindow = true;
        driverService.EnableVerboseLogging = false;

        using (var driver = new ChromeDriver(driverService, options))
        {
            driver.Navigate().GoToUrl(url);

            // Take screenshot into memory
            Screenshot screenshot = ((ITakesScreenshot)driver).GetScreenshot();
            byte[] imageBytes = screenshot.AsByteArray;

            // Return the image directly from memory
            return Results.File(new MemoryStream(imageBytes), "image/png");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        return Results.Problem(detail: ex.ToString(), statusCode: 500);
    }

});

app.Run();
