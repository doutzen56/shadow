using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;

namespace SimpX.Net.Test;

/// <summary>
/// 测试基类
/// </summary>
public class BaseTest : IDisposable
{
    private readonly string _baseUrl = "http://localhost:8888";
    protected readonly EdgeDriver Driver = new();

    protected BaseTest(string token = null)
    {
        var url = _baseUrl;
        if (!string.IsNullOrWhiteSpace(token)) url += $"/#/login?token={token}";
        Driver.Manage().Window.Maximize();
        Driver.Navigate().GoToUrl(url);

        // 隐式等待3秒（隐式等待是元素未被呈现出来，才会等待）
        Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3);
    }

    /// <summary>
    /// 等待网页加载完成
    /// </summary>
    protected async Task WaitExecutorCompleteAsync()
    {
        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(30));
        wait.Until(driver => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete"));
        await Task.Delay(1000);
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="account"></param>
    /// <param name="password"></param>
    protected async Task Login(string account = "superSimpX.Net", string password = "SimpX.Net++010101")
    {
        await GoToUrlAsync("/#/login");
        var inputList = Driver.FindElements(By.CssSelector("#pane-account input"));

        // 输入用户名
        var accountInput = inputList.First();
        accountInput.Clear();
        accountInput.SendKeys(account);

        // 输入密码
        var passwordInput = inputList.Skip(1).First();
        passwordInput.Clear();
        passwordInput.SendKeys(password);

        // 输入验证码
        var captchaInput = inputList.Skip(2).First();
        captchaInput.Clear();
        captchaInput.SendKeys("0");

        // 提交
        var button = Driver.FindElement(By.CssSelector("#pane-account button"));
        button.Click();
    }

    /// <summary>
    /// 打开指定页面
    /// </summary>
    /// <param name="url"></param>
    protected async Task GoToUrlAsync(string url)
    {
        if (url.StartsWith("http")) await Driver.Navigate().GoToUrlAsync(url);
        else await Driver.Navigate().GoToUrlAsync(_baseUrl + "/" + url.TrimStart('/'));
        await WaitExecutorCompleteAsync();
    }

    /// <summary>
    /// 等待用户按回车键继续
    /// </summary>
    /// <param name="text">提示词</param>
    protected void WaitEnter(string text = "等待用户按回车键继续...")
    {
        Console.WriteLine(text);
        Console.ReadLine();
    }

    public void Dispose()
    {
        Driver.Quit();
        Driver.Dispose();
    }
}