using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace MovChat.Email
{
    public class SendGridEmail : IEmail
    {
        private readonly string apiKey;

        public SendGridEmail(string apiKey)
        {
            this.apiKey = apiKey ?? throw new System.ArgumentNullException(nameof(apiKey));
        }

        public async Task<Response> EnviarEmail(
            string to, 
            string header, 
            string subject, 
            string fromAddress = "", 
            string name = null, 
            string textPlain = null)
        {
            var mail = new SendGridMessage();
            mail.AddTo(to);
            mail.From = new EmailAddress(string.IsNullOrEmpty(fromAddress) ? "admportal@moveleiros.com.br" : fromAddress, name);
            mail.Subject = subject;
            mail.HtmlContent = header;

            if (!string.IsNullOrEmpty(textPlain))
                mail.PlainTextContent = textPlain;

            mail.Asm = new ASM
            {
                GroupId = 25735,
                GroupsToDisplay = new System.Collections.Generic.List<int> { 25735 }
            };

            var sender = new SendGridClient(apiKey);
            var response = await sender.SendEmailAsync(mail);

            return response;
        } 
    }
}
