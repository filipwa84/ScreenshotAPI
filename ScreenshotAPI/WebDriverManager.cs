using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ScreenshotAPI
{
    public class WebDriverManager : IDisposable
    {
        private readonly ConcurrentDictionary<int, IWebDriver> _webDrivers = new();
        private bool _disposed = false;

        public IWebDriver GetDriver()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WebDriverManager));
            }

            var threadId = Thread.CurrentThread.ManagedThreadId;

            return _webDrivers.GetOrAdd(threadId, _ =>
            {
                var options = new ChromeOptions();
                options.AddArgument("--headless=new");
                options.AddArgument("--disable-gpu");
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--disable-setuid-sandbox");
                options.AddArgument("--ignore-certificate-errors");
                options.AddArgument("--window-size=1920,1080");

                var driver = new ChromeDriver(options);
                driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(60);

                return driver;
            });
        }

        public void DisposeDriver()
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;

            if (_webDrivers.TryRemove(threadId, out IWebDriver? driver))
            {
                driver.Quit();
                driver.Dispose();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            foreach (var driver in _webDrivers.Values)
            {
                driver.Quit();
                driver.Dispose();
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

