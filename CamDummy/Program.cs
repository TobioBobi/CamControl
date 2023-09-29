using System.Net;

namespace CamDummy
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var url = "http://localhost:8080/";
            using (var listener = new HttpListener())
            {
                listener.Prefixes.Add(url);
                listener.Start();
                Console.WriteLine("Start listening ...");

                while (true)
                {
                    var context = listener.GetContext();
                    var requestUrl = context.Request.Url.PathAndQuery.ToLower();

                    Console.WriteLine($"Request: {requestUrl}");

                    var response = context.Response;
                    response.StatusCode = (int)HttpStatusCode.OK;
                    response.Close();
                }
            }
        }
    }
}