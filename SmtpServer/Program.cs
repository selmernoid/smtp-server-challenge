using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SmtpServer;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting SMTP Server on port 25...");
        
        var server = new SmtpServerCore();
        await server.StartAsync();
    }
}

public class SmtpServerCore
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _isRunning;

    public SmtpServerCore()
    {
        _listener = new TcpListener(IPAddress.Any, 25);
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task StartAsync()
    {
        try
        {
            _listener.Start();
            _isRunning = true;
            
            Console.WriteLine("SMTP Server started successfully on port 25");
            Console.WriteLine("Press Ctrl+C to stop the server");
            
            Console.CancelKeyPress += (sender, args) =>
            {
                args.Cancel = true;
                _cancellationTokenSource.Cancel();
            };

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await _listener.AcceptTcpClientAsync();
                    Console.WriteLine($"Client connected: {tcpClient.Client.RemoteEndPoint}");
                    
                    // Handle each client connection concurrently
                    _ = Task.Run(async () => await HandleClientAsync(tcpClient));
                }
                catch (ObjectDisposedException)
                {
                    // Server is shutting down
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting server: {ex.Message}");
        }
        finally
        {
            Stop();
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        var session = new SmtpSession(client);
        try
        {
            await session.HandleSessionAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client: {ex.Message}");
        }
        finally
        {
            client.Close();
            Console.WriteLine($"Client disconnected: {client.Client.RemoteEndPoint}");
        }
    }

    public void Stop()
    {
        if (_isRunning)
        {
            _isRunning = false;
            _listener?.Stop();
            Console.WriteLine("SMTP Server stopped");
        }
    }
}
