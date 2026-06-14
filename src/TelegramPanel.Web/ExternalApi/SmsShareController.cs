using Microsoft.AspNetCore.Mvc;
using TelegramPanel.Data;
using TelegramPanel.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace TelegramPanel.Web.ExternalApi;

/// <summary>
/// 外部接码分享 API（无需身份认证）
/// 用于分享链接访问验证码和2FA信息
/// </summary>
[ApiController]
[Route("api/external/sms-share")]
public class SmsShareController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public SmsShareController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// 获取接码链接的 HTML 页面（无需认证）
    /// 访问地址：https://yourhost.com/api/external/sms-share/view/{shareToken}
    /// </summary>
    [HttpGet("view/{shareToken}")]
    public async Task<IActionResult> GetCodeView(string shareToken)
    {
        var share = await _db.AccountCodeShares
            .Include(x => x.Account)
            .FirstOrDefaultAsync(x => x.ShareToken == shareToken && x.IsEnabled);

        if (share == null)
        {
            return NotFound(GenerateErrorPage("链接已失效或不存在"));
        }

        var html = GenerateCodePageHtml(share);
        return Content(html, "text/html; charset=utf-8");
    }

    /// <summary>
    /// 获取验证码数据（JSON API，供前端轮询）
    /// </summary>
    [HttpGet("data/{shareToken}")]
    public async Task<IActionResult> GetCodeData(string shareToken)
    {
        var share = await _db.AccountCodeShares
            .FirstOrDefaultAsync(x => x.ShareToken == shareToken && x.IsEnabled);

        if (share == null)
            return NotFound(new { error = "链接已失效" });

        return Ok(new
        {
            verificationCode = share.LastVerificationCode,
            twoFaCode = share.Last2FaCode,
            lastReceivedAt = share.LastCodeReceivedAt,
            isReceived = !string.IsNullOrEmpty(share.LastVerificationCode)
        });
    }

    /// <summary>
    /// 生成自定义样式的 HTML 页面
    /// </summary>
    private string GenerateCodePageHtml(AccountCodeShare share)
    {
        var pageTitle = _config["SmsShare:PageTitle"] ?? "Telegram 验证码接收";
        var announcement = _config["SmsShare:Announcement"] ?? "请勿分享此链接给他人";
        var notes = _config["SmsShare:Notes"] ?? "验证码有效期通常为 5-15 分钟，接收后自动失效";
        var contactInfo = _config["SmsShare:ContactInfo"] ?? "";
        var themeColor = _config["SmsShare:ThemeColor"] ?? "#0088cc";

        var hasCode = !string.IsNullOrEmpty(share.LastVerificationCode);
        var codeStatusClass = hasCode ? "received" : "waiting";
        var codeStatusText = hasCode ? "✓ 已接收" : "⏳ 等待中...";

        return $@"
<!DOCTYPE html>
<html lang=""zh-CN"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{pageTitle}</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: linear-gradient(135deg, {themeColor} 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
            padding: 20px;
        }}
        .container {{
            background: white;
            border-radius: 12px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            padding: 40px;
            max-width: 500px;
            width: 100%;
        }}
        .header {{
            text-align: center;
            margin-bottom: 30px;
        }}
        .header h1 {{
            font-size: 24px;
            color: #333;
            margin-bottom: 10px;
        }}
        .header p {{
            font-size: 14px;
            color: #999;
        }}
        .announcement {{
            background: #fff3cd;
            border: 1px solid #ffc107;
            border-radius: 6px;
            padding: 15px;
            margin-bottom: 20px;
            color: #856404;
            font-size: 14px;
            font-weight: 500;
        }}
        .code-section {{
            background: #f8f9fa;
            border: 2px solid {themeColor};
            border-radius: 8px;
            padding: 20px;
            margin-bottom: 20px;
            text-align: center;
        }}
        .code-label {{
            font-size: 12px;
            color: #999;
            margin-bottom: 10px;
            text-transform: uppercase;
            letter-spacing: 1px;
        }}
        .code-value {{
            font-size: 36px;
            font-weight: bold;
            color: {themeColor};
            font-family: 'Monaco', 'Courier New', monospace;
            letter-spacing: 6px;
            word-break: break-all;
            min-height: 50px;
            display: flex;
            align-items: center;
            justify-content: center;
        }}
        .code-value.empty {{
            font-size: 16px;
            color: #ccc;
            letter-spacing: normal;
        }}
        .status {{
            text-align: center;
            padding: 15px;
            font-size: 14px;
            border-radius: 6px;
            margin-bottom: 20px;
        }}
        .status.waiting {{
            background: #fff8e1;
            color: #f57f17;
        }}
        .status.received {{
            background: #e8f5e9;
            color: #2e7d32;
        }}
        .timestamp {{
            text-align: center;
            font-size: 12px;
            color: #999;
            margin-bottom: 20px;
        }}
        .notes {{
            background: #e7f3ff;
            border-left: 4px solid {themeColor};
            padding: 15px;
            margin-bottom: 20px;
            font-size: 13px;
            color: #004085;
            line-height: 1.6;
        }}
        .contact {{
            text-align: center;
            padding-top: 20px;
            border-top: 1px solid #eee;
            font-size: 13px;
            color: #666;
        }}
        .contact-label {{
            display: block;
            margin-bottom: 8px;
            font-weight: 500;
        }}
        .contact-info {{
            display: block;
            color: {themeColor};
            font-weight: 600;
            margin-top: 8px;
        }}
        .refresh-hint {{
            text-align: center;
            font-size: 12px;
            color: #bbb;
            margin-top: 20px;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>{pageTitle}</h1>
            <p>账号: {share.Account.DisplayPhone}</p>
        </div>
        
        <div class=""announcement"">
            ⚠️ {announcement}
        </div>

        <div class=""code-section"">
            <div class=""code-label"">验证码</div>
            <div class=""code-value {(hasCode ? "" : "empty")}"" id=""verificationCode"">
                {(hasCode ? share.LastVerificationCode : "等待接收...")}
            </div>
        </div>

        {(string.IsNullOrEmpty(share.Last2FaCode) ? "" : $@"
        <div class=""code-section"">
            <div class=""code-label"">2FA 双因素认证</div>
            <div class=""code-value"" id=""twoFaCode"">
                {share.Last2FaCode}
            </div>
        </div>
        ")}

        <div class=""status {codeStatusClass}"" id=""status"">
            {codeStatusText}
        </div>

        {(share.LastCodeReceivedAt.HasValue ? $@"
        <div class=""timestamp"">
            最后接收时间: {share.LastCodeReceivedAt:yyyy-MM-dd HH:mm:ss} (UTC)
        </div>
        " : "")}

        <div class=""notes"">
            <strong>📋 使用说明:</strong><br>
            {notes}
        </div>

        {(string.IsNullOrEmpty(contactInfo) ? "" : $@"
        <div class=""contact"">
            <span class=""contact-label"">如有问题，请联系:</span>
            <span class=""contact-info"">{contactInfo}</span>
        </div>
        ")}

        <div class=""refresh-hint"">
            💡 此页面每 3 秒自动刷新一次
        </div>
    </div>

    <script>
        // 自动刷新验证码（每 3 秒）
        function refreshCode() {{
            fetch('/api/external/sms-share/data/{share.ShareToken}')
                .then(response => response.json())
                .then(data => {{
                    if (data.verificationCode) {{
                        document.getElementById('verificationCode').textContent = data.verificationCode;
                        document.getElementById('verificationCode').classList.remove('empty');
                    }}
                    if (data.twoFaCode) {{
                        const twoFaDiv = document.getElementById('twoFaCode');
                        if (twoFaDiv) {{
                            twoFaDiv.textContent = data.twoFaCode;
                        }}
                    }}
                    
                    const statusDiv = document.getElementById('status');
                    if (data.isReceived) {{
                        statusDiv.textContent = '✓ 已接收';
                        statusDiv.classList.remove('waiting');
                        statusDiv.classList.add('received');
                    }}
                }})
                .catch(err => console.error('刷新失败:', err));
        }}
        
        // 初始化和自动刷新
        setInterval(refreshCode, 3000);
    </script>
</body>
</html>
";
    }

    private string GenerateErrorPage(string message)
    {
        return $@"
<!DOCTYPE html>
<html lang=""zh-CN"">
<head>
    <meta charset=""UTF-8"">
    <title>错误</title>
    <style>
        body {{ font-family: sans-serif; display: flex; justify-content: center; align-items: center; min-height: 100vh; background: #f5f5f5; }}
        .error {{ background: white; padding: 40px; border-radius: 8px; text-align: center; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        h1 {{ color: #d32f2f; margin-bottom: 10px; }}
    </style>
</head>
<body>
    <div class=""error"">
        <h1>❌ {message}</h1>
    </div>
</body>
</html>
";
    }
}
