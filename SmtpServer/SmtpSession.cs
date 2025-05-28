using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace SmtpServer;

public class SmtpSession
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private SmtpSessionState _state;
    private readonly List<string> _recipients;
    private string _sender = string.Empty;
    private readonly List<string> _emailData;

    public SmtpSession(TcpClient client)
    {
        _client = client;
        _stream = client.GetStream();
        _reader = new StreamReader(_stream, Encoding.UTF8);
        _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };
        _state = SmtpSessionState.Connected;
        _recipients = new List<string>();
        _emailData = new List<string>();
    }

    public async Task HandleSessionAsync()
    {
        await SendResponseAsync("220 CC SMTP Server Ready");
        
        string? line;
        while ((line = await _reader.ReadLineAsync()) != null)
        {
            Console.WriteLine($"Received: {line}");
            
            var command = line.Trim().ToUpper();
            var parts = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length == 0) continue;
            
            var cmd = parts[0];
            var argument = parts.Length > 1 ? parts[1] : string.Empty;
            
            try
            {
                await ProcessCommandAsync(cmd, argument, line);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing command: {ex.Message}");
                await SendResponseAsync("500 Internal server error");
            }
            
            if (_state == SmtpSessionState.Quit)
                break;
        }
    }

    private async Task ProcessCommandAsync(string command, string argument, string originalLine)
    {
        switch (command)
        {
            case "HELO":
            case "EHLO":
                await HandleHeloAsync(argument);
                break;
            case "MAIL":
                await HandleMailFromAsync(originalLine);
                break;
            case "RCPT":
                await HandleRcptToAsync(originalLine);
                break;
            case "DATA":
                await HandleDataAsync();
                break;
            case "RSET":
                await HandleResetAsync();
                break;
            case "QUIT":
                await HandleQuitAsync();
                break;
            case "NOOP":
                await SendResponseAsync("250 OK");
                break;
            default:
                await SendResponseAsync($"500 Command '{command}' not recognized");
                break;
        }
    }

    private async Task HandleHeloAsync(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            await SendResponseAsync("501 Syntax error in parameters or arguments");
            return;
        }
        
        _state = SmtpSessionState.Greeted;
        await SendResponseAsync($"250 Hello {domain}, pleased to meet you");
    }

    private async Task HandleMailFromAsync(string line)
    {
        if (_state != SmtpSessionState.Greeted && _state != SmtpSessionState.MailComplete)
        {
            await SendResponseAsync("503 Bad sequence of commands");
            return;
        }
        
        var match = Regex.Match(line, @"MAIL\s+FROM:\s*<([^>]*)>", RegexOptions.IgnoreCase);
        
        if (!match.Success)
        {
            await SendResponseAsync("501 Syntax error in MAIL FROM command");
            return;
        }
        
        _sender = match.Groups[1].Value;
        _state = SmtpSessionState.MailFrom;
        await SendResponseAsync("250 OK");
    }

    private async Task HandleRcptToAsync(string line)
    {
        if (_state != SmtpSessionState.MailFrom && _state != SmtpSessionState.RcptTo)
        {
            await SendResponseAsync("503 Bad sequence of commands");
            return;
        }
        
        var match = Regex.Match(line, @"RCPT\s+TO:\s*<([^>]*)>", RegexOptions.IgnoreCase);
        
        if (!match.Success)
        {
            await SendResponseAsync("501 Syntax error in RCPT TO command");
            return;
        }
        
        var recipient = match.Groups[1].Value;
        _recipients.Add(recipient);
        _state = SmtpSessionState.RcptTo;
        await SendResponseAsync("250 OK");
    }

    private async Task HandleDataAsync()
    {
        if (_state != SmtpSessionState.RcptTo)
        {
            await SendResponseAsync("503 Bad sequence of commands");
            return;
        }
        
        _state = SmtpSessionState.Data;
        await SendResponseAsync("354 Start mail input; end with <CRLF>.<CRLF>");
        
        _emailData.Clear();
        string? line;
        
        while ((line = await _reader.ReadLineAsync()) != null)
        {
            if (line == ".")
            {
                break;
            }
            
            // Handle transparency - remove leading dot if line starts with ..
            if (line.StartsWith(".."))
            {
                line = line.Substring(1);
            }
            
            _emailData.Add(line);
        }
        
        await ProcessEmailAsync();
        
        _state = SmtpSessionState.MailComplete;
        await SendResponseAsync("250 OK: Message accepted for delivery");
    }

    private async Task ProcessEmailAsync()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        var filename = $"email_{timestamp}.txt";
        var filepath = Path.Combine("emails", filename);
        
        Directory.CreateDirectory("emails");
        
        var emailContent = new StringBuilder();
        emailContent.AppendLine($"From: {_sender}");
        emailContent.AppendLine($"To: {string.Join(", ", _recipients)}");
        emailContent.AppendLine($"Received: {DateTime.Now:R}");
        emailContent.AppendLine();
        
        foreach (var line in _emailData)
        {
            emailContent.AppendLine(line);
        }
        
        await File.WriteAllTextAsync(filepath, emailContent.ToString());
        
        Console.WriteLine($"Email saved: {filepath}");
        Console.WriteLine($"From: {_sender}");
        Console.WriteLine($"To: {string.Join(", ", _recipients)}");
        Console.WriteLine($"Lines: {_emailData.Count}");
    }

    private async Task HandleResetAsync()
    {
        _sender = string.Empty;
        _recipients.Clear();
        _emailData.Clear();
        _state = SmtpSessionState.Greeted;
        await SendResponseAsync("250 OK");
    }

    private async Task HandleQuitAsync()
    {
        _state = SmtpSessionState.Quit;
        await SendResponseAsync("221 CC SMTP Server closing connection");
    }

    private async Task SendResponseAsync(string response)
    {
        Console.WriteLine($"Sent: {response}");
        await _writer.WriteLineAsync(response);
    }
}
