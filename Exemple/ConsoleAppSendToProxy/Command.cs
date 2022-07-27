using System.Diagnostics;

namespace ConsoleAppSendToProxy
{
    internal static class Command
    {
        public static void SendMessageToRedirect()
        {
            Console.WriteLine("Sending message to Redirect");
            var httpClient = new HttpClient();
            var response = httpClient.Send(new HttpRequestMessage(HttpMethod.Get, "https://localhost:7109/api/redirect"));
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Call doesn't work");
            }
            else
            {
                Console.WriteLine($"Response {response.Content.ReadAsStringAsync().Result}");
            }
        }

        public static void SendMessageToLongCall()
        {
            Console.WriteLine("Sending message to Long Call");

            var sw = Stopwatch.StartNew();
            var httpClient = new HttpClient();
            var response = httpClient.Send(new HttpRequestMessage(HttpMethod.Get, "https://localhost:7158/api/longcall"));
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Call doesn't work");
            }
            else
            {
                Console.WriteLine($"Response {response.Content.ReadAsStringAsync().Result}");
            }
            sw.Stop();
            Console.WriteLine($"Response in {sw.Elapsed}");

        }

        public static void SendMessageToResetLongCall()
        {
            Console.WriteLine("Sending message to Reset Long Call Cache entry");

            var httpClient = new HttpClient();
            var response = httpClient.Send(new HttpRequestMessage(HttpMethod.Get, "https://localhost:7158/api/resetlongcall"));
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Call doesn't work");
            }
            else
            {
                Console.WriteLine($"Response {response.Content.ReadAsStringAsync().Result}");
            }

        }
    }
}
