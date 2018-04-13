using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace AutoTransfer.Utility
{
    public class EmailRelated
    {
        private string host = ConfigurationManager.AppSettings["host"];
        private int port = int.Parse(ConfigurationManager.AppSettings["port"]);
        private string mailFrom = ConfigurationManager.AppSettings["mailFrom"];
        private string NetworkCredentialPassword = ConfigurationManager.AppSettings["NetworkCredentialPassword"];
        private string mailTo = ConfigurationManager.AppSettings["mailTo"];

        public void SendEmail(string subject, string body, string fileName)
        {
            bool enableSSL = true;
            
            string[] emailTo = mailTo.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            using (MailMessage mail = new MailMessage())
            {
                mail.From = new MailAddress(mailFrom);
                for (int i = 0; i < emailTo.Length; i++)
                {
                    if (!string.IsNullOrEmpty(emailTo[i].Trim()))
                    {
                        mail.To.Add(new MailAddress(emailTo[i].Trim()));
                    }
                }
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = true;

                if (fileName != "")
                {
                    mail.Attachments.Add(new Attachment(fileName));
                }

                using (SmtpClient smtp = new SmtpClient(host, port))
                {
                    smtp.Credentials = new NetworkCredential(mailFrom, NetworkCredentialPassword);
                    smtp.EnableSsl = enableSSL;
                    smtp.Send(mail);
                }
            }
        }
    }
}