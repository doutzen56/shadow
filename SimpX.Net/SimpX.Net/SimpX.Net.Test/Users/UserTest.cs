using OpenQA.Selenium;
using Xunit;

namespace SimpX.Net.Test.Users;

public class UserTest : BaseTest
{
    // 用户登录 token
    private static readonly string Token = "xxxxxxxxx";

    public UserTest() : base(Token)
    {
    }

    [Fact]
    public async Task Login_User()
    {
        await base.Login();
        WaitEnter();
    }

    [Fact]
    public async Task AddUser()
    {
        await Task.Delay(1000);

        await GoToUrlAsync("/#/system/user");
        var addBut = Driver.FindElement(By.XPath("//*[@id=\"app\"]/section/section/div/div[1]/div/main/div/div[1]/div/div[1]/div[1]/div[1]/div[3]/div[1]/div/form/div[6]/div/button"));
        addBut.Click();

        //点击基础信息选项卡
        await Task.Delay(1000);
        Driver.FindElement(By.Id("tab-0")).Click();

        var tab = Driver.FindElement(By.Id("pane-0"));
        var formItemList = tab.FindElements(By.CssSelector("input"));

        // 输入 账号名称
        var first = formItemList.First();
        first.Clear();
        first.SendKeys("test1");
        await Task.Delay(1000);

        // 输入 手机号码
        var second = formItemList.Skip(1).First();
        second.Clear();
        second.SendKeys("17396157893");
        await Task.Delay(1000);

        // 输入 姓名
        var third = formItemList.Skip(2).First();
        third.Clear();
        third.SendKeys("测试1");
        await Task.Delay(1000);

        // 阻塞
        WaitEnter();
    }
}