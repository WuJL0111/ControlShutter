using System.IO;
using System.Net;
using System.Text;

namespace ControlDoors.Common
{
    public class Http
    {
        public string PostJson(string url, string postInfo)
        {
            try
            {
                string result = "";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.Timeout = Timeout.Infinite;
                request.AllowAutoRedirect = false;
                request.ContentType = "application/json;charset=UTF-8";
                request.KeepAlive = true;
                request.ContentLength = Encoding.UTF8.GetByteCount(postInfo);
                byte[] data = Encoding.UTF8.GetBytes(postInfo);
                request.ProtocolVersion = HttpVersion.Version11;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                using (Stream reqStream = request.GetRequestStream())
                {
                    reqStream.Write(data, 0, data.Length);
                    reqStream.Close();
                }
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();

                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    result = reader.ReadToEnd();
                }

                if (response != null) response.Close();
                if (request != null) request.Abort();
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"HTTP/POST出现异常，{ex.Message}");
                GC.Collect();
                throw;
            }
        }
    }
}
