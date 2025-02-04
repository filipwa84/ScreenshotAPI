using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ScreenshotAPI
{
    public class DisposableChromeDriver : IDisposable
    {
        private bool _disposed = false;
        private ChromeDriverService _driverService;
        private ChromeDriver _driver;
        public ChromeDriver GetDriver(string language = "en-US", bool optimize = false)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DisposableChromeDriver));
            }

            if(_driver != null)
            {
                return _driver;
            }

            var options = new ChromeOptions();
            options.AddArgument("--headless=new");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-setuid-sandbox");
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("--window-size=1920,1080");

            if (optimize)
            {
                options.AddArgument("--single-process");
                options.AddArgument("--no-zygote");
                options.AddArgument("--disable-extensions");
                options.AddArgument("--disable-background-networking");
                options.AddArgument("--disable-features=SitePerProcess");
                options.AddArgument("--disable-background-timer-throttling");
                options.AddArgument("--disk-cache-size=0");
                options.AddArgument("--mute-audio");
                options.AddArgument("--disable-sync");
            }

            if (!string.IsNullOrEmpty(language) && language.Length >= 2)
            {
                options.AddArgument($"--lang={language}");
                options.AddArgument($"--force-lang={language}");
                options.AddArgument($"--accept-lang={language}");
                options.AddUserProfilePreference($"intl.accept_languages", $"{language},{language[..2]};q=0.9");
            }

            _driverService = ChromeDriverService.CreateDefaultService();
            _driverService.SuppressInitialDiagnosticInformation = true;
            _driverService.EnableVerboseLogging = false;
            _driverService.HideCommandPromptWindow = true;
            _driverService.LogPath = "/dev/null";
            _driverService.EnableAppendLog = false;

            _driver = new ChromeDriver(_driverService, options);
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(15);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(15);

            return _driver;
        }

        public async Task<bool> WaitForImagesToLoadAsync(int maxRetries = 5, int delayMs = 1000)
        {
            if (_disposed || _driver == null)
                throw new ObjectDisposedException(nameof(DisposableChromeDriver));

            var js = (IJavaScriptExecutor)_driver;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {                
                js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
                                                
                bool allImagesLoaded = (bool)js.ExecuteScript(@"var images = document.images;
                                                                return images.length > 0 && Array.from(images).every(img => img.complete && img.naturalHeight > 0);");

                Console.WriteLine($"[Attempt {attempt}/{maxRetries}] Images loaded: {allImagesLoaded}");

                if (allImagesLoaded)
                    return true; 

                await Task.Delay(delayMs);
            }

            Console.WriteLine("Warning: Images may not be fully loaded after max retries.");
            
            return false;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _driver?.Quit();
            _driver?.Dispose();
            _driverService?.Dispose();
            
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        ~DisposableChromeDriver()
        {
            Dispose();
        }
    }

}

