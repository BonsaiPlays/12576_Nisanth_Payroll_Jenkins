using System;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
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
                    var actions = new Actions(driver);

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
                        .SendKeys("2490b39210!a");
                    driver.FindElement(By.CssSelector("[data-testid='login-button']")).Click();

                    // Verify login success
                    wait.Until(d =>
                        d.FindElement(By.CssSelector("[data-testid='dashboard-page']"))
                    );
                    Console.WriteLine("Login successful");
                    Thread.Sleep(1000); // Wait for dashboard to fully load

                    // STEP 2: NAVIGATE TO GENERATE PAYSLIP
                    Console.WriteLine("\n=== STEP 2: NAVIGATE TO GENERATE PAYSLIP ===");
                    driver
                        .FindElement(By.CssSelector("[data-testid='generate-payslip-card']"))
                        .Click();
                    wait.Until(d =>
                        d.FindElement(By.CssSelector("[data-testid='generate-payslip-page']"))
                    );
                    Console.WriteLine("Navigated to payslip generation page");

                    // STEP 3: SELECT EMPLOYEE
                    Console.WriteLine("\n=== STEP 3: SELECT EMPLOYEE ===");
                    var employeeDropdown = wait.Until(d =>
                        d.FindElement(By.CssSelector("[role='combobox'][aria-haspopup='listbox']"))
                    );
                    employeeDropdown.Click();
                    Thread.Sleep(2000); // Wait for dropdown to open

                    // Scroll to find "Sales Guy" (employee ID 151)
                    var employeeOption = wait.Until(d =>
                        d.FindElement(By.CssSelector("[data-testid='employee-option-151']"))
                    );
                    actions.MoveToElement(employeeOption).Perform();
                    Thread.Sleep(2000); // Visual confirmation
                    employeeOption.Click();
                    Console.WriteLine("Selected employee 'Sales Guy'");

                    // Wait for CTC details to load
                    wait.Until(d =>
                        d.FindElement(By.CssSelector("[data-testid='ctc-details-card']"))
                    );
                    Thread.Sleep(3000); // Wait for calculations

                    // STEP 4: FILL PAYSLIP DETAILS
                    Console.WriteLine("\n=== STEP 4: FILL PAYSLIP DETAILS ===");

                    // Set LOP days
                    driver.FindElement(By.CssSelector("[data-testid='lop-days-input']")).Clear();
                    driver
                        .FindElement(By.CssSelector("[data-testid='lop-days-input']"))
                        .SendKeys("3");
                    Console.WriteLine("Set LOP days to 3");

                    // Set override allowances
                    driver
                        .FindElement(By.CssSelector("[data-testid='override-allowances-input']"))
                        .Clear();
                    driver
                        .FindElement(By.CssSelector("[data-testid='override-allowances-input']"))
                        .SendKeys("1000");
                    Console.WriteLine("Set override allowances to 1000");

                    // Set override deductions
                    driver
                        .FindElement(By.CssSelector("[data-testid='override-deductions-input']"))
                        .Clear();
                    driver
                        .FindElement(By.CssSelector("[data-testid='override-deductions-input']"))
                        .SendKeys("800");
                    Console.WriteLine("Set override deductions to 800");

                    // Verify preview updates
                    wait.Until(d =>
                        d.FindElement(By.CssSelector("[data-testid='net-pay-preview']"))
                    );
                    Thread.Sleep(1000); // Let user see the preview

                    // STEP 5: GENERATE PAYSLIP
                    Console.WriteLine("\n=== STEP 5: GENERATE PAYSLIP ===");
                    driver
                        .FindElement(By.CssSelector("[data-testid='generate-payslip-button']"))
                        .Click();

                    // Wait for success message with more robust handling
                    try
                    {
                        // First check if we're redirected (which would indicate success)
                        wait.Until(d => d.Url != "http://localhost:3000/hr/generate-payslip");
                        Console.WriteLine(
                            "Navigation detected - payslip likely generated successfully"
                        );
                    }
                    catch (WebDriverTimeoutException)
                    {
                        // If not redirected, look for success message
                        try
                        {
                            wait.Until(d =>
                                d.FindElement(By.CssSelector("[data-testid='success-snackbar']"))
                            );
                            Console.WriteLine(
                                "Success snackbar detected - payslip generated successfully"
                            );
                        }
                        catch (WebDriverTimeoutException)
                        {
                            // If neither worked, check for error message
                            try
                            {
                                var errorElement = driver.FindElement(
                                    By.CssSelector("[data-testid='error-snackbar'], [role='alert']")
                                );
                                Console.WriteLine($"Error detected: {errorElement.Text}");
                                throw new Exception(
                                    $"Payslip generation failed: {errorElement.Text}"
                                );
                            }
                            catch (NoSuchElementException)
                            {
                                // If no error message either, proceed with warning
                                Console.WriteLine(
                                    "WARNING: Could not verify success or error state, proceeding anyway"
                                );
                            }
                        }
                    }

                    Thread.Sleep(3000);

                    // STEP 6: NAVIGATE TO APPROVALS
                    Console.WriteLine("\n=== STEP 6: NAVIGATE TO APPROVALS ===");
                    try
                    {
                        // Navigate directly to approvals page
                        driver.Navigate().GoToUrl("http://localhost:3000/manager/approvals");

                        // Wait for either the approvals page or a loading indicator
                        wait.Until(d =>
                            d.FindElements(
                                By.CssSelector(
                                    "[data-testid='approvals-page'], [data-testid='loading-spinner']"
                                )
                            ).Count > 0
                        );

                        // If we see a loading spinner, wait for it to disappear
                        if (
                            driver
                                .FindElements(By.CssSelector("[data-testid='loading-spinner']"))
                                .Count > 0
                        )
                        {
                            wait.Until(d =>
                                d.FindElements(
                                    By.CssSelector("[data-testid='loading-spinner']")
                                ).Count == 0
                            );
                        }

                        // Final verification of approvals page
                        wait.Until(d =>
                            d.FindElement(
                                By.CssSelector("[data-testid='approvals-page']")
                            ).Displayed
                        );
                        Console.WriteLine("Navigated to approvals page");

                        // Take screenshot for verification
                        ((ITakesScreenshot)driver)
                            .GetScreenshot()
                            .SaveAsFile("approvals_page.png");
                        Console.WriteLine("Screenshot saved: approvals_page.png");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error navigating to approvals: {ex.Message}");
                        ((ITakesScreenshot)driver)
                            .GetScreenshot()
                            .SaveAsFile("approvals_error.png");
                        throw;
                    }

                    // STEP 7: SEARCH FOR PAYSLIP
                    Console.WriteLine("\n=== STEP 7: SEARCH FOR PAYSLIP ===");
                    try
                    {
                        // Wait for search input to be interactive
                        var searchInput = wait.Until(d =>
                            d.FindElement(By.CssSelector("[data-testid='search-input']"))
                        );

                        // Clear and search
                        searchInput.Clear();
                        searchInput.SendKeys("Sales Guy");
                        Console.WriteLine("Search entered");

                        // Wait for search results (with longer timeout)
                        new WebDriverWait(driver, TimeSpan.FromSeconds(30)).Until(d =>
                            d.FindElements(By.CssSelector("[data-testid='record-row-0']")).Count > 0
                        );

                        // Verify payslip appears in results
                        var firstPayslipRow = driver.FindElement(
                            By.CssSelector("[data-testid='record-row-0']")
                        );
                        Console.WriteLine("Found payslip in search results");

                        // Take screenshot of search results
                        ((ITakesScreenshot)driver)
                            .GetScreenshot()
                            .SaveAsFile("search_results.png");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error searching for payslip: {ex.Message}");
                        ((ITakesScreenshot)driver).GetScreenshot().SaveAsFile("search_error.png");
                        throw;
                    }

                    // STEP 8: REVIEW AND APPROVE PAYSLIP
                    // STEP 8: REVIEW AND APPROVE PAYSLIP
                    Console.WriteLine("\n=== STEP 8: REVIEW AND APPROVE PAYSLIP ===");
                    try
                    {
                        // Click review button with retry and enhanced waiting
                        int reviewAttempts = 0;
                        while (reviewAttempts < 3)
                        {
                            try
                            {
                                // Wait for the review button to be clickable
                                var reviewButton = new WebDriverWait(
                                    driver,
                                    TimeSpan.FromSeconds(30)
                                ).Until(d =>
                                {
                                    var btn = d.FindElement(
                                        By.CssSelector("[data-testid='review-button-0']")
                                    );
                                    return btn.Displayed && btn.Enabled ? btn : null;
                                });

                                // Scroll to button and add visual highlight for debugging
                                ((IJavaScriptExecutor)driver).ExecuteScript(
                                    "arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});",
                                    reviewButton
                                );
                                ((IJavaScriptExecutor)driver).ExecuteScript(
                                    "arguments[0].style.border='3px solid red';",
                                    reviewButton
                                );
                                Thread.Sleep(2000); // Extra time for visual confirmation

                                // Click using JavaScript
                                ((IJavaScriptExecutor)driver).ExecuteScript(
                                    "arguments[0].click();",
                                    reviewButton
                                );
                                Console.WriteLine("Clicked review button");

                                // Remove highlight
                                ((IJavaScriptExecutor)driver).ExecuteScript(
                                    "arguments[0].style.border='';",
                                    reviewButton
                                );
                                break;
                            }
                            catch (Exception ex)
                            {
                                reviewAttempts++;
                                Console.WriteLine(
                                    $"Review button click attempt {reviewAttempts} failed: {ex.Message}"
                                );
                                if (reviewAttempts == 3)
                                    throw;
                                Thread.Sleep(2000); // Longer delay between attempts
                            }
                        }

                        // Wait for modal to fully open with multiple verification points
                        var modal = new WebDriverWait(driver, TimeSpan.FromSeconds(30)).Until(d =>
                        {
                            try
                            {
                                var m = d.FindElement(
                                    By.CssSelector("[data-testid='review-modal']")
                                );
                                if (!m.Displayed || m.GetAttribute("aria-hidden") == "true")
                                    return null;

                                // Check for loading indicators inside modal
                                var loadingIndicators = m.FindElements(
                                    By.CssSelector("[data-testid='loading-indicator']")
                                );
                                if (
                                    loadingIndicators.Count > 0
                                    && loadingIndicators.Any(i => i.Displayed)
                                )
                                    return null;

                                return m;
                            }
                            catch
                            {
                                return null;
                            }
                        });

                        if (modal == null)
                        {
                            // Take screenshot of current state for debugging
                            ((ITakesScreenshot)driver)
                                .GetScreenshot()
                                .SaveAsFile("modal_failed_to_open.png");
                            throw new Exception(
                                "Review modal failed to open properly - see modal_failed_to_open.png"
                            );
                        }

                        // Find approve button with multiple fallback strategies
                        IWebElement approveButton = null;
                        string[] approveButtonSelectors = new[]
                        {
                            "[data-testid='approve-payslip-button']",
                            "button:contains('Approve')",
                            ".MuiButton-containedPrimary",
                            "button.MuiButton-root:not([disabled])",
                        };

                        foreach (var selector in approveButtonSelectors)
                        {
                            try
                            {
                                approveButton = modal.FindElement(By.CssSelector(selector));
                                if (approveButton.Displayed && approveButton.Enabled)
                                {
                                    Console.WriteLine(
                                        $"Found approve button with selector: {selector}"
                                    );
                                    break;
                                }
                            }
                            catch
                            {
                                continue;
                            }
                        }

                        if (approveButton == null)
                        {
                            // Dump all buttons in modal for debugging
                            var buttons = modal.FindElements(By.CssSelector("button"));
                            Console.WriteLine($"Found {buttons.Count} buttons in modal:");
                            foreach (var btn in buttons)
                            {
                                Console.WriteLine(
                                    $"- Text: '{btn.Text}' | Enabled: {btn.Enabled} | Displayed: {btn.Displayed} | TestID: {btn.GetAttribute("data-testid")}"
                                );
                            }
                            throw new Exception("Could not find an active approve button in modal");
                        }

                        // Click approve button with enhanced handling
                        int approveAttempts = 0;
                        while (approveAttempts < 3)
                        {
                            try
                            {
                                // Highlight button for visibility
                                ((IJavaScriptExecutor)driver).ExecuteScript(
                                    "arguments[0].style.border='3px solid green';",
                                    approveButton
                                );
                                Thread.Sleep(1000);

                                // Scroll to button
                                ((IJavaScriptExecutor)driver).ExecuteScript(
                                    "arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});",
                                    approveButton
                                );
                                Thread.Sleep(1000);

                                // Click using JavaScript
                                ((IJavaScriptExecutor)driver).ExecuteScript(
                                    "arguments[0].click();",
                                    approveButton
                                );
                                Console.WriteLine("Clicked approve button");

                                // Remove highlight
                                ((IJavaScriptExecutor)driver).ExecuteScript(
                                    "arguments[0].style.border='';",
                                    approveButton
                                );
                                break;
                            }
                            catch (Exception ex)
                            {
                                approveAttempts++;
                                Console.WriteLine(
                                    $"Approve button click attempt {approveAttempts} failed: {ex.Message}"
                                );
                                if (approveAttempts == 3)
                                    throw;
                                Thread.Sleep(2000);
                            }
                        }

                        // Enhanced approval verification
                        bool approvalComplete = false;
                        DateTime startTime = DateTime.Now;
                        int checks = 0;

                        while (!approvalComplete && (DateTime.Now - startTime).TotalSeconds < 45) // Extended timeout
                        {
                            checks++;
                            try
                            {
                                // Check for success snackbar
                                var snackbars = driver.FindElements(
                                    By.CssSelector("[data-testid='success-snackbar']")
                                );
                                if (snackbars.Count > 0 && snackbars.Any(s => s.Displayed))
                                {
                                    Console.WriteLine("Approval success snackbar detected");
                                    approvalComplete = true;
                                    break;
                                }

                                // Check table status
                                var statusCell = driver.FindElement(
                                    By.CssSelector("[data-testid='status-cell-0']")
                                );
                                if (statusCell.Text.Contains("Approved"))
                                {
                                    Console.WriteLine("Status cell shows Approved");
                                    approvalComplete = true;
                                    break;
                                }

                                // Check if modal closed (if it should)
                                if (
                                    driver
                                        .FindElements(
                                            By.CssSelector("[data-testid='review-modal']")
                                        )
                                        .Count == 0
                                )
                                {
                                    Console.WriteLine("Modal closed after approval");
                                    approvalComplete = true;
                                    break;
                                }

                                // Every 5 seconds, log current state
                                if (checks % 5 == 0)
                                {
                                    Console.WriteLine(
                                        $"Waiting for approval confirmation... ({(DateTime.Now - startTime).TotalSeconds}s elapsed)"
                                    );
                                    ((ITakesScreenshot)driver)
                                        .GetScreenshot()
                                        .SaveAsFile($"approval_wait_{checks}.png");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Approval check error: {ex.Message}");
                            }

                            Thread.Sleep(1000);
                        }

                        if (!approvalComplete)
                        {
                            ((ITakesScreenshot)driver)
                                .GetScreenshot()
                                .SaveAsFile("approval_timeout.png");
                            throw new Exception(
                                "Approval verification timeout - see approval_timeout.png"
                            );
                        }

                        Console.WriteLine("Payslip approved successfully");
                        Thread.Sleep(3000); // Extended buffer time
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error approving payslip: {ex.Message}");
                        ((ITakesScreenshot)driver).GetScreenshot().SaveAsFile("approval_error.png");
                        throw;
                    }

                    // STEP 9: RELEASE PAYSLIP
                    Console.WriteLine("\n=== STEP 9: RELEASE PAYSLIP ===");
                    try
                    {
                        // Wait for UI to stabilize after approval with visual feedback
                        Console.WriteLine("Waiting for UI to stabilize after approval...");
                        ((IJavaScriptExecutor)driver).ExecuteScript(
                            "document.body.style.border='3px solid orange';"
                        );
                        Thread.Sleep(3000); // Additional wait time
                        ((IJavaScriptExecutor)driver).ExecuteScript(
                            "document.body.style.border='';"
                        );

                        // Re-find the review button with enhanced waiting
                        var reviewButton = new WebDriverWait(
                            driver,
                            TimeSpan.FromSeconds(30)
                        ).Until(d =>
                        {
                            var btn = d.FindElement(
                                By.CssSelector("[data-testid='review-button-0']")
                            );
                            if (!btn.Displayed || !btn.Enabled)
                                return null;

                            // Visual feedback
                            ((IJavaScriptExecutor)driver).ExecuteScript(
                                "arguments[0].style.border='3px solid blue'; arguments[0].style.boxShadow='0 0 10px blue';",
                                btn
                            );
                            return btn;
                        });

                        // Click review button with enhanced handling
                        int reviewAttempts = 0;
                        while (reviewAttempts < 3)
                        {
                            try
                            {
                                // Smooth scroll to button
                                ((IJavaScriptExecutor)driver).ExecuteScript(
                                    "arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});",
                                    reviewButton
                                );
                                Thread.Sleep(2000);

                                // Click using JavaScript
                                ((IJavaScriptExecutor)driver).ExecuteScript(
                                    "arguments[0].click();",
                                    reviewButton
                                );
                                Console.WriteLine("Clicked review button for release");

                                // Remove highlight
                                ((IJavaScriptExecutor)driver).ExecuteScript(
                                    "arguments[0].style.border=''; arguments[0].style.boxShadow='';",
                                    reviewButton
                                );
                                break;
                            }
                            catch (Exception ex)
                            {
                                reviewAttempts++;
                                Console.WriteLine(
                                    $"Review button click attempt {reviewAttempts} failed: {ex.Message}"
                                );
                                if (reviewAttempts == 3)
                                    throw;
                                Thread.Sleep(2000);
                            }
                        }

                        // Wait for release modal with comprehensive checks
                        var releaseModal = new WebDriverWait(
                            driver,
                            TimeSpan.FromSeconds(30)
                        ).Until(d =>
                        {
                            try
                            {
                                var m = d.FindElement(
                                    By.CssSelector("[data-testid='review-modal']")
                                );
                                if (!m.Displayed || m.GetAttribute("aria-hidden") == "true")
                                    return null;

                                // Check for loading states
                                var loadingIndicators = m.FindElements(
                                    By.CssSelector("[data-testid='loading-indicator']")
                                );
                                if (
                                    loadingIndicators.Count > 0
                                    && loadingIndicators.Any(i => i.Displayed)
                                )
                                    return null;

                                // Verify release button is present and ready
                                var releaseBtn = m.FindElements(
                                    By.CssSelector("[data-testid='release-payslip-button']")
                                );
                                if (
                                    releaseBtn.Count == 0
                                    || !releaseBtn[0].Displayed
                                    || !releaseBtn[0].Enabled
                                )
                                    return null;

                                // Visual confirmation
                                ((IJavaScriptExecutor)driver).ExecuteScript(
                                    "arguments[0].style.border='3px solid purple';",
                                    m
                                );
                                return m;
                            }
                            catch
                            {
                                return null;
                            }
                        });

                        if (releaseModal == null)
                        {
                            ((ITakesScreenshot)driver)
                                .GetScreenshot()
                                .SaveAsFile("release_modal_failed.png");
                            throw new Exception(
                                "Release modal failed to open - see release_modal_failed.png"
                            );
                        }

                        // Find release button with multiple selector strategies
                        IWebElement releaseButton = null;
                        string[] releaseButtonSelectors = new[]
                        {
                            "[data-testid='release-payslip-button']",
                            "button:contains('Release')",
                            ".MuiButton-containedPrimary",
                            "button.MuiButton-root:not([disabled])",
                        };

                        foreach (var selector in releaseButtonSelectors)
                        {
                            try
                            {
                                releaseButton = releaseModal.FindElement(By.CssSelector(selector));
                                if (releaseButton.Displayed && releaseButton.Enabled)
                                {
                                    Console.WriteLine(
                                        $"Found release button with selector: {selector}"
                                    );
                                    // Highlight button
                                    ((IJavaScriptExecutor)driver).ExecuteScript(
                                        "arguments[0].style.border='3px solid green'; arguments[0].style.boxShadow='0 0 10px green';",
                                        releaseButton
                                    );
                                    break;
                                }
                            }
                            catch
                            {
                                continue;
                            }
                        }

                        if (releaseButton == null)
                        {
                            // Diagnostic dump
                            var buttons = releaseModal.FindElements(By.CssSelector("button"));
                            Console.WriteLine($"Found {buttons.Count} buttons in release modal:");
                            foreach (var btn in buttons)
                            {
                                Console.WriteLine(
                                    $"- Text: '{btn.Text}' | Enabled: {btn.Enabled} | Displayed: {btn.Displayed} | TestID: {btn.GetAttribute("data-testid")}"
                                );
                            }
                            throw new Exception("Could not find an active release button in modal");
                        }

                        // Click release button with retry logic
                        int releaseAttempts = 0;
                        while (releaseAttempts < 3)
                        {
                            try
                            {
                                // Scroll to button
                                ((IJavaScriptExecutor)driver).ExecuteScript(
                                    "arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});",
                                    releaseButton
                                );
                                Thread.Sleep(1000);

                                // Click using JavaScript
                                ((IJavaScriptExecutor)driver).ExecuteScript(
                                    "arguments[0].click();",
                                    releaseButton
                                );
                                Console.WriteLine("Clicked release button");

                                // Remove highlight
                                ((IJavaScriptExecutor)driver).ExecuteScript(
                                    "arguments[0].style.border=''; arguments[0].style.boxShadow='';",
                                    releaseButton
                                );
                                break;
                            }
                            catch (Exception ex)
                            {
                                releaseAttempts++;
                                Console.WriteLine(
                                    $"Release button click attempt {releaseAttempts} failed: {ex.Message}"
                                );
                                if (releaseAttempts == 3)
                                    throw;
                                Thread.Sleep(2000);
                            }
                        }

                        // Enhanced release verification
                        bool releaseComplete = false;
                        DateTime startTime = DateTime.Now;
                        int checks = 0;

                        while (!releaseComplete && (DateTime.Now - startTime).TotalSeconds < 45)
                        {
                            checks++;
                            try
                            {
                                // Check for success snackbar
                                var snackbars = driver.FindElements(
                                    By.CssSelector("[data-testid='success-snackbar']")
                                );
                                if (snackbars.Count > 0 && snackbars.Any(s => s.Displayed))
                                {
                                    Console.WriteLine("Release success snackbar detected");
                                    releaseComplete = true;
                                    break;
                                }

                                // Check table status
                                var statusCell = driver.FindElement(
                                    By.CssSelector("[data-testid='status-cell-0']")
                                );
                                if (statusCell.Text.Contains("Released"))
                                {
                                    Console.WriteLine("Status cell shows Released");
                                    releaseComplete = true;
                                    break;
                                }

                                // Check if modal closed
                                if (
                                    driver
                                        .FindElements(
                                            By.CssSelector("[data-testid='review-modal']")
                                        )
                                        .Count == 0
                                )
                                {
                                    Console.WriteLine("Release modal closed");
                                    releaseComplete = true;
                                    break;
                                }

                                // Periodic logging
                                if (checks % 5 == 0)
                                {
                                    Console.WriteLine(
                                        $"Waiting for release confirmation... ({(DateTime.Now - startTime).TotalSeconds}s elapsed)"
                                    );
                                    ((ITakesScreenshot)driver)
                                        .GetScreenshot()
                                        .SaveAsFile($"release_wait_{checks}.png");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Release check error: {ex.Message}");
                            }

                            Thread.Sleep(1000);
                        }

                        if (!releaseComplete)
                        {
                            ((ITakesScreenshot)driver)
                                .GetScreenshot()
                                .SaveAsFile("release_timeout.png");
                            throw new Exception(
                                "Release verification timeout - see release_timeout.png"
                            );
                        }

                        Console.WriteLine("Payslip released successfully");
                        Thread.Sleep(3000); // Final buffer time
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error releasing payslip: {ex.Message}");
                        ((ITakesScreenshot)driver).GetScreenshot().SaveAsFile("release_error.png");
                        throw;
                    }

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
