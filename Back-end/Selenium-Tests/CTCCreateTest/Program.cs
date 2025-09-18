using System;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace SeleniumTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // Initialize Chrome driver
            var options = new ChromeOptions();
            options.AddArgument("--start-maximized");

            using (var driver = new ChromeDriver(options))
            {
                try
                {
                    // Set timeouts
                    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

                    // STEP 1: LOGIN
                    Console.WriteLine("=== STEP 1: LOGIN ===");
                    driver.Navigate().GoToUrl("http://localhost:3000/");

                    // Wait for login page to load
                    wait.Until(d => d.FindElement(By.CssSelector("[data-testid='login-page']")));

                    // Enter credentials and login
                    driver
                        .FindElement(By.CssSelector("[data-testid='email-input']"))
                        .SendKeys("burnerghost306@gmail.com");
                    driver
                        .FindElement(By.CssSelector("[data-testid='password-input']"))
                        .SendKeys("2770b2090a!a");
                    driver.FindElement(By.CssSelector("[data-testid='login-button']")).Click();

                    // Verify login success
                    wait.Until(d =>
                        d.FindElement(By.CssSelector("[data-testid='dashboard-page']"))
                    );
                    Console.WriteLine("Login successful");
                    Thread.Sleep(1000); // Wait for dashboard to fully load

                    // STEP 2: NAVIGATE TO CTC FORM
                    Console.WriteLine("\n=== STEP 2: NAVIGATE TO CTC FORM ===");
                    driver.FindElement(By.CssSelector("[data-testid='create-ctc-card']")).Click();
                    wait.Until(d =>
                        d.FindElement(By.CssSelector("[data-testid='ctc-form-container']"))
                    );
                    Console.WriteLine("Navigated to CTC form");

                    // STEP 3: SELECT EMPLOYEE
                    Console.WriteLine("\n=== STEP 3: SELECT EMPLOYEE ===");
                    driver
                        .FindElement(By.CssSelector("[data-testid='select-employees-button']"))
                        .Click();

                    // Wait for employee selection modal
                    wait.Until(d =>
                        d.FindElement(By.CssSelector("[data-testid='employee-selection-modal']"))
                    );

                    // Search for employee
                    driver
                        .FindElement(By.CssSelector("[data-testid='employee-search-input']"))
                        .SendKeys("Sales Guy");
                    Thread.Sleep(1000); // Wait for search results

                    // Select employee checkbox
                    driver
                        .FindElement(By.CssSelector("[data-testid='employee-checkbox-151']"))
                        .Click();

                    // Confirm selection
                    driver
                        .FindElement(
                            By.CssSelector("[data-testid='confirm-employee-selection-button']")
                        )
                        .Click();

                    // Verify employee was selected
                    wait.Until(d =>
                        d.FindElement(By.CssSelector("[data-testid='selected-employees-count']"))
                    );
                    Console.WriteLine("Employee selected successfully");

                    // STEP 4: FILL CTC DETAILS
                    Console.WriteLine("\n=== STEP 4: FILL CTC DETAILS ===");

                    // Fill basic salary
                    var basicInput = driver.FindElement(
                        By.CssSelector("[data-testid='basic-input']")
                    );
                    basicInput.Clear();
                    basicInput.SendKeys("1200000");
                    Thread.Sleep(500); // Wait for HRA calculation

                    // Set effective date
                    var dateInput = driver.FindElement(
                        By.CssSelector("[data-testid='effective-date-input']")
                    );
                    dateInput.Clear();
                    dateInput.SendKeys(DateTime.Now.AddMonths(1).ToString("MM/dd/yyyy"));

                    // Add allowance
                    driver
                        .FindElement(By.CssSelector("[data-testid='add-allowance-button']"))
                        .Click();
                    Thread.Sleep(500); // Wait for allowance row to appear

                    // Fill allowance details
                    driver
                        .FindElement(By.CssSelector("[data-testid='allowance-label-0']"))
                        .SendKeys("Bonus");
                    driver
                        .FindElement(By.CssSelector("[data-testid='allowance-amount-0']"))
                        .SendKeys("1000");

                    // Add deduction
                    driver
                        .FindElement(By.CssSelector("[data-testid='add-deduction-button']"))
                        .Click();
                    Thread.Sleep(500); // Wait for deduction row to appear

                    // Fill deduction details
                    driver
                        .FindElement(By.CssSelector("[data-testid='deduction-label-0']"))
                        .SendKeys("PF");
                    driver
                        .FindElement(By.CssSelector("[data-testid='deduction-amount-0']"))
                        .SendKeys("1000");

                    // Verify preview calculations
                    wait.Until(d =>
                        d.FindElement(By.CssSelector("[data-testid='gross-ctc-preview']"))
                    );
                    Console.WriteLine("CTC details filled successfully");
                    Thread.Sleep(1000); // Let user see the preview

                    // STEP 5: SUBMIT CTC
                    Console.WriteLine("\n=== STEP 5: SUBMIT CTC ===");
                    driver.FindElement(By.CssSelector("[data-testid='submit-ctc-button']")).Click();

                    // Confirm submission
                    wait.Until(d =>
                        d.FindElement(By.CssSelector("[data-testid='confirm-submission-modal']"))
                    );
                    driver
                        .FindElement(By.CssSelector("[data-testid='confirm-submission-button']"))
                        .Click();

                    // Wait for results modal and then dashboard
                    try
                    {
                        wait.Until(d =>
                            d.FindElement(By.CssSelector("[data-testid='results-modal']"))
                        );
                        Console.WriteLine("CTC submitted successfully - results modal shown");

                        // Close modal if close button exists
                        try
                        {
                            driver
                                .FindElement(
                                    By.CssSelector("[data-testid='close-results-modal-button']")
                                )
                                .Click();
                        }
                        catch
                        { /* Modal might not have close button or already closed */
                        }

                        // Ensure we're back on dashboard
                        wait.Until(d =>
                            d.FindElement(By.CssSelector("[data-testid='dashboard-page']"))
                        );
                        Thread.Sleep(1000);
                    }
                    catch (WebDriverTimeoutException)
                    {
                        // Maybe modal didn't appear but submission succeeded
                        Console.WriteLine("Results modal didn't appear, checking for dashboard...");
                        wait.Until(d =>
                            d.FindElement(By.CssSelector("[data-testid='dashboard-page']"))
                        );
                    }

                    // STEP 6: NAVIGATE TO APPROVALS - Use direct URL to be safe
                    Console.WriteLine("\n=== STEP 6: NAVIGATE TO APPROVALS ===");
                    driver.Navigate().GoToUrl("http://localhost:3000/manager/approvals");
                    wait.Until(d =>
                        d.FindElement(By.CssSelector("[data-testid='approvals-page']"))
                    );
                    Console.WriteLine("Navigated to approvals page");

                    // STEP 6: NAVIGATE TO APPROVALS
                    Console.WriteLine("\n=== STEP 6: NAVIGATE TO APPROVALS ===");
                    // Instead of relying on UI navigation, use direct URL:
                    driver.Navigate().GoToUrl("http://localhost:3000/manager/approvals");
                    wait.Until(d =>
                        d.FindElement(By.CssSelector("[data-testid='approvals-page']"))
                    );

                    // STEP 7: SWITCH TO CTCs TAB AND SEARCH
                    Console.WriteLine("\n=== STEP 7: SEARCH CTC ===");
                    driver.FindElement(By.CssSelector("[data-testid='ctcs-view-button']")).Click();
                    Thread.Sleep(1000); // Wait for tab switch

                    // Search for the CTC we just created
                    driver
                        .FindElement(By.CssSelector("[data-testid='search-input']"))
                        .SendKeys("Sales Guy");
                    Thread.Sleep(2000); // Wait for search results

                    // Verify CTC appears in results
                    wait.Until(d => d.FindElement(By.CssSelector("[data-testid='record-row-0']")));
                    Console.WriteLine("CTC found in search results");

                    // STEP 8: REVIEW AND APPROVE CTC
                    Console.WriteLine("\n=== STEP 8: REVIEW AND APPROVE CTC ===");
                    driver.FindElement(By.CssSelector("[data-testid='review-button-0']")).Click();

                    // Wait for review modal
                    wait.Until(d => d.FindElement(By.CssSelector("[data-testid='review-modal']")));

                    // Approve the CTC
                    driver
                        .FindElement(By.CssSelector("[data-testid='approve-ctc-button']"))
                        .Click();
                    Thread.Sleep(1000); // Wait for approval to process

                    // Verify approval success
                    wait.Until(d =>
                        d.FindElement(By.CssSelector("[data-testid='status-cell-0']"))
                            .Text.Contains("Approved")
                    );
                    Console.WriteLine("CTC approved successfully");

                    Console.WriteLine("\n=== TEST COMPLETED SUCCESSFULLY ===");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n!!! TEST FAILED: {ex.Message}");
                }
                finally
                {
                    driver.Close();
                    driver.Quit();
                }
            }
        }
    }
}
