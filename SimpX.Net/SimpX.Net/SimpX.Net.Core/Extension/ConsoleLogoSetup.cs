namespace SimpX.Net.Core;

/// <summary>
/// 启动时在控制台打印 SimpX 标识。
/// </summary>
public static class ConsoleLogoSetup
{
    /// <summary>
    /// 注册启动 Logo（调用时直接打印到控制台）。
    /// </summary>
    /// <param name="services">服务集合</param>
    public static void AddConsoleLogo(this IServiceCollection services)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
 ____  _                 __  __
/ ___|(_)_ __ ___  _ __  \ \/ /
\___ \| | '_ ` _ \| '_ \  \  /
 ___) | | | | | | | |_) | /  \
|____/|_|_| |_| |_| .__/ /_/\_\
                  |_|
");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("SimpX");
        Console.ResetColor();
    }
}
