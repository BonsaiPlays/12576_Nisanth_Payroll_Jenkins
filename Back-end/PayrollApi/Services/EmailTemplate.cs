using System;

namespace PayrollApi.Services
{
    public static class EmailTemplate
    {
        public static string Build(
            string title,
            string message,
            string? actionText = null,
            string? actionUrl = null,
            string? status = null // e.g. "Action Required", "Info", "Approved"
        )
        {
            var buttonHtml = string.Empty;
            if (!string.IsNullOrEmpty(actionText) && !string.IsNullOrEmpty(actionUrl))
            {
                buttonHtml =
                    $@"
                  <p style='margin:25px 0; text-align:center;'>
                    <a href='{actionUrl}' style='
                      display:inline-block;
                      padding:12px 24px;
                      background:#0d6efd;
                      color:#ffffff !important;
                      text-decoration:none;
                      border-radius:6px;
                      font-size:15px;
                      font-weight:bold;
                      font-family:Arial, sans-serif;'>
                      {actionText}
                    </a>
                  </p>";
            }

            var statusHtml = string.Empty;
            if (!string.IsNullOrEmpty(status))
            {
                statusHtml =
                    $@"
                  <div style='
                    background:#e9ecef;
                    color:#212529;
                    font-size:13px;
                    text-align:center;
                    padding:6px;
                    border-bottom:1px solid #dee2e6;'>
                    {status}
                  </div>";
            }

            return $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='UTF-8'>
  <meta name='viewport' content='width=device-width, initial-scale=1.0'/>
</head>
<body style='background-color:#f8f9fa; margin:0; padding:15px;'>
  <div style='
    max-width:600px; 
    margin:auto; 
    background:#ffffff; 
    border-radius:8px; 
    overflow:hidden; 
    border:1px solid #ddd; 
    box-shadow:0 2px 6px rgba(0,0,0,0.08);
    font-family:Arial, sans-serif;'>

    <!-- HEADER -->
    <div style='background:#343a40; color:#fff; padding:18px; text-align:center;'>
      <span style='font-size:22px; font-weight:bold;'>Payroll System</span>
    </div>

    <!-- STATUS STRIP -->
    {statusHtml}

    <!-- CONTENT -->
    <div style='padding:25px; color:#212529; line-height:1.6;'>
      <h2 style='font-size:20px; margin-top:0; margin-bottom:16px; color:#0d6efd;'>{title}</h2>
      <p style='margin:0; font-size:15px;'>{message}</p>
      {buttonHtml}
    </div>

    <!-- FOOTER -->
    <div style='background:#f1f1f1; padding:12px; text-align:center; font-size:12px; color:#6c757d;'>
      © {DateTime.UtcNow.Year} Payroll System — All rights reserved.
    </div>
  </div>
</body>
</html>";
        }
    }
}
