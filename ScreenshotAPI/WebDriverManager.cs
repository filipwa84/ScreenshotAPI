using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ScreenshotAPI
{
    public class WebDriverManager : IDisposable
    {
        private readonly ConcurrentDictionary<int, (IWebDriver Driver, ChromeDriverService Service)> _webDrivers = new();
        private bool _disposed = false;

        public IWebDriver GetDriver(string language = "en-US")
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WebDriverManager));
            }

            var threadId = Thread.CurrentThread.ManagedThreadId;

            var result = _webDrivers.GetOrAdd(threadId, _ =>
            {
                var options = new ChromeOptions();
                options.AddArgument("--headless=new");
                options.AddArgument("--disable-gpu");
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--disable-setuid-sandbox");
                options.AddArgument("--ignore-certificate-errors");
                options.AddArgument("--window-size=1920,1080");

                if (!string.IsNullOrEmpty(language) && language.Length >= 2)
                {
                    options.AddArgument($"--lang={language}");
                    options.AddArgument($"--force-lang={language}");
                    options.AddArgument($"--accept-lang={language}");
                    options.AddUserProfilePreference($"intl.accept_languages", $"{language},{language[..2]};q=0.9");                    
                }

                var driverService = ChromeDriverService.CreateDefaultService();
                driverService.SuppressInitialDiagnosticInformation = true;
                driverService.EnableVerboseLogging = false;
                driverService.HideCommandPromptWindow = true;
                driverService.LogPath = "/dev/null";
                driverService.EnableAppendLog = false;

                var driver = new ChromeDriver(driverService, options);
                driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(15);
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(15);

                return (driver, driverService);
            });

            return result.Driver;
        }

        public void DisposeDriver()
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;

            if (_webDrivers.TryRemove(threadId, out var driverTuple))
            {                
                driverTuple.Driver.Quit();
                driverTuple.Driver.Dispose();
                driverTuple.Service.Dispose();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            foreach (var (_, (driver, service)) in _webDrivers)
            {
                driver.Quit();
                driver.Dispose();
                service.Dispose();
            }

            _webDrivers.Clear();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        ~WebDriverManager()
        {
            Dispose();
        }
    }

}

