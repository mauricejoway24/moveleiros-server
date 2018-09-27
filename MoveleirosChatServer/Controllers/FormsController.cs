using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using MovChat.Data.Repositories;
using MovChat.Email;
using MoveleirosChatServer.Models;

namespace MoveleirosChatServer.Controllers
{
    public class FormsController : Controller
    {
        private readonly UOW uow;
        private readonly IHostingEnvironment environment;

        public FormsController(
            UOW uow, 
            IHostingEnvironment environment)
        {
            this.uow = uow ?? throw new System.ArgumentNullException(nameof(uow));
            this.environment = environment ?? throw new System.ArgumentNullException(nameof(environment));
        }

        /// <summary>
        /// This method receives a question form to be sent.
        /// </summary>
        /// <param name="questionForm">Form to be sent</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> SendQuestionForm([FromBody] QuestionFormData questionForm)
        {
            // Get seller emails list
            var storeRepository = uow.GetRepository<StoreRepository>();
            var adminLojaEmails = await storeRepository.GetAdminsEmailFromStore(questionForm.StoreId);

            // Send an email for each person in store
            var emailService = new SendGridEmail("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");

            // Read email template
            var emailTemplate = string.Empty;
            using (var reader = new StreamReader(Path.Combine(environment.ContentRootPath, "EmailTemplates", "client_without_agent.html")))
            {
                emailTemplate = await reader.ReadToEndAsync();
            }

            // Send emails
            var tempEmailTemplate = string.Empty;
            foreach (var email in adminLojaEmails)
            {
                tempEmailTemplate = emailTemplate
                    .Replace("{{name}}", questionForm.Name)
                    .Replace("{{contact}}", questionForm.Phone)
                    .Replace("{{message}}", questionForm.Message);

                await emailService.EnviarEmail(
                    to: email.Email,
                    header: tempEmailTemplate,
                    subject: "Cliente sem atendimento!",
                    fromAddress: "no-reply@moveleiros.com.br",
                    name: "Chat Moveleiros",
                    textPlain: $"O cliente {questionForm.Name} não foi atendido. " +
                        $"O contato informado é {questionForm.Phone}. " +
                        $"A mensagem deixada foi {questionForm.Message}"
                );
            }

            return Ok();
        }
    }
}