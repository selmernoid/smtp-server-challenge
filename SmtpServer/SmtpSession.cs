using System.Net.Sockets;
using System.Text;

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
        // Step 1: Send greeting message
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
