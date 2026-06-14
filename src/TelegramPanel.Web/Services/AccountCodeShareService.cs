using TelegramPanel.Data;
using TelegramPanel.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace TelegramPanel.Web.Services;

/// <summary>
/// 账号接码分享服务
/// 用于在接收验证码和2FA时自动更新分享链接中的信息
/// </summary>
public class AccountCodeShareService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AccountCodeShareService> _logger;

    public AccountCodeShareService(AppDbContext db, ILogger<AccountCodeShareService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// 当接收到验证码时调用此方法
    /// </summary>
    public async Task UpdateVerificationCodeAsync(int accountId, string verificationCode)
    {
        try
        {
            var share = await _db.AccountCodeShares
                .FirstOrDefaultAsync(x => x.AccountId == accountId && x.IsEnabled);

            if (share != null)
            {
                share.LastVerificationCode = verificationCode;
                share.LastCodeReceivedAt = DateTime.UtcNow;
                _db.AccountCodeShares.Update(share);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"已更新账号 {accountId} 的验证码到分享链接");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"更新验证码失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 当接收到2FA代码时调用此方法
    /// </summary>
    public async Task Update2FaCodeAsync(int accountId, string twoFaCode)
    {
        try
        {
            var share = await _db.AccountCodeShares
                .FirstOrDefaultAsync(x => x.AccountId == accountId && x.IsEnabled);

            if (share != null)
            {
                share.Last2FaCode = twoFaCode;
                share.LastCodeReceivedAt = DateTime.UtcNow;
                _db.AccountCodeShares.Update(share);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"已更新账号 {accountId} 的2FA代码到分享链接");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"更新2FA代码失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 同时更新验证码和2FA代码
    /// </summary>
    public async Task UpdateCodesAsync(int accountId, string? verificationCode = null, string? twoFaCode = null)
    {
        try
        {
            var share = await _db.AccountCodeShares
                .FirstOrDefaultAsync(x => x.AccountId == accountId && x.IsEnabled);

            if (share != null)
            {
                if (!string.IsNullOrEmpty(verificationCode))
                    share.LastVerificationCode = verificationCode;

                if (!string.IsNullOrEmpty(twoFaCode))
                    share.Last2FaCode = twoFaCode;

                share.LastCodeReceivedAt = DateTime.UtcNow;
                _db.AccountCodeShares.Update(share);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"已更新账号 {accountId} 的验证码和2FA代码到分享链接");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"更新验证码失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 清除分享链接中的验证码（登录成功后调用）
    /// </summary>
    public async Task ClearCodesAsync(int accountId)
    {
        try
        {
            var share = await _db.AccountCodeShares
                .FirstOrDefaultAsync(x => x.AccountId == accountId && x.IsEnabled);

            if (share != null)
            {
                share.LastVerificationCode = null;
                share.Last2FaCode = null;
                _db.AccountCodeShares.Update(share);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"已清除账号 {accountId} 的分享链接中的验证码");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"清除验证码失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 禁用账号的分享链接（踢出设备时调用）
    /// </summary>
    public async Task DisableShareLinkAsync(int accountId)
    {
        try
        {
            var share = await _db.AccountCodeShares
                .FirstOrDefaultAsync(x => x.AccountId == accountId);

            if (share != null)
            {
                share.IsEnabled = false;
                _db.AccountCodeShares.Update(share);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"已禁用账号 {accountId} 的分享链接");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"禁用分享链接失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取账号的分享链接
    /// </summary>
    public async Task<AccountCodeShare?> GetShareLinkAsync(int accountId)
    {
        try
        {
            return await _db.AccountCodeShares
                .FirstOrDefaultAsync(x => x.AccountId == accountId);
        }
        catch (Exception ex)
        {
            _logger.LogError($"获取分享链接失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 检查账号是否有启用的分享链接
    /// </summary>
    public async Task<bool> HasEnabledShareLinkAsync(int accountId)
    {
        try
        {
            return await _db.AccountCodeShares
                .AnyAsync(x => x.AccountId == accountId && x.IsEnabled);
        }
        catch (Exception ex)
        {
            _logger.LogError($"检查分享链接失败: {ex.Message}");
            return false;
        }
    }
}
