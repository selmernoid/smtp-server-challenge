using System.Net.Sockets;
using System.Text;

namespace SmtpServer;

public class SmtpSessionHandlers
{
    public static async Task HandleHeloAsync(StreamWriter writer, string domain, SmtpSessionState state)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            await SendResponseAsync(writer, "501 Syntax error in parameters or arguments");
            return;
        }
        
        await SendResponseAsync(writer, $"250 Hello {domain}, pleased to meet you");
    }

    public static async Task HandleMailFromAsync(StreamWriter writer, string line)
    {
        // Parse MAIL FROM:<email>
        var match = System.Text.RegularExpressions.Regex.Match(line, @"MAIL\s+FROM:\s*<([^>]*)>", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        if (!match.Success)
        {
            await SendResponseAsync(writer, "501 Syntax error in MAIL FROM command");
            return;
        }
        
        await SendResponseAsync(writer, "250 OK");
    }

    public static async Task HandleRcptToAsync(StreamWriter writer, string line)
    {
        // Parse RCPT TO:<email>
        var match = System.Text.RegularExpressions.Regex.Match(line, @"RCPT\s+TO:\s*<([^>]*)>", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        if (!match.Success)
        {
            await SendResponseAsync(writer, "501 Syntax error in RCPT TO command");
            return;
        }
        
        await SendResponseAsync(writer, "250 OK");
    }

    public static async Task ProcessEmailAsync(string sender, List<string> recipients, List<string> emailData)
    {
        // Save email to file system
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        var filename = $"email_{timestamp}.txt";
        var filepath = Path.Combine("emails", filename);
        
        // Ensure emails directory exists
        Directory.CreateDirectory("emails");
        
        var emailContent = new StringBuilder();
        emailContent.AppendLine($"From: {sender}");
        emailContent.AppendLine($"To: {string.Join(", ", recipients)}");
        emailContent.AppendLine($"Received: {DateTime.Now:R}");
        emailContent.AppendLine();
        
        foreach (var line in emailData)
        {
            emailContent.AppendLine(line);
        }
        
        await File.WriteAllTextAsync(filepath, emailContent.ToString());
        
        Console.WriteLine($"Email saved: {filepath}");
        Console.WriteLine($"From: {sender}");
        Console.WriteLine($"To: {string.Join(", ", recipients)}");
        Console.WriteLine($"Lines: {emailData.Count}");
    }

    private static async Task SendResponseAsync(StreamWriter writer, string response)
    {
        Console.WriteLine($"Sent: {response}");
        await writer.WriteLineAsync(response);
    }
}

public enum SmtpSessionState
{
    Connected,
    Greeted,
    MailFrom,
    RcptTo,
    Data,
    MailComplete,
    Quit
}
