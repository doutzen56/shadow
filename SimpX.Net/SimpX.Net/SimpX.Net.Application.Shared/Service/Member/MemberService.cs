namespace SimpX.Net.Application.Shared;

/// <summary>
/// 会员领域服务（无 DynamicApi）。WebApi 与 Admin 共用写库与校验规则。
/// </summary>
public sealed class MemberService : ITransient
{
    private readonly SqlSugarRepository<Member> _memberRep;
    private readonly ICurrentTenantAccessor _currentTenant;

    /// <summary>
    /// 注入会员仓储与当前租户。
    /// </summary>
    /// <param name="memberRep">会员仓储</param>
    /// <param name="currentTenant">当前请求已定好的租户</param>
    public MemberService(SqlSugarRepository<Member> memberRep, ICurrentTenantAccessor currentTenant)
    {
        _memberRep = memberRep;
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 注册会员。按域名确定租户的进程要先解析到租户。
    /// </summary>
    /// <param name="input">账号、密码、昵称</param>
    /// <returns>新会员 Id</returns>
    public async Task<long> Register(MemberRegisterInput input)
    {
        var account = input.Account?.Trim();
        if (string.IsNullOrWhiteSpace(account))
            throw Oops.Oh("账号不能为空");
        if (await _memberRep.IsAnyAsync(u => u.Account == account))
            throw Oops.Oh("账号已存在");

        var tenantId = RequireTenantIdForMemberWrite();
        var member = new Member
        {
            TenantId = tenantId,
            Account = account,
            Password = CryptogramUtil.Encrypt(input.Password),
            NickName = string.IsNullOrWhiteSpace(input.NickName) ? account : input.NickName.Trim(),
            Status = StatusEnum.Enable
        };
        var created = await _memberRep.AsInsertable(member).ExecuteReturnEntityAsync();
        return created.Id;
    }

    /// <summary>
    /// 校验账号密码，通过则返回会员实体（令牌由调用方签发）。
    /// </summary>
    /// <param name="input">账号、密码</param>
    /// <returns>会员</returns>
    public async Task<Member> Authenticate(MemberLoginInput input)
    {
        var account = input.Account?.Trim();
        var member = await _memberRep.GetFirstAsync(u => u.Account == account)
            ?? throw Oops.Oh("账号或密码错误");

        if (member.Status == StatusEnum.Disable)
            throw Oops.Oh("账号已禁用");

        var plain = CryptogramUtil.Decrypt(member.Password);
        if (plain != input.Password)
            throw Oops.Oh("账号或密码错误");

        return member;
    }

    /// <summary>
    /// 按 Id 取会员；不存在则失败。
    /// </summary>
    /// <param name="memberId">会员 Id</param>
    /// <returns>会员</returns>
    public async Task<Member> GetRequired(long memberId)
    {
        return await _memberRep.GetByIdAsync(memberId) ?? throw Oops.Oh("会员不存在");
    }

    /// <summary>
    /// 转成对外资料。
    /// </summary>
    /// <param name="member">会员</param>
    /// <returns>资料</returns>
    public static MemberProfileOutput ToProfile(Member member)
    {
        return new MemberProfileOutput
        {
            Id = member.Id,
            Account = member.Account,
            NickName = member.NickName,
            Phone = member.Phone,
            Status = member.Status,
            TenantId = member.TenantId
        };
    }

    /// <summary>
    /// 会员自己改昵称、手机号。
    /// </summary>
    /// <param name="memberId">当前登录会员 Id</param>
    /// <param name="input">昵称、手机号</param>
    public async Task UpdateOwnProfile(long memberId, MemberUpdateProfileInput input)
    {
        var member = await GetRequired(memberId);
        if (!string.IsNullOrWhiteSpace(input.NickName))
            member.NickName = input.NickName.Trim();
        if (input.Phone != null)
            member.Phone = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim();
        await _memberRep.AsUpdateable(member)
            .UpdateColumns(u => new { u.NickName, u.Phone, u.UpdateTime })
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 后台分页查会员（当前租户过滤由仓储过滤器处理）。
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>分页结果</returns>
    public async Task<SqlSugarPagedList<MemberProfileOutput>> Page(PageMemberInput input)
    {
        return await _memberRep.AsQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(input.Account), u => u.Account.Contains(input.Account.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Phone), u => u.Phone.Contains(input.Phone.Trim()))
            .WhereIF(input.Status.HasValue, u => u.Status == input.Status.Value)
            .OrderBy(u => u.CreateTime, OrderByType.Desc)
            .Select(u => new MemberProfileOutput
            {
                Id = u.Id,
                Account = u.Account,
                NickName = u.NickName,
                Phone = u.Phone,
                Status = u.Status,
                TenantId = u.TenantId
            })
            .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 后台启用或停用会员。
    /// </summary>
    /// <param name="input">会员 Id 与目标状态</param>
    public async Task SetStatus(MemberSetStatusInput input)
    {
        var member = await GetRequired(input.Id);
        member.Status = input.Status;
        await _memberRep.AsUpdateable(member)
            .UpdateColumns(u => new { u.Status, u.UpdateTime })
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 后台重置会员密码。
    /// </summary>
    /// <param name="input">会员 Id 与新密码</param>
    public async Task ResetPassword(MemberResetPasswordInput input)
    {
        var member = await GetRequired(input.Id);
        member.Password = CryptogramUtil.Encrypt(input.Password);
        await _memberRep.AsUpdateable(member)
            .UpdateColumns(u => new { u.Password, u.UpdateTime })
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 写会员数据时要已有当前租户（按域名解析到的，或登录 Claim 写入的）。
    /// </summary>
    /// <returns>租户 Id</returns>
    private long RequireTenantIdForMemberWrite()
    {
        if (_currentTenant.TenantId is > 0 and var tid)
            return tid;
        throw Oops.Oh(ErrorCodeEnum.Z1005);
    }
}
