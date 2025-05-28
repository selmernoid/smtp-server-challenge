# SMTP Server Challenge - .NET Implementation

A Simple SMTP Server implementation in .NET for [Coding Challenge #90](https://codingchallenges.substack.com/p/coding-challenge-90-smtp-server).

## Overview

This project implements a basic SMTP (Simple Mail Transfer Protocol) server that can receive emails from SMTP clients. The implementation follows RFC 5321 specifications and includes all the required steps from the coding challenge.

## Features

✅ **Step 1**: TCP server listening on port 25 with 220 greeting
✅ **Step 2**: HELO/EHLO command support
✅ **Step 3**: MAIL FROM and RCPT TO command handling
✅ **Step 4**: DATA command with email body processing and transparency handling
✅ **Step 5**: Concurrent client support

## Project Structure

```
smtp-server-challenge/
├── SmtpServer/
│   ├── Program.cs          # Main server application
│   ├── SmtpSession.cs      # SMTP session handling
│   └── SmtpServer.csproj   # Server project file
├── SmtpClient/
│   ├── Program.cs          # Test client application
│   └── SmtpClient.csproj   # Client project file
└── README.md
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Administrator privileges (required for binding to port 25)

### Running the Server

1. Clone this repository:
   ```bash
   git clone https://github.com/selmernoid/smtp-server-challenge.git
   cd smtp-server-challenge
   ```

2. Build and run the SMTP server:
   ```bash
   cd SmtpServer
   dotnet run
   ```

   **Note**: On Windows/Linux, you may need to run as Administrator/root to bind to port 25.

### Testing with the Client

1. Open a new terminal and run the test client:
   ```bash
   cd SmtpClient
   dotnet run
   ```

### Testing with Telnet

You can also test the server manually using telnet:

```bash
telnet localhost 25
```

Then send SMTP commands:
```
HELO test.local
MAIL FROM:<sender@example.com>
RCPT TO:<recipient@example.com>
DATA
Subject: Test Email

This is a test message.
.
QUIT
```

## SMTP Commands Supported

| Command | Description | Implementation Status |
|---------|-------------|----------------------|
| HELO | Client identification | ✅ Implemented |
| EHLO | Extended HELO | ✅ Implemented |
| MAIL FROM | Specify sender | ✅ Implemented |
| RCPT TO | Specify recipient | ✅ Implemented |
| DATA | Email content | ✅ Implemented |
| RSET | Reset session | ✅ Implemented |
| QUIT | Close connection | ✅ Implemented |
| NOOP | No operation | ✅ Implemented |

## Email Storage

Received emails are automatically saved to the `emails/` directory with timestamps. Each email file contains:
- Sender information
- Recipient list
- Timestamp
- Full email content

## Architecture

### SmtpServerCore
- Handles TCP listener on port 25
- Manages concurrent client connections
- Implements graceful shutdown

### SmtpSession
- Manages individual client sessions
- Implements SMTP protocol state machine
- Handles command parsing and responses
- Processes email data with transparency rules

### SmtpTestClient
- Simple test client for verification
- Demonstrates proper SMTP communication
- Useful for testing server functionality

## Protocol Compliance

This implementation follows RFC 5321 specifications:
- Proper response codes (220, 250, 354, 500, 501, 503)
- Command sequence validation
- Data transparency handling (dot-stuffing)
- Graceful error handling

## Concurrency

The server supports multiple concurrent clients by:
- Using async/await for non-blocking operations
- Creating separate tasks for each client connection
- Maintaining independent session state per client

## Development Notes

### Error Handling
- Invalid commands return appropriate error codes
- Network errors are logged and handled gracefully
- Client disconnections are detected and cleaned up

### Security Considerations
- This is a basic implementation for educational purposes
- Production use would require authentication, encryption, and spam protection
- No relay protection is implemented

## Future Enhancements

Potential improvements:
- [ ] Authentication support (AUTH command)
- [ ] TLS/SSL encryption (STARTTLS)
- [ ] Email queue management
- [ ] POP3/IMAP server for email retrieval
- [ ] Configuration file support
- [ ] Logging framework integration
- [ ] Unit tests

## Contributing

Feel free to submit issues and enhancement requests!

## License

This project is open source and available under the MIT License.
