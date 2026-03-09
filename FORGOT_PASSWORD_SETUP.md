# Forgot Password Email Setup Guide

## Overview
The forgot password functionality has been implemented with email verification. Users can request a password reset link via email.

## Features Implemented
? Forgot Password page with email input
? Email service using SMTP (Gmail configured)
? Password reset token generation
? Secure password reset link via email
? Reset Password page with validation
? Confirmation pages for better UX
? 1-hour token expiration for security

---

## Setup Instructions

### 1. Configure Gmail App Password

Since Gmail has deprecated "less secure app access", you need to create an **App Password**:

#### Steps to Get Gmail App Password:

1. **Enable 2-Factor Authentication** on your Gmail account:
   - Go to https://myaccount.google.com/security
   - Click on "2-Step Verification"
   - Follow the setup process

2. **Generate App Password**:
   - Go to https://myaccount.google.com/apppasswords
   - Select app: "Mail"
   - Select device: "Other (Custom name)" ? Enter "EasyCart"
   - Click "Generate"
   - **Copy the 16-character password** (it looks like: `abcd efgh ijkl mnop`)

3. **Update appsettings.json**:
   ```json
   "EmailSettings": {
     "SmtpServer": "smtp.gmail.com",
     "SmtpPort": "587",
     "SenderEmail": "your-email@gmail.com",
     "SenderPassword": "abcd efgh ijkl mnop",
     "SenderName": "EasyCart Support"
   }
   ```

### 2. Alternative: Use Other Email Providers

#### Outlook/Hotmail:
```json
"EmailSettings": {
  "SmtpServer": "smtp-mail.outlook.com",
  "SmtpPort": "587",
  "SenderEmail": "your-email@outlook.com",
  "SenderPassword": "your-password",
  "SenderName": "EasyCart Support"
}
```

#### Custom SMTP Server:
```json
"EmailSettings": {
  "SmtpServer": "mail.yourdomain.com",
  "SmtpPort": "587",
  "SenderEmail": "noreply@yourdomain.com",
  "SenderPassword": "your-password",
  "SenderName": "EasyCart Support"
}
```

---

## Testing the Feature

### Test Flow:

1. **Forgot Password Request**
   - Navigate to `/Account/Login`
   - Click "Forgot Password?"
   - Enter email address
   - Click "Send Reset Link"
   - Should redirect to confirmation page

2. **Check Email**
   - Open the email inbox
   - Look for email from "EasyCart Support"
   - Email contains:
     - Styled HTML template
     - Reset password button/link
     - 1-hour expiration notice

3. **Reset Password**
   - Click the reset link in email
   - Should redirect to `/Account/ResetPassword?email=...&token=...`
   - Enter new password (minimum 6 characters)
   - Confirm password
   - Click "Reset Password"

4. **Login with New Password**
   - Should redirect to success page
   - Click "Login Now"
   - Login with new password

---

## Security Features

? **Token Expiration**: Reset links expire after 1 hour
? **Secure Tokens**: Uses ASP.NET Core Identity token generation
? **URL Encoding**: Tokens are Base64URL encoded
? **SSL/TLS**: Email sent over secure connection (port 587)
? **No User Enumeration**: Same response whether email exists or not

---

## Troubleshooting

### Email Not Sending?

**Problem**: `SmtpException` or timeout error

**Solutions**:
1. Verify Gmail App Password is correct (16 characters, no spaces)
2. Check Gmail 2FA is enabled
3. Ensure firewall allows port 587
4. Try using port 465 with EnableSsl = true
5. Check antivirus isn't blocking SMTP

### Email Going to Spam?

**Solutions**:
1. Use a verified domain email (not Gmail for production)
2. Add SPF, DKIM, DMARC records to domain
3. Warm up email sending gradually
4. Use a dedicated email service (SendGrid, AWS SES, Mailgun)

### Token Expired Error?

**Solution**:
- Link expires after 1 hour for security
- Request a new password reset link
- Complete reset within 1 hour

### Link Not Working?

**Solution**:
- Ensure the URL scheme matches (http vs https)
- Check token encoding/decoding
- Verify user exists in database

---

## Production Recommendations

For production environments, consider using professional email services:

### Recommended Email Services:

1. **SendGrid** (Free tier: 100 emails/day)
   - https://sendgrid.com/
   - NuGet: `SendGrid`

2. **AWS SES** (Very cheap, highly reliable)
   - https://aws.amazon.com/ses/
   - NuGet: `AWSSDK.SimpleEmail`

3. **Mailgun** (Free tier: 5,000 emails/month)
   - https://www.mailgun.com/
   - NuGet: `Mailgun.NET`

4. **Azure Communication Services**
   - https://azure.microsoft.com/en-us/services/communication-services/
   - NuGet: `Azure.Communication.Email`

### Why Use Professional Services?

- ? Better deliverability
- ? Higher sending limits
- ? Built-in analytics
- ? Email templates
- ? Bounce handling
- ? Spam score checking
- ? Domain reputation management

---

## File Structure

```
Controllers/
  ??? AccountController.cs           (Forgot/Reset password actions)

Services/
  ??? IEmailService.cs               (Email service interface)
  ??? EmailService.cs                (SMTP implementation)

Models/
  ??? ForgotPasswordViewModel.cs     (Email input)
  ??? ResetPasswordViewModel.cs      (Password reset form)

Views/Account/
  ??? ForgotPassword.cshtml          (Request reset link)
  ??? ForgotPasswordConfirmation.cshtml  (Check email page)
  ??? ResetPassword.cshtml           (Enter new password)
  ??? ResetPasswordConfirmation.cshtml   (Success page)

appsettings.json                     (Email configuration)
Program.cs                           (Service registration)
```

---

## Email Template Preview

The email sent includes:
- ?? EasyCart branding
- User's name
- "Reset Password" button (styled)
- Security information
- 1-hour expiration notice
- Professional footer

---

## Support

If you encounter issues:
1. Check the console/logs for error messages
2. Verify email settings in appsettings.json
3. Test with a different email provider
4. Ensure firewall/antivirus isn't blocking SMTP

---

## Future Enhancements

Potential improvements:
- [ ] Add rate limiting (prevent spam)
- [ ] Add CAPTCHA on forgot password page
- [ ] Email template customization
- [ ] SMS-based password reset option
- [ ] Password history (prevent reusing old passwords)
- [ ] Account lockout after failed reset attempts
- [ ] Email change verification

---

**Created for EasyCart Project**
Last Updated: February 2026
