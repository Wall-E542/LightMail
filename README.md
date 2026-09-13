# LightMail

LightMail is a lightweight email client built with .NET MAUI and Blazor Hybrid. It connects to your existing email accounts via IMAP and SMTP and provides a clean interface for reading, writing, and organizing your mail.

Only in german for the moment!

## Features

- Connect to any IMAP/SMTP email account (including Gmail)
- Read, reply, reply-all, and forward messages
- Rich text email composition with a WYSIWYG editor (Quill)
- File attachments
- Folder navigation: Inbox, Sent, Drafts, Archive, Trash, Spam, Flagged
- Move messages to trash or delete permanently
- HTML email sanitization for safe rendering of incoming messages
- Built with MudBlazor

## Tech Stack

- .NET MAUI (Blazor Hybrid)
- MailKit / MimeKit for IMAP, SMTP, and MIME message handling
- MudBlazor for UI components
- Quill for rich text editing
- HtmlSanitizer for sanitizing HTML email content

## Getting Started

### Prerequisites

- .NET 10 SDK
- MAUI workload installed (`dotnet workload install maui`)
- Windows 10/11 (current primary target platform)

### Build and Run

```bash
dotnet build
dotnet run
```

Or open the project in an IDE such as JetBrains Rider or Visual Studio and run it from there.

### Logging In

On first launch, log in with your email address and password. LightMail will attempt to detect the correct IMAP and SMTP server settings automatically.

## Roadmap / Missing Features

LightMail is still in early development. The following are known gaps and planned improvements:

### Missing Features
- Settings page (currently does not exist)
- Email address autocomplete / suggestions when composing a message
- Mobile support (Android and iOS builds are not yet functional, despite being part of the target framework list)
- Multiple account support
- Search functionality across mailboxes
- Push notifications for new mail
- Dark mode / theme customization
- Offline support and local caching of messages
- Message threading / conversation view
- Languages

### Known Issues / Technical Debt
- Some parts of the codebase are outdated and need refactoring for stability
- IMAP operations are not fully protected against race conditions in all areas of the code
- Error handling and crash resilience need improvement throughout the app
- No automated tests yet

## Contributing

This project is under active development. Issues, feature requests, and pull requests are welcome.

## License

No license has been specified yet.
