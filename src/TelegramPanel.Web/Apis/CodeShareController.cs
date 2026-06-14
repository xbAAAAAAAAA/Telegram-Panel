using Microsoft.AspNetCore.Mvc;
using TelegramPanel.Data;
using TelegramPanel.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace TelegramPanel.Web.Apis;

/// <summary>
/// 接码链接管理 API（需要认证）
/// 用于生成、管理和禁用分享链接
/// </summary>
[ApiController]
[Route("api/code-share")]
public class CodeShareController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public CodeShareController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// 为账号生成接码分享链接
    /// </summary>
    [HttpPost("generate/{accountId}")]
    public async Task<IActionResult> GenerateShareLink(int accountId)
    {
        var account = await _db.Accounts.FindAsync(accountId);
        if (account == null)
            return NotFound(new { error = "账号不存在" });

        // 检查是否已存在分享链接
        var existing = await _db.AccountCodeShares
            .FirstOrDefaultAsync(x => x.AccountId == accountId);

        if (existing != null)
        {
            // 返回现有链接
            var existingUrl = $"{Request.Scheme}://{Request.Host}/api/external/sms-share/view/{existing.ShareToken}";
            return Ok(new
            {
                shareToken = existing.ShareToken,
                shareUrl = existingUrl,
                isNew = false,
                isEnabled = existing.IsEnabled,
                createdAt = existing.CreatedAt
            });
        }

        // 生成新链接
        var shareToken = Guid.NewGuid().ToString("N").Substring(0, 32);
        var share = new AccountCodeShare
        {
            AccountId = accountId,
            ShareToken = shareToken,
            CreatedAt = DateTime.UtcNow
        };

        _db.AccountCodeShares.Add(share);
        await _db.SaveChangesAsync();

        var shareUrl = $"{Request.Scheme}://{Request.Host}/api/external/sms-share/view/{shareToken}";
        return Ok(new
        {
            shareToken,
            shareUrl,
            isNew = true,
            isEnabled = share.IsEnabled,
            createdAt = share.CreatedAt
        });
    }

    /// <summary>
    /// 禁用分享链接
    /// </summary>
    [HttpPost("disable/{accountId}")]
    public async Task<IActionResult> DisableShareLink(int accountId)
    {
        var share = await _db.AccountCodeShares
            .FirstOrDefaultAsync(x => x.AccountId == accountId);

        if (share == null)
            return NotFound(new { error = "分享链接不存在" });

        share.IsEnabled = false;
        _db.AccountCodeShares.Update(share);
        await _db.SaveChangesAsync();

        return Ok(new { message = "链接已禁用", isEnabled = false });
    }

    /// <summary>
    /// 启用分享链接
    /// </summary>
    [HttpPost("enable/{accountId}")]
    public async Task<IActionResult> EnableShareLink(int accountId)
    {
        var share = await _db.AccountCodeShares
            .FirstOrDefaultAsync(x => x.AccountId == accountId);

        if (share == null)
            return NotFound(new { error = "分享链接不存在" });

        share.IsEnabled = true;
        _db.AccountCodeShares.Update(share);
        await _db.SaveChangesAsync();

        var shareUrl = $"{Request.Scheme}://{Request.Host}/api/external/sms-share/view/{share.ShareToken}";
        return Ok(new { message = "链接已启用", shareUrl, isEnabled = true });
    }

    /// <summary>
    /// 获取账号的分享链接信息
    /// </summary>
    [HttpGet("info/{accountId}")]
    public async Task<IActionResult> GetShareInfo(int accountId)
    {
        var share = await _db.AccountCodeShares
            .FirstOrDefaultAsync(x => x.AccountId == accountId);

        if (share == null)
            return NotFound(new { error = "此账号还未生成分享链接" });

        var shareUrl = $"{Request.Scheme}://{Request.Host}/api/external/sms-share/view/{share.ShareToken}";
        return Ok(new
        {
            shareToken = share.ShareToken,
            shareUrl,
            isEnabled = share.IsEnabled,
            lastVerificationCode = share.LastVerificationCode,
            last2FaCode = share.Last2FaCode,
            lastCodeReceivedAt = share.LastCodeReceivedAt,
            createdAt = share.CreatedAt
        });
    }

    /// <summary>
    /// 删��分享链接
    /// </summary>
    [HttpDelete("delete/{accountId}")]
    public async Task<IActionResult> DeleteShareLink(int accountId)
    {
        var share = await _db.AccountCodeShares
            .FirstOrDefaultAsync(x => x.AccountId == accountId);

        if (share == null)
            return NotFound(new { error = "分享链接不存在" });

        _db.AccountCodeShares.Remove(share);
        await _db.SaveChangesAsync();

        return Ok(new { message = "链接已删除" });
    }

    /// <summary>
    /// 重新生成分享链接（生成新的token）
    /// </summary>
    [HttpPost("regenerate/{accountId}")]
    public async Task<IActionResult> RegenerateShareLink(int accountId)
    {
        var share = await _db.AccountCodeShares
            .FirstOrDefaultAsync(x => x.AccountId == accountId);

        if (share == null)
            return NotFound(new { error = "分享链接不存在，请先生成" });

        // 生成新的 token
        var newShareToken = Guid.NewGuid().ToString("N").Substring(0, 32);
        share.ShareToken = newShareToken;
        share.CreatedAt = DateTime.UtcNow;
        share.IsEnabled = true;

        _db.AccountCodeShares.Update(share);
        await _db.SaveChangesAsync();

        var shareUrl = $"{Request.Scheme}://{Request.Host}/api/external/sms-share/view/{newShareToken}";
        return Ok(new
        {
            message = "链接已重新生成",
            shareToken = newShareToken,
            shareUrl,
            isEnabled = true,
            createdAt = share.CreatedAt
        });
    }

    /// <summary>
    /// 列出所有分享链接（仅限某个账号或所有账号）
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> ListShareLinks(int? accountId = null)
    {
        IQueryable<AccountCodeShare> query = _db.AccountCodeShares.Include(x => x.Account);

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId);

        var shares = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();

        var result = shares.Select(s => new
        {
            s.Id,
            s.AccountId,
            s.Account.Phone,
            s.Account.DisplayPhone,
            s.Account.Nickname,
            shareUrl = $"{Request.Scheme}://{Request.Host}/api/external/sms-share/view/{s.ShareToken}",
            s.IsEnabled,
            s.LastVerificationCode,
            s.Last2FaCode,
            s.LastCodeReceivedAt,
            s.CreatedAt
        });

        return Ok(new
        {
            total = result.Count(),
            data = result
        });
    }
}
