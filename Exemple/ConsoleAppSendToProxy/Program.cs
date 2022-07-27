// See https://aka.ms/new-console-template for more information

Console.WriteLine("Press a key to send http request to proxy.");
Console.ReadLine();

bool retry = true;
do
{
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

    Console.WriteLine("Press c to continue or another key to terminate.");
    retry = Console.ReadLine() == "c";
} while (retry);

