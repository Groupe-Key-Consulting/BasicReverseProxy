// See https://aka.ms/new-console-template for more information

using ConsoleAppSendToProxy;

var actionPerInput = new Dictionary<string, Action>
{
    ["r"] = Command.SendMessageToRedirect,
    ["l"] = Command.SendMessageToLongCall,
    ["c"] = Command.SendMessageToResetLongCall,
};

while (true)
{
    Console.WriteLine("Press :");
    Console.WriteLine(" r to send http request to proxy");
    Console.WriteLine(" l to send http request to cache proxy");
    Console.WriteLine(" c to send http request to reset cache proxy entry");

    var input = Console.ReadLine()!;

    if (!actionPerInput.TryGetValue(input, out var command))
    {
        return;
    }

    command();
}
