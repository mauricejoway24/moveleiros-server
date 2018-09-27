using SendGrid;
using System.Threading.Tasks;

namespace MovChat.Email
{
    public interface IEmail
    {
        Task<Response> EnviarEmail(string to, string header,string subject, string fromAddress = "", string name = null, string textPlain = null);
    }
}