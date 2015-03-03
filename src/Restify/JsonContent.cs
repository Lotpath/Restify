using System.Net.Http;
using System.Text;

namespace Restify
{
    public class JsonContent : StringContent
    {
        public JsonContent(ISerializer serializer, object payload = null)
            : base(payload == null ? "" : serializer.Serialize(payload), Encoding.UTF8, "application/json")
        {
        }
    }
}