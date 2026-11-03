namespace SimpX.Net.Application.Shared;

/// <summary>
/// 返水设定缺口：币种/游戏大类排序字典。
/// </summary>
public sealed partial class RebateSettingService
{
    /// <summary>
    /// 返水展示类型字典。对齐旧 returntype。
    /// </summary>
    /// <returns>键值：AA=综合，A0..=游戏大类</returns>
    public Dictionary<string, string> ReturnType()
    {
        // 对齐旧 GameCategory 枚举文案（去掉 # 号）。
        return new Dictionary<string, string>
        {
            ["AA"] = "综合",
            ["A0"] = "彩票",
            ["A1"] = "电竞",
            ["A2"] = "电子",
            ["A3"] = "真人",
            ["A4"] = "棋牌",
            ["A5"] = "体育",
            ["A6"] = "捕鱼",
            ["A7"] = "动物",
            ["A8"] = "小游戏",
            ["A9"] = "对战"
        };
    }
}
