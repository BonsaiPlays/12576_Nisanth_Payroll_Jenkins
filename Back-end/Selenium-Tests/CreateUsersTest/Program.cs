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
            // Uncomment the next line to run in headless mode
            // options.AddArgument("--headless");

            using (var driver = new ChromeDriver(options))
            {
                try
                {
                    // Maximize browser window
                    driver.Manage().Window.Maximize();
                    // Set implicit wait
                    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

                    // STEP 1: LOGIN
                    Console.WriteLine("=== STEP 1: LOGIN ===");
                    Console.WriteLine("Navigating to React app login page...");
                    driver.Navigate().GoToUrl("http://localhost:3000/");

                    // Wait a moment for the page to fully load
                    Thread.Sleep(2000);

                    // Find and fill the email field
                    Console.WriteLine("Entering email...");
                    IWebElement emailField = null;
                    try
                    {
                        emailField = driver.FindElement(
                            By.XPath("//label[text()='Email']/following-sibling::div/input")
                        );
                    }
                    catch (NoSuchElementException)
                    {
                        try
                        {
                            emailField = driver.FindElement(
                                By.XPath("//form//input[@type='text'][1]")
                            );
                        }
                        catch (NoSuchElementException)
                        {
                            emailField = driver.FindElement(
                                By.XPath(
                                    "//input[@type='text'][preceding::label[contains(text(),'Email')]]"
                                )
                            );
                        }
                    }

                    emailField.Clear();
                    emailField.SendKeys("nisanthsaru.oto@gmail.com");

                    // Find and fill the password field
                    Console.WriteLine("Entering password...");
                    IWebElement passwordField = null;
                    try
                    {
                        passwordField = driver.FindElement(
                            By.XPath("//label[text()='Password']/following-sibling::div/input")
                        );
                    }
                    catch (NoSuchElementException)
                    {
                        try
                        {
                            passwordField = driver.FindElement(
                                By.XPath("//form//input[@type='password']")
                            );
                        }
                        catch (NoSuchElementException)
                        {
                            passwordField = driver.FindElement(
                                By.CssSelector("input[type='password']")
                            );
                        }
                    }

                    passwordField.Clear();
                    passwordField.SendKeys("Admin@123");

                    // Click the login button
                    Console.WriteLine("Clicking login button...");
                    var loginButton = driver.FindElement(
                        By.XPath("//button[@type='submit' and contains(text(), 'Login')]")
                    );
                    loginButton.Click();

                    // Wait for dashboard to load
                    Console.WriteLine("Waiting for dashboard to load...");
                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

                    var welcomeElement = wait.Until(d =>
                        d.FindElement(By.XPath("//h5[contains(text(), 'Welcome')]"))
                    );
                    Console.WriteLine("Successfully logged in! Dashboard loaded.");

                    // Wait a moment to see the dashboard
                    Thread.Sleep(2000);

                    // STEP 2: NAVIGATE TO MANAGE USERS
                    Console.WriteLine("\n=== STEP 2: NAVIGATE TO MANAGE USERS ===");
                    Console.WriteLine("Looking for Manage Users card...");

                    // Find and click the Manage Users card
                    var manageUsersCard = driver.FindElement(
                        By.XPath("//h6[text()='Manage Users']/ancestor::a")
                    );
                    manageUsersCard.Click();

                    // Wait for the User Management page to load
                    Console.WriteLine("Waiting for User Management page...");
                    wait.Until(d => d.FindElement(By.XPath("//h5[text()='User Management']")));
                    Thread.Sleep(2000);

                    // STEP 3: CREATE NEW HR USER
                    Console.WriteLine("\n=== STEP 3: CREATE NEW hr USER ===");
                    Console.WriteLine("Clicking Create User button...");

                    // Click Create User button
                    var createUserButton = driver.FindElement(
                        By.XPath("//button[contains(text(), 'Create User')]")
                    );
                    createUserButton.Click();

                    // Wait for the dialog to open
                    Thread.Sleep(1000);

                    // For the email field in create user dialog
                    Console.WriteLine("Filling user details...");
                    try
                    {
                        var waitForDialog = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                        var newEmailField = waitForDialog.Until(d =>
                        {
                            var element = d.FindElement(
                                By.XPath(
                                    "//div[@role='dialog']//label[text()='Email']/following-sibling::div/input"
                                )
                            );
                            return element.Enabled && element.Displayed ? element : null;
                        });

                        newEmailField.Clear();
                        Thread.Sleep(500);
                        newEmailField.SendKeys("nisanthsaru@gmail.com");
                    }
                    catch (WebDriverTimeoutException)
                    {
                        Console.WriteLine(
                            "Failed to find or interact with email field in create dialog"
                        );
                        throw;
                    }

                    // For the full name field
                    try
                    {
                        var waitForName = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                        var fullNameField = waitForName.Until(d =>
                        {
                            var element = d.FindElement(
                                By.XPath(
                                    "//div[@role='dialog']//label[text()='Full Name']/following-sibling::div/input"
                                )
                            );
                            return element.Enabled && element.Displayed ? element : null;
                        });

                        fullNameField.Clear();
                        Thread.Sleep(500);
                        fullNameField.SendKeys("Nisanth");
                    }
                    catch (WebDriverTimeoutException)
                    {
                        Console.WriteLine(
                            "Failed to find or interact with name field in create dialog"
                        );
                        throw;
                    }

                    // For the role dropdown
                    Console.WriteLine("Selecting role...");
                    try
                    {
                        var waitForRole = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                        var roleDropdown = waitForRole.Until(d =>
                        {
                            var element = d.FindElement(
                                By.XPath(
                                    "//div[@role='dialog']//div[contains(@class, 'MuiSelect-select')]"
                                )
                            );
                            return element.Enabled && element.Displayed ? element : null;
                        });

                        roleDropdown.Click();
                        Thread.Sleep(1000);

                        var hrOption = driver.FindElement(
                            By.XPath("//li[@role='option' and text()='HR']")
                        );
                        hrOption.Click();
                        Thread.Sleep(1000);
                    }
                    catch (WebDriverTimeoutException)
                    {
                        Console.WriteLine("Failed to find or interact with role dropdown");
                        throw;
                    }

                    // For the create button
                    Console.WriteLine("Clicking Create button...");
                    try
                    {
                        var waitForCreate = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                        var createButton = waitForCreate.Until(d =>
                        {
                            var element = d.FindElement(
                                By.XPath("//div[@role='dialog']//button[text()='Create']")
                            );
                            return element.Enabled && element.Displayed ? element : null;
                        });

                        createButton.Click();
                        Thread.Sleep(5000); // Wait for API response
                    }
                    catch (WebDriverTimeoutException)
                    {
                        Console.WriteLine("Failed to find or click create button");
                        throw;
                    }

                    // For the snackbar check
                    try
                    {
                        var waitForSnack = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
                        var snackbar = waitForSnack.Until(d =>
                        {
                            try
                            {
                                var element = d.FindElement(
                                    By.XPath(
                                        "//div[contains(@class, 'MuiSnackbar-root')]//div[contains(text(), 'User created!')]"
                                    )
                                );
                                return element.Displayed ? element : null;
                            }
                            catch (StaleElementReferenceException)
                            {
                                return null;
                            }
                        });

                        Console.WriteLine(
                            "Success! Snackbar message appeared confirming user creation."
                        );
                    }
                    catch (WebDriverTimeoutException)
                    {
                        Console.WriteLine(
                            "Failed! Success snackbar did not appear - user creation might have failed"
                        );
                        throw;
                    }

                    // Give Rest
                    Thread.Sleep(1000);

                    // STEP 4: CREATE NEW HR MANAGER USER
                    Console.WriteLine("\n=== STEP 3: CREATE NEW HR MANAGER USER ===");
                    Console.WriteLine("Clicking Create User button...");

                    // Click Create User button
                    createUserButton.Click();

                    // Wait for the dialog to open
                    Thread.Sleep(1000);

                    // For the email field in create user dialog
                    Console.WriteLine("Filling user details...");
                    try
                    {
                        var waitForDialog = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                        var newEmailField = waitForDialog.Until(d =>
                        {
                            var element = d.FindElement(
                                By.XPath(
                                    "//div[@role='dialog']//label[text()='Email']/following-sibling::div/input"
                                )
                            );
                            return element.Enabled && element.Displayed ? element : null;
                        });

                        newEmailField.Clear();
                        Thread.Sleep(500);
                        newEmailField.SendKeys("burnerghost306@gmail.com");
                    }
                    catch (WebDriverTimeoutException)
                    {
                        Console.WriteLine(
                            "Failed to find or interact with email field in create dialog"
                        );
                        throw;
                    }

                    // For the full name field
                    try
                    {
                        var waitForName = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                        var fullNameField = waitForName.Until(d =>
                        {
                            var element = d.FindElement(
                                By.XPath(
                                    "//div[@role='dialog']//label[text()='Full Name']/following-sibling::div/input"
                                )
                            );
                            return element.Enabled && element.Displayed ? element : null;
                        });

                        fullNameField.Clear();
                        Thread.Sleep(500);
                        fullNameField.SendKeys("Burner Ghost");
                    }
                    catch (WebDriverTimeoutException)
                    {
                        Console.WriteLine(
                            "Failed to find or interact with name field in create dialog"
                        );
                        throw;
                    }

                    // For the role dropdown
                    Console.WriteLine("Selecting role...");
                    try
                    {
                        var waitForRole = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                        var roleDropdown = waitForRole.Until(d =>
                        {
                            var element = d.FindElement(
                                By.XPath(
                                    "//div[@role='dialog']//div[contains(@class, 'MuiSelect-select')]"
                                )
                            );
                            return element.Enabled && element.Displayed ? element : null;
                        });

                        roleDropdown.Click();
                        Thread.Sleep(1000);

                        var hrOption = driver.FindElement(
                            By.XPath("//li[@role='option' and text()='HRManager']")
                        );
                        hrOption.Click();
                        Thread.Sleep(1000);
                    }
                    catch (WebDriverTimeoutException)
                    {
                        Console.WriteLine("Failed to find or interact with role dropdown");
                        throw;
                    }

                    // For the create button
                    Console.WriteLine("Clicking Create button...");
                    try
                    {
                        var waitForCreate = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                        var createButton = waitForCreate.Until(d =>
                        {
                            var element = d.FindElement(
                                By.XPath("//div[@role='dialog']//button[text()='Create']")
                            );
                            return element.Enabled && element.Displayed ? element : null;
                        });

                        createButton.Click();
                        Thread.Sleep(5000); // Wait for API response
                    }
                    catch (WebDriverTimeoutException)
                    {
                        Console.WriteLine("Failed to find or click create button");
                        throw;
                    }

                    // For the snackbar check
                    try
                    {
                        var waitForSnack = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
                        var snackbar = waitForSnack.Until(d =>
                        {
                            try
                            {
                                var element = d.FindElement(
                                    By.XPath(
                                        "//div[contains(@class, 'MuiSnackbar-root')]//div[contains(text(), 'User created!')]"
                                    )
                                );
                                return element.Displayed ? element : null;
                            }
                            catch (StaleElementReferenceException)
                            {
                                return null;
                            }
                        });

                        Console.WriteLine(
                            "Success! Snackbar message appeared confirming user creation."
                        );
                    }
                    catch (WebDriverTimeoutException)
                    {
                        Console.WriteLine(
                            "Failed! Success snackbar did not appear - user creation might have failed"
                        );
                        throw;
                    }

                    // Give Rest
                    Thread.Sleep(1000);

                    // STEP 5: CREATE NEW EMPLOYEE USER
                    Console.WriteLine("\n=== STEP 5: CREATE NEW EMPLOYEE USER ===");
                    Console.WriteLine("Clicking Create User button...");

                    // Click Create User button
                    createUserButton.Click();

                    // Wait for the dialog to open
                    Thread.Sleep(1000);

                    // For the email field in create user dialog
                    Console.WriteLine("Filling user details...");
                    try
                    {
                        var waitForDialog = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                        var newEmailField = waitForDialog.Until(d =>
                        {
                            var element = d.FindElement(
                                By.XPath(
                                    "//div[@role='dialog']//label[text()='Email']/following-sibling::div/input"
                                )
                            );
                            return element.Enabled && element.Displayed ? element : null;
                        });

                        newEmailField.Clear();
                        Thread.Sleep(500);
                        newEmailField.SendKeys("keccse21072@kingsedu.ac.in");
                    }
                    catch (WebDriverTimeoutException)
                    {
                        Console.WriteLine(
                            "Failed to find or interact with email field in create dialog"
                        );
                        throw;
                    }

                    // For the full name field
                    try
                    {
                        var waitForName = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                        var fullNameField = waitForName.Until(d =>
                        {
                            var element = d.FindElement(
                                By.XPath(
                                    "//div[@role='dialog']//label[text()='Full Name']/following-sibling::div/input"
                                )
                            );
                            return element.Enabled && element.Displayed ? element : null;
                        });

                        fullNameField.Clear();
                        Thread.Sleep(500);
                        fullNameField.SendKeys("Sales Guy");
                    }
                    catch (WebDriverTimeoutException)
                    {
                        Console.WriteLine(
                            "Failed to find or interact with name field in create dialog"
                        );
                        throw;
                    }

                    // ROLE SELECTION
                    Console.WriteLine("Selecting role...");
                    try
                    {
                        // First select the Role
                        var waitForRole = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                        var roleDropdown = waitForRole.Until(d =>
                        {
                            var element = d.FindElement(
                                By.XPath(
                                    "//div[@role='dialog']//div[contains(@class, 'MuiSelect-select')]"
                                )
                            );
                            return element.Enabled && element.Displayed ? element : null;
                        });

                        roleDropdown.Click();
                        Thread.Sleep(1000);

                        var employeeOption = driver.FindElement(
                            By.XPath("//li[@role='option' and text()='Employee']")
                        );
                        employeeOption.Click();
                        Thread.Sleep(2000); // Wait for department dropdown to appear
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error selecting role: {ex.Message}");
                        throw;
                    }

                    // DEPT SELECTION
                    Console.WriteLine("Selecting department...");
                    try
                    {
                        // First select the Role
                        var waitForRole = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                        var departmentDropdown = wait.Until(d =>
                        {
                            var elements = d.FindElements(
                                By.XPath(
                                    "//div[@role='dialog']//div[contains(@class, 'MuiSelect-select')]"
                                )
                            );
                            return
                                elements.Count > 1 && elements[1].Enabled && elements[1].Displayed
                                ? elements[1]
                                : null;
                        });

                        departmentDropdown.Click();
                        Thread.Sleep(1000);

                        var deptOption = driver.FindElement(
                            By.XPath("//li[@role='option' and text()='Sales']")
                        );
                        deptOption.Click();
                        Thread.Sleep(2000); // Wait for department dropdown to appear
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error selecting role: {ex.Message}");
                        throw;
                    }

                    // For the create button
                    Console.WriteLine("Clicking Create button...");
                    try
                    {
                        var waitForCreate = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                        var createButton = waitForCreate.Until(d =>
                        {
                            var element = d.FindElement(
                                By.XPath("//div[@role='dialog']//button[text()='Create']")
                            );
                            return element.Enabled && element.Displayed ? element : null;
                        });

                        createButton.Click();
                        Thread.Sleep(5000); // Wait for API response
                    }
                    catch (WebDriverTimeoutException)
                    {
                        Console.WriteLine("Failed to find or click create button");
                        throw;
                    }

                    // For the snackbar check
                    try
                    {
                        var waitForSnack = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
                        var snackbar = waitForSnack.Until(d =>
                        {
                            try
                            {
                                var element = d.FindElement(
                                    By.XPath(
                                        "//div[contains(@class, 'MuiSnackbar-root')]//div[contains(text(), 'User created!')]"
                                    )
                                );
                                return element.Displayed ? element : null;
                            }
                            catch (StaleElementReferenceException)
                            {
                                return null;
                            }
                        });

                        Console.WriteLine(
                            "Success! Snackbar message appeared confirming user creation."
                        );
                    }
                    catch (WebDriverTimeoutException)
                    {
                        Console.WriteLine(
                            "Failed! Success snackbar did not appear - user creation might have failed"
                        );
                        throw;
                    }

                    Console.WriteLine("\n=== TEST COMPLETED SUCCESSFULLY! ===");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error occurred: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    Console.ReadKey();
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
