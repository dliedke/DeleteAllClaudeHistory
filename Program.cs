/* *******************************************************************************************************************
 * Application: DeleteAllClaudeHistory
 * 
 * Autor:  Daniel Liedke
 * 
 * Copyright © Daniel Liedke 2024
 * Usage and reproduction in any manner whatsoever without the written permission of Daniel Liedke is strictly forbidden.
 *  
 * Purpose: Clear all history of chats for Claude AI
 *           
 * *******************************************************************************************************************/

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace DeleteAllClaudeHistory
{
    public class Program
    {
        static void Main(string[] args)
        {
            // Request email for chrome profile that is already logged in Claude AI and save in email variable
            Console.WriteLine("NOTE: If you face issues, clear all cache from Chrome Browser, log into Claude AI and try again.\r\n");
            Console.WriteLine("Please enter the email address used for the Chrome Browser profile logged already into Claude AI:\r\n");

            string? email = Console.ReadLine();

            // Validate the email input
            while (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            {
                Console.WriteLine("Invalid email address. Please try again:");
                email = Console.ReadLine();
            }

            string profileName;
            try
            {
                profileName = FindChromeProfilePath(email);
                Console.WriteLine($"Found profile: {profileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding Chrome profile: {ex.Message}");
                return;
            }

            // Find the user data directory for the profile
            string userDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Google\Chrome\User Data");

            // Create a new Chrome driver with the user data directory and profile
            ChromeOptions options = new ChromeOptions();
            options.AddArgument($"user-data-dir={userDataDir}");
            options.AddArgument($"profile-directory={profileName}");
            options.AddArgument("--start-maximized");
            options.AddUserProfilePreference("excludeSwitches", new string[] { "enable-logging" });

            // Add these lines to suppress ChromeDriver output
            options.AddArgument("--log-level=3");
            options.AddArgument("--silent");

            // Use Selenium Manager
            var service = ChromeDriverService.CreateDefaultService();
            service.EnableVerboseLogging = false;
            service.SuppressInitialDiagnosticInformation = true;

            // Create a new Chrome driver with the options
            using (IWebDriver driver = new ChromeDriver(service, options))
            {
                try
                {
                    // Navigate to the chats page
                    driver.Navigate().GoToUrl("https://claude.ai/chats");

                    // Wait 5 seconds for the page to load first time
                    Thread.Sleep(5000);

                    // Default to 15s wait time
                    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

                    int totalChatsCount = 0;
                    int count = 0;

                    while (true)
                    {
                        // Check if we have any chats
                        var chatRows = driver.FindElements(By.CssSelector("a[href^='/chat/']"));

                        if (chatRows.Count == 0)
                        {
                            if (count == 0)
                            {
                                Console.WriteLine("\r\nNo chats found. Exiting...\r\n");
                            }
                            else
                            {
                                Console.WriteLine($"\r\nAll chats deleted successfully.\r\n");
                            }
                            break;
                        }

                        // Get total chats count
                        if (totalChatsCount == 0)
                        {
                            totalChatsCount = chatRows.Count;
                            Console.WriteLine($"\r\nTotal chats found: {totalChatsCount}");
                        }

                        // Find first chat row
                        var chatRow = chatRows[0];

                        // Scroll to the chat row
                        ScrollToElement(driver, chatRow);

                        // Click on the chat row
                        chatRow.Click();

                        // Wait 2 seconds
                        Thread.Sleep(2000);

                        // Click on the div.font-tiempos.truncate element to expand the chat
                        wait.Until(d => d.FindElement(By.CssSelector("div.font-tiempos.truncate")));
                        IWebElement expandChat = wait.Until(d => d.FindElement(By.CssSelector("div.font-tiempos.truncate")));
                        expandChat.Click();

                        // Click on the delete button
                        IWebElement deleteButton = wait.Until(d => d.FindElement(By.CssSelector("div[data-testid='delete-chat-trigger']")));
                        deleteButton.Click();
                        Thread.Sleep(500);

                        // Click on the confirm delete button
                        IWebElement confirmDelete = wait.Until(d => d.FindElement(By.CssSelector("button[data-testid='delete-modal-confirm']")));
                        confirmDelete.Click();

                        // Show success message with chat name
                        count++;
                        Console.WriteLine($"{count}/{totalChatsCount} - {(double)count / totalChatsCount * 100:F2}% - Chat \"{expandChat.Text}\" deleted successfully.");

                        // Wait 2 seconds for page reload
                        Thread.Sleep(2000);
                    }

                    // Show message and Press any key to exit
                    Console.WriteLine("\r\nPress any key to exit...");
                    Console.ReadKey();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }

        static string FindChromeProfilePath(string email)
        {
            // Find the user data directory for the profile
            string localStatePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Google\Chrome\User Data\Local State");

            // Check if the local state file exists
            if (!File.Exists(localStatePath))
            {
                throw new FileNotFoundException("Chrome Local State file not found.");
            }

            // Read the local state file
            string jsonContent = File.ReadAllText(localStatePath);
            using JsonDocument doc = JsonDocument.Parse(jsonContent);
            JsonElement root = doc.RootElement;

            // Try to find the Chrome profile for the email
            if (root.TryGetProperty("profile", out JsonElement profile) &&
                profile.TryGetProperty("info_cache", out JsonElement infoCache))
            {
                foreach (JsonProperty profileProp in infoCache.EnumerateObject())
                {
                    if (profileProp.Value.TryGetProperty("user_name", out JsonElement userName) &&
                        userName.GetString() == email)
                    {
                        return profileProp.Name;
                    }
                }
            }

            throw new Exception($"Profile for email {email} not found.");
        }

        static void ScrollToElement(IWebDriver driver, IWebElement element)
        {
            // Scroll to the element
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", element);

            // Give the page some time to settle after scrolling
            Thread.Sleep(500);
        }

        // Helper method to validate email format
        static bool IsValidEmail(string email)
        {
            try
            {
                // Try parsing the email address
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}