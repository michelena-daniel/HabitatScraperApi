using HabitatScraperApi.Models.Entities;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using OpenQA.Selenium;
using Serilog;
using System.Diagnostics;
using System.Globalization;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using HabitatScraper.Utils.Helpers;
using HabitatScraperApi.Models.DTO;

namespace HabitatScraperApi.Services
{
    public interface IScrapService
    {
        void FotoCasaScrape();
        //void IdealistaScrape();
    }

    public class ScrapService : IScrapService
    {
        public ScrapService() { }

        public async void FotoCasaScrape()
        {
            #region Setup
            //SETUP
            Console.WriteLine("--- Waking up. ---");
            var userAgents = GetUserAgents();

            var random = new Random();

            string userAgent = userAgents[random.Next(userAgents.Count)];
            Console.WriteLine($"Using agent: {userAgent} ");

            // Setup ChromeDriver using WebDriverManager for automatic driver management
            new DriverManager().SetUpDriver(new ChromeConfig());

            var chromeOptions = new ChromeOptions();
            int width = random.Next(320, 1920);
            int height = random.Next(480, 1080);

            chromeOptions.AddAdditionalOption("useAutomationExtension", false);
            chromeOptions.AddArgument($"window-size=1920:1080");
            chromeOptions.AddArgument("--incognito");
            chromeOptions.AddExcludedArgument("enable-automation");
            chromeOptions.AddArgument("--disable-blink-features=AutomationControlled");
            chromeOptions.AddArgument($"user-agent={userAgent}");
            //chromeOptions.AddArgument($"--headless");


            //LOADING DRIVER
            Console.WriteLine("Initializing driver");
            using var driver = new ChromeDriver(chromeOptions);
            driver.Manage().Cookies.DeleteAllCookies();
            driver.Manage().Window.Maximize();
            var devTools = driver.ExecuteCdpCommand("Page.addScriptToEvaluateOnNewDocument", new Dictionary<string, object>
            {
                { "source", @"
                    Object.defineProperty(navigator, 'webdriver', {
                        get: () => undefined,
                        configurable: true
                    });
                    Object.defineProperty(navigator, 'languages', {get: () => ['en-US', 'en']});
                    Object.defineProperty(navigator, 'plugins', {get: () => [1, 2, 3, 4, 5]});
                    // Removed 'platform' modification to prevent potential conflicts
                " }
            });

            ((IJavaScriptExecutor)driver).ExecuteScript(
                @"
                Object.defineProperty(navigator, 'permissions', {
                    get: () => ({
                        query: () => Promise.resolve({ state: 'granted' })
                    })
                });
                "
            );

            ((IJavaScriptExecutor)driver).ExecuteScript(
                @"
                const getParameter = WebGLRenderingContext.prototype.getParameter;
                WebGLRenderingContext.prototype.getParameter = function(parameter) {
                    if (parameter === 37445) {
                        return 'Intel Inc.';
                    }
                    if (parameter === 37446) {
                        return 'Intel Iris OpenGL Engine';
                    }
                    return getParameter(parameter);
                };
                "
            );

            string url = "https://www.fotocasa.es/es/alquiler/viviendas/a-coruna-capital/todas-las-zonas/l";
            Console.WriteLine($"-- Navigating to: {url}");
            driver.Navigate().GoToUrl(url);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Console.WriteLine("-- Waiting for elements to be visible --");
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));

            // HANDLE CONSENT POP-UP
            try
            {
                await Task.Delay(2000);
                Console.WriteLine("Attempting to handle the consent pop-up...");
                By acceptButtonSelector = By.Id("didomi-notice-agree-button");
                wait.Until(ExpectedConditions.ElementToBeClickable(acceptButtonSelector));
                // Find and click the accept button
                var acceptButton = driver.FindElement(acceptButtonSelector);
                acceptButton.Click();
                Console.WriteLine("Consent pop-up handled successfully.");
                await Task.Delay(3000);

                Console.Write("Closing map");
                By mapSelector = By.CssSelector("button[aria-label='Cerrar mapa']");
                wait.Until(ExpectedConditions.ElementToBeClickable(mapSelector));
                var mapButtonClose = driver.FindElement(mapSelector);
                mapButtonClose.Click();
                Console.WriteLine("Map closed.");
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine("Consent pop-up not found or took too long to appear. Proceeding without handling it.");
            }
            catch (NoSuchElementException)
            {
                Console.WriteLine("Consent pop-up elements not found. Proceeding without handling it.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error while handling consent pop-up: {ex.Message}");
            }
            #endregion

            #region DataExtraction
            //DATA EXTRACTION
            Console.WriteLine("--Starting data extraction--");
            Stopwatch executionStopwatch = new Stopwatch();
            executionStopwatch.Start();
            bool hasNextPage = true;
            int currentPage = 1;
            while (hasNextPage)
            {
                var anuncios = new List<Anuncio>();
                Console.WriteLine("-- Starting slow scroll to load all content --");
                await ScrollToBottomSlowlyAsync(driver, scrollPauseTime: 500, scrollIncrement: 300);
                Console.WriteLine($"-- Finished scrolling page: {currentPage} --");

                //wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("div.re-CardPackPremium-info")));
                //wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(".re-CardPackPremium")));
                //wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(".re-CardPackAdvance")));
                //wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(".re-CardPackBasic")));
                wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("span.re-CardPrice")));
                stopwatch.Stop();
                Console.WriteLine($"-- Elements visible after {stopwatch.Elapsed} on page: {currentPage}");

                await Task.Delay(10000);
                Console.WriteLine($"-- Processing page {currentPage} --");
                //Find elements
                var cardPacksPremium = driver.FindElements(By.CssSelector(".re-CardPackPremium"));
                if (cardPacksPremium.Count > 0)
                {
                    foreach (var cardpack in cardPacksPremium)
                    {
                        try
                        {
                            var anuncio = ScanElement(cardpack, ".re-CardPackPremium");
                            anuncios.Add(anuncio);
                            Console.WriteLine($"Anuncio added {anuncio.Title}");
                            await Task.Delay(random.Next(1000, 4000));
                        }
                        catch (NoSuchElementException nse)
                        {
                            Console.WriteLine("Element not found: " + nse.Message);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error fetching listing: " + ex.Message);
                        }

                    }
                }

                var cardPacksAdvanced = driver.FindElements(By.CssSelector(".re-CardPackAdvance"));
                if (cardPacksAdvanced.Count > 0)
                {
                    foreach (var cardPackAdvanced in cardPacksAdvanced)
                    {
                        try
                        {
                            var anuncio = ScanElement(cardPackAdvanced, ".re-CardPackAdvance");
                            anuncios.Add(anuncio);
                            Console.WriteLine($"Anuncio added {anuncio.Title}");
                            await Task.Delay(random.Next(1000, 4000));
                        }
                        catch (NoSuchElementException nse)
                        {
                            Console.WriteLine("Element not found: " + nse.Message);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error fetching listing: " + ex.Message);
                        }
                    }
                }

                var cardPacksBasic = driver.FindElements(By.CssSelector(".re-CardPackBasic"));
                if (cardPacksBasic.Count > 0)
                {
                    foreach (var cardPackBasic in cardPacksBasic)
                    {
                        try
                        {
                            var anuncio = ScanElement(cardPackBasic, ".re-CardPackBasic");
                            anuncios.Add(anuncio);
                            Console.WriteLine($"Anuncio added {anuncio.Title}");
                            await Task.Delay(random.Next(1000, 4000));
                        }
                        catch (NoSuchElementException nse)
                        {
                            Console.WriteLine("Element not found: " + nse.Message);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error fetching listing: " + ex.Message);
                        }
                    }
                }

                var cardPacksMinimal = driver.FindElements(By.CssSelector(".re-CardPackMinimal"));
                if (cardPacksMinimal.Count > 0)
                {
                    foreach (var cardPackMinimal in cardPacksMinimal)
                    {
                        try
                        {
                            var anuncio = ScanElement(cardPackMinimal, ".re-CardPackMinimal");
                            anuncios.Add(anuncio);
                            Console.WriteLine($"Anuncio added {anuncio.Title}");
                            await Task.Delay(random.Next(1000, 4000));
                        }
                        catch (NoSuchElementException nse)
                        {
                            Console.WriteLine("Element not found: " + nse.Message);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error fetching listing: " + ex.Message);
                        }
                    }
                }
                Console.WriteLine("-- Driver Operation Finished --");

                #region Write
                //SAVE DATA
                Console.WriteLine("Writing data");
                foreach (var n in anuncios)
                {
                    Console.WriteLine($"Title to write: {n.Title}");
                }
                string fileName = $"Listings_{DateTime.Now:yyyy-MM-dd}_page_{currentPage}.csv";

                try
                {
                    using (var writer = new StreamWriter(fileName))
                    using (var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csv.WriteRecords(anuncios);
                        csv.WriteComment($"Extracted from : {url.ToString()}");
                        csv.WriteComment($"Extracted on  {DateTime.Now.ToString()}");
                        csv.WriteComment($"Items:  {anuncios.Count}");
                    }
                    Log.Information($"Data successfully written to CSV. Count: {anuncios.Count}");
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine($"IO Error while writing to file: {ioEx.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error: {ex.Message}");
                }

                Console.WriteLine("Scraper completed successfully.");
                #endregion


                By nextPageSelector = By.CssSelector($"a.sui-AtomButton[href='/es/alquiler/viviendas/a-coruna-capital/todas-las-zonas/l/{currentPage + 1}']");
                Console.WriteLine($"Next page count: {driver.FindElements(nextPageSelector).Count}");
                if (driver.FindElements(nextPageSelector).Count > 0)
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript($"window.scrollBy(0, -3500);");
                    wait.Until(ExpectedConditions.ElementToBeClickable(nextPageSelector));
                    currentPage++;
                    await Task.Delay(2000);
                    Console.WriteLine("Attempting to click next page...");
                    // Find and click the accept button
                    var nextPageButton = driver.FindElement(nextPageSelector);
                    nextPageButton.Click();
                    await Task.Delay(9000);
                }
                else
                {
                    Console.WriteLine("No next page found");
                    hasNextPage = false;
                    executionStopwatch.Stop();
                    Console.WriteLine($"All pages scanned. Time elapsed: {executionStopwatch}");
                    //Call CSV
                    driver.Quit();
                    for (int i = 3; i >= 0; i--)
                    {
                        Console.WriteLine($"Shutting down in: {i}");
                        await Task.Delay(3000);
                    }
                }
            }
            #endregion
        }

        static List<string> GetUserAgents()
        {
            return
                [
                    // Chrome on Windows
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.102 Safari/537.36",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.85 Safari/537.36",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/88.0.4324.190 Safari/537.36",

                    //// Firefox on Windows
                    //"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:88.0) Gecko/20100101 Firefox/88.0",
                    //"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:86.0) Gecko/20100101 Firefox/86.0",
                    //"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:84.0) Gecko/20100101 Firefox/84.0",

                    //// Edge on Windows
                    //"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.83 Safari/537.36 Edg/85.0.564.41",
                    //"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.135 Safari/537.36 Edg/84.0.522.63",

                    //// Safari on iOS (iPhone)
                    //"Mozilla/5.0 (iPhone; CPU iPhone OS 15_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.0 Mobile/15E148 Safari/604.1",
                    //"Mozilla/5.0 (iPhone; CPU iPhone OS 13_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0 Mobile/15E148 Safari/604.1",

                    //// Safari on iOS (iPad)
                    //"Mozilla/5.0 (iPad; CPU OS 14_8 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.0 Mobile/15E148 Safari/604.1",
                    //"Mozilla/5.0 (iPad; CPU OS 13_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0 Mobile/15E148 Safari/604.1",

                    //// Chrome on Android
                    //"Mozilla/5.0 (Linux; Android 11; Pixel 4) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.101 Mobile Safari/537.36",
                    //"Mozilla/5.0 (Linux; Android 10; SM-A205U) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.198 Mobile Safari/537.36",
                    //"Mozilla/5.0 (Linux; Android 9; LG-V405) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.121 Mobile Safari/537.36",

                    //// Firefox on Android
                    //"Mozilla/5.0 (Android 10; Mobile; rv:85.0) Gecko/85.0 Firefox/85.0",
                    //"Mozilla/5.0 (Android 9; Mobile; rv:83.0) Gecko/83.0 Firefox/83.0",

                    //// Safari on macOS
                    //"Mozilla/5.0 (Macintosh; Intel Mac OS X 11_2_3) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.0.3 Safari/605.1.15",
                    //"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_14_6) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.1.2 Safari/605.1.15"
                ];
        }

        private static Anuncio ScanElement(IWebElement e, string cardPackType)
        {
            string url = "";
            string title = "";
            var price = "";
            var description = "";
            var size = "";
            var type = "";
            var rooms = "";
            var daysActive = "";
            var agentName = "";
            var street = "";
            int? streetNumber = null;
            var location = "";
            var source = "Fotocasa";

            if (cardPackType == ".re-CardPackPremium")
            {
                var carousel = e.FindElement(By.CssSelector(".re-CardPackPremium-carousel"));
                url = carousel.GetAttribute("href");
                title = carousel.GetAttribute("title");
                price = e.FindElement(By.CssSelector(".re-CardPrice")).Text;
                description = e.FindElement(By.CssSelector("span.re-CardDescription-text.re-CardDescription-text--isTwoLines")).Text.Trim();
                size = e.FindElement(By.CssSelector("span.re-CardFeaturesWithIcons-feature-icon.re-CardFeaturesWithIcons-feature-icon--surface")).Text.Trim();
                type = e.FindElement(By.CssSelector("span.re-I18nPropertyTitle > strong")).Text.Trim();
                rooms = e.FindElement(By.CssSelector(".re-CardFeaturesWithIcons-feature-icon.re-CardFeaturesWithIcons-feature-icon--rooms")).Text.Trim();
                daysActive = e.FindElement(By.CssSelector(".re-CardTimeAgo.re-CardTimeAgo--isInlineWithPrice")).Text.Trim();
                agentName = e.FindElement(By.CssSelector(".re-CardPromotionBanner-title-name")).Text.Trim();
                var address = MapAddress(title, type);
                street = address.Street;
                if(address.StreetNumber != null)
                    streetNumber = address.StreetNumber;
                location = address.Location;

            }

            if (cardPackType == ".re-CardPackAdvance")
            {
                var carousel = e.FindElement(By.CssSelector(".re-CardPackAdvance-slider"));
                url = carousel.GetAttribute("href");
                title = carousel.GetAttribute("title");
                price = e.FindElement(By.CssSelector(".re-CardPrice")).Text.Trim();
                description = e.FindElement(By.CssSelector("span.re-CardDescription-text.re-CardDescription-text--isTwoLines")).Text.Trim();
                size = e.FindElement(By.CssSelector("span.re-CardFeaturesWithIcons-feature-icon.re-CardFeaturesWithIcons-feature-icon--surface")).Text.Trim();
                type = e.FindElement(By.CssSelector("span.re-I18nPropertyTitle > strong")).Text.Trim();
                rooms = e.FindElement(By.CssSelector(".re-CardFeaturesWithIcons-feature-icon.re-CardFeaturesWithIcons-feature-icon--rooms")).Text.Trim();
                daysActive = e.FindElement(By.CssSelector(".re-CardTimeAgo.re-CardTimeAgo--isInlineWithPrice")).Text.Trim();
                agentName = e.FindElement(By.CssSelector(".re-CardPromotionBanner-title-name")).Text.Trim();
                var address = MapAddress(title, type);
                street = address.Street;
                if (address.StreetNumber != null)
                    streetNumber = address.StreetNumber;
                location = address.Location;
            }

            if (cardPackType == ".re-CardPackBasic")
            {
                var carousel = e.FindElement(By.CssSelector(".re-CardPackBasic-slider"));
                url = carousel.GetAttribute("href");
                title = carousel.GetAttribute("title");
                price = e.FindElement(By.CssSelector(".re-CardPrice")).Text.Trim();
                description = e.FindElement(By.CssSelector("span.re-cardDescription-text.re-cardDescription-text--isTwoLines")).Text.Trim();
                size = e.FindElement(By.CssSelector("span.re-cardFeaturesWithIcons-feature-icon.re-cardFeaturesWithIcons-feature-icon--surface")).Text.Trim();
                type = e.FindElement(By.CssSelector("span.re-I18nPropertyTitle > strong")).Text.Trim();
                rooms = e.FindElement(By.CssSelector(".re-CardFeaturesWithIcons-feature-icon.re-CardFeaturesWithIcons-feature-icon--rooms")).Text.Trim();
                daysActive = e.FindElement(By.CssSelector(".re-CardTimeAgo")).Text.Trim();
                var agentNameRaw = e.FindElements(By.CssSelector("div.re-CardPromotionLogo > a > img"));
                if (agentNameRaw.Count > 0)
                {
                    agentName = e.FindElement(By.CssSelector("div.re-CardPromotionLogo > a > img")).GetAttribute("title");
                }
                else
                {
                    agentName = "Sin agente especificado.";
                }
                var address = MapAddress(title, type);
                street = address.Street;
                if (address.StreetNumber != null)
                    streetNumber = address.StreetNumber;
                location = address.Location;
            }

            if (cardPackType == ".re-CardPackMinimal")
            {
                var carousel = e.FindElement(By.CssSelector(".re-CardPackMinimal-slider"));
                url = carousel.GetAttribute("href");
                title = carousel.GetAttribute("title");
                price = e.FindElement(By.CssSelector(".re-CardPrice")).Text.Trim();
                description = e.FindElement(By.CssSelector("span.re-cardDescription-text.re-cardDescription-text--isTwoLines")).Text.Trim();
                size = e.FindElement(By.CssSelector("li.re-CardFeatures-item.re-CardFeatures-feature:nth-of-type(3)")).Text.Trim();
                type = e.FindElement(By.CssSelector("span.re-I18nPropertyTitle > strong")).Text.Trim();
                rooms = e.FindElement(By.CssSelector("li.re-CardFeatures-item.re-CardFeatures-feature:nth-of-type(1)")).Text.Trim();
                daysActive = e.FindElement(By.CssSelector(".re-CardTimeAgo")).Text.Trim();
                var agentNameRaw = e.FindElements(By.CssSelector("div.re-CardPromotionLogo > a > img"));
                if (agentNameRaw.Count > 0)
                {
                    agentName = e.FindElement(By.CssSelector("div.re-CardPromotionLogo > a > img")).GetAttribute("title");
                }
                else
                {
                    agentName = "Sin agente especificado.";
                }
                var address = MapAddress(title, type);
                street = address.Street;
                if (address.StreetNumber != null)
                    streetNumber = address.StreetNumber;
                location = address.Location;
            }

            //var squareMeters = e.FindElement(By.CssSelector("re-CardFeaturesWithIcons-feature-icon--surface")).Text.Trim();
            //var rooms = e.FindElement(By.CssSelector("re-CardFeaturesWithIcons-feature-icon--rooms")).Text.Trim();

            Console.WriteLine($"Scanned new listing: --{title}-- --{price}--");
            return new Anuncio
            {
                Title = title,
                Price = ParsePriceHelper.ParsePrice(price),
                Description = description,
                Size = size,
                PropertyType = type,
                Rooms = rooms,
                DaysActive = daysActive,
                URL = url,
                AgentName = agentName,
                Street = street,
                StreetNumber = streetNumber,          
                Location= location,
                Source = source
            };
        }

        private static Address MapAddress(string title, string tipo)
        {
            var result = new Address();
            var split = title.Split(",");
            var hasStreetNumber = Int32.TryParse(split[1], out int streetNumber);
            result.Street = split[0].Replace($"{tipo} de alquiler en ", "").Trim();
            result.Location = title.Replace(split[0], "").Replace(split[1], "");
            if (hasStreetNumber)
            {
                result.StreetNumber = streetNumber;
            }
            return result;
        }

        private static async Task ScrollToBottomSlowlyAsync(IWebDriver driver, int scrollPauseTime = 500, int scrollIncrement = 300)
        {
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            long lastHeight = (long)js.ExecuteScript("return document.body.scrollHeight");
            long currentPosition = 0;

            while (currentPosition < lastHeight)
            {
                // Scroll down by the increment
                currentPosition += scrollIncrement;
                js.ExecuteScript($"window.scrollTo(0, {currentPosition});");

                // Wait for the specified pause time
                await Task.Delay(scrollPauseTime);

                // Update the total scroll height in case new content is loaded
                lastHeight = (long)js.ExecuteScript("return document.body.scrollHeight");
            }
        }
    }
}
