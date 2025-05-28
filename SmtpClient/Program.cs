using System.Net.Sockets;
using System.Text;

namespace SmtpClient;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("SMTP Client Test");
        
        var client = new SmtpTestClient();
        
        try
        {
            await client.SendTestEmailAsync("localhost", 25);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}

public class SmtpTestClient
{
    public async Task SendTestEmailAsync(string host, int port)
    {
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(host, port);
        
        using var stream = tcpClient.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
        
        // Read greeting
        var greeting = await reader.ReadLineAsync();
        Console.WriteLine($"Server: {greeting}");
        
        // Send HELO
        await SendCommandAsync(writer, reader, "HELO testclient.local");
        
        // Send MAIL FROM
        await SendCommandAsync(writer, reader, "MAIL FROM:<test@example.com>");
        
        // Send RCPT TO
        await SendCommandAsync(writer, reader, "RCPT TO:<recipient@example.com>");
        
        // Send DATA
        await SendCommandAsync(writer, reader, "DATA");
        
        // Send email content
        Console.WriteLine("Client: [Sending email content]");
        await writer.WriteLineAsync("Subject: Test Email");
        await writer.WriteLineAsync("From: test@example.com");
        await writer.WriteLineAsync("To: recipient@example.com");
        await writer.WriteLineAsync("");
        await writer.WriteLineAsync("This is a test email sent to the SMTP server.");
        await writer.WriteLineAsync("It contains multiple lines of content.");
        await writer.WriteLineAsync("");
        await writer.WriteLineAsync("Best regards,");
        await writer.WriteLineAsync("SMTP Test Client");
        
        // End data with .
        await SendCommandAsync(writer, reader, ".");
        
        // Send QUIT
        await SendCommandAsync(writer, reader, "QUIT");
        
        Console.WriteLine("Email sent successfully!");
    }
    
    private async Task SendCommandAsync(StreamWriter writer, StreamReader reader, string command)
    {
        Console.WriteLine($"Client: {command}");
        await writer.WriteLineAsync(command);
        
        var response = await reader.ReadLineAsync();
        Console.WriteLine($"Server: {response}");
    }
}
