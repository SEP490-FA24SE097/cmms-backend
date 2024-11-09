
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace CMMS.API.Services
{
    public interface IMailService
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }
    public class MailService : IMailService
    {
        public MailService()
        {

        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var emailToSend = new MimeMessage();
            emailToSend.From.Add(new MailboxAddress("CMMS", "dat.nt271102@gmail.com"));
            emailToSend.To.Add(MailboxAddress.Parse(email));
            emailToSend.Subject = subject;

            string htmlBody =
            @"<!DOCTYPE html>
<html lang=""en"">

<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>

<body>
    <div id=""wrapper"" style=""display: flex; max-width:600px; margin: 0 auto;"">
        <div class=""form"" style=""
        width: 600px;
        height: 800px;
        background-color: #efc15f5c;"">
            <div class=""header"" style=""margin-top: 50px;"">
                <div class=""logo""
                    style=""height: 90px; width: 100%;  margin: 10px auto; border-radius: 10px; display: flex; align-items: center; justify-content: center;"">
                    <img src=""https://firebasestorage.googleapis.com/v0/b/bmosfile.appspot.com/o/images%2Flogo.png?alt=media&token=76b0e3c4-089d-404f-8c6c-ac6b2acef832""
                        alt=""Logo"" srcset="""" style=""width: 100%; height: 100%; margin: 10px 5px 15px 60px;"">
                </div>
                <div class=""text"" style=""text-align: center;"">
                    <h1>Chào mừng bạn đến với Unimarket</h1>
                    <a href=" + htmlMessage + @" 
                    style=""border: 1px solid #039a21;
                    background-color: #039a21;
                    color: #FFFFFF;
                    font-size: 12px;
                    font-weight: bold;
                    padding: 12px 45px;
                    letter-spacing: 1px;
                    text-transform: uppercase;
                    transition: transform 80ms ease-in;
                    width: 200px;
                    margin: 0 auto;
                    display: block;
                    font-size: 24px;
                    text-decoration: none;"">
                    Xác nhận
                    </a>
                    <div class=""headertext"" style=""margin-top: 25px; margin-bottom: 10px;"">
                        <span>(Chúng tôi cần xác thực địa chỉ email của bạn để kích hoạt tài khoản)</span>
                    </div>
                </div>
            </div>

            <div class=""footer"" style=""display: flex;  margin-top: 200px;"">
                <div class=""footercontent"" style=""width: 480px; text-align: center; margin: 0px auto;"">
                    <div class=""span1"" style=""margin-top: 10px; margin-bottom: 10px;"">
                        <span>Copyright © 2024 Unimarket, Allrights reserved.</span>
                    </div>
                    <div><b>Địa chỉ của chúng tôi:</b></div>
                    <div class=""span2"" style=""margin-top: 10px;"">
                        <span>C11-01, số 473 Man Thiện, Thủ Đức, KDT Geleximco Lê Trọng Tấn, Phường Dương Nội, Quận Hà
                            Đông, Hà Nội</span>
                    </div>
                    <div class=""span3"" style=""margin-top: 10px;"">
                        <span>Email: Unimarket8888@gmail.com</span>
                    </div>
                    <div class=""span4"" style=""margin-top: 10px;""><span>Hotline: 0979500611</span></div>
                </div>
            </div>
        </div>
    </div>
</body>

</html>";

            emailToSend.Body = new TextPart(TextFormat.Html)
            {
                Text = htmlBody
            };
            using (var emailClient = new SmtpClient())
            {
                emailClient.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                emailClient.Authenticate("dat.nt271102@gmail.com", "vtozcgeslnwbvxqw");
                emailClient.Send(emailToSend);
                emailClient.Disconnect(true);
            }
            return Task.CompletedTask;
        }
    }
}