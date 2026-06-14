namespace TelegramPanel.Data.Entities;

/// <summary>
/// 账号接码链接分享记录
/// </summary>
public class AccountCodeShare
{
    public int Id { get; set; }
    
    /// <summary>
    /// 关联的账号 ID
    /// </summary>
    public int AccountId { get; set; }
    
    /// <summary>
    /// 唯一分享令牌（用于生成分享链接）
    /// </summary>
    public string ShareToken { get; set; } = null!;
    
    /// <summary>
    /// 最后一次接收到的验证码
    /// </summary>
    public string? LastVerificationCode { get; set; }
    
    /// <summary>
    /// 最后一次接收到的 2FA 代码
    /// </summary>
    public string? Last2FaCode { get; set; }
    
    /// <summary>
    /// 最后一次接收验证码的时间
    /// </summary>
    public DateTime? LastCodeReceivedAt { get; set; }
    
    /// <summary>
    /// 是否启用此分享链接
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // 导航属性
    public Account Account { get; set; } = null!;
}
