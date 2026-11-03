using SimpX.Net.Application.Shared;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 自助反水 V2 到点切换规则（纯内存，不写库）。
/// </summary>
public sealed class RebateV2CutoverTests
{
    /// <summary>预约到期：False → True + Locked，幂等可再跑。</summary>
    [Fact]
    public void TryApplyCutover_Activates_When_EffectiveAt_Due()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["IsAutoReturn"] = bool.FalseString,
            ["AutoReturnV2Locked"] = bool.FalseString,
            ["AutoReturnV2EffectiveAt"] = "2026-01-01 00:00:00"
        };

        var changed = RebateV2CutoverService.TryApplyCutover(
            map,
            new DateTime(2026, 1, 2, 0, 1, 0),
            out var watermark,
            out var activated);

        Assert.True(changed);
        Assert.True(activated);
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0), watermark);
        Assert.True(SiteSettingQuery.GetBool(map, "IsAutoReturn"));
        Assert.True(SiteSettingQuery.GetBool(map, "AutoReturnV2Locked"));
        Assert.Equal("2026-01-01 00:00:00", map["AutoReturnV2EffectiveAt"]);

        // 已锁定再跑：不再改
        var again = RebateV2CutoverService.TryApplyCutover(
            map,
            new DateTime(2026, 1, 3),
            out _,
            out var activated2);
        Assert.False(again);
        Assert.False(activated2);
    }

    /// <summary>未到期：不翻旗。</summary>
    [Fact]
    public void TryApplyCutover_Skips_When_Not_Due()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["IsAutoReturn"] = bool.FalseString,
            ["AutoReturnV2Locked"] = bool.FalseString,
            ["AutoReturnV2EffectiveAt"] = "2099-01-01 00:00:00"
        };

        var changed = RebateV2CutoverService.TryApplyCutover(
            map,
            DateTime.Now,
            out var watermark,
            out var activated);

        Assert.False(changed);
        Assert.False(activated);
        Assert.Null(watermark);
        Assert.False(SiteSettingQuery.GetBool(map, "IsAutoReturn"));
    }

    /// <summary>已开自助但未锁：补锁并保留/补齐生效点。</summary>
    [Fact]
    public void TryApplyCutover_Locks_When_Already_Auto_Unlocked()
    {
        var now = new DateTime(2026, 3, 10, 15, 0, 0);
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["IsAutoReturn"] = bool.TrueString,
            ["AutoReturnV2Locked"] = bool.FalseString
        };

        var changed = RebateV2CutoverService.TryApplyCutover(map, now, out var watermark, out var activated);

        Assert.True(changed);
        Assert.False(activated);
        Assert.Equal(now.Date, watermark);
        Assert.True(SiteSettingQuery.GetBool(map, "IsAutoReturn"));
        Assert.True(SiteSettingQuery.GetBool(map, "AutoReturnV2Locked"));
        Assert.Equal(RebateV2CutoverService.FormatEffectiveAt(now.Date), map["AutoReturnV2EffectiveAt"]);
    }

    /// <summary>配置总开关解析。</summary>
    [Fact]
    public void IsRuntimeEnabled_Reads_Truthy_Flags()
    {
        Assert.True(RebateV2CutoverService.IsRuntimeEnabled(new MapConfig(
            ("ScheduleHost:AutoReturnV2RuntimeEnabled", "true"))));
        Assert.False(RebateV2CutoverService.IsRuntimeEnabled(new MapConfig(
            ("AppSettings:AutoReturnV2RuntimeEnabled", "0"))));
        Assert.True(RebateV2CutoverService.IsRuntimeEnabled(new MapConfig(
            ("AppSettings:AutoReturnV2RuntimeEnabled", "1"))));
    }

    /// <summary>极简配置替身，只服务 indexer。</summary>
    private sealed class MapConfig : Microsoft.Extensions.Configuration.IConfiguration
    {
        private readonly Dictionary<string, string> _map;

        public MapConfig(params (string Key, string Value)[] pairs)
        {
            _map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (k, v) in pairs)
                _map[k] = v;
        }

        public string this[string key]
        {
            get => _map.TryGetValue(key, out var v) ? v : null;
            set => _map[key] = value;
        }

        public IEnumerable<Microsoft.Extensions.Configuration.IConfigurationSection> GetChildren() =>
            Array.Empty<Microsoft.Extensions.Configuration.IConfigurationSection>();

        public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => new NeverToken();

        public Microsoft.Extensions.Configuration.IConfigurationSection GetSection(string key) =>
            throw new NotSupportedException();

        private sealed class NeverToken : Microsoft.Extensions.Primitives.IChangeToken
        {
            public bool HasChanged => false;
            public bool ActiveChangeCallbacks => false;
            public IDisposable RegisterChangeCallback(Action<object> callback, object state) =>
                Empty.Instance;

            private sealed class Empty : IDisposable
            {
                public static readonly Empty Instance = new();
                public void Dispose() { }
            }
        }
    }
}
