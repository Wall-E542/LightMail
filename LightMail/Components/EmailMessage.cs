namespace LightMail.Components;

public class EmailMessage
{
        public string Id { get; set; } = "";

        public string Sender { get; set; } = "";

        public List<string> Recipients { get; set; } = new();

        public string Subject { get; set; } = "";

        public DateTime Date { get; set; }

        public string Body { get; set; } = "";

        public string PlainBody { get; set; } = "";

        public bool IsHtml { get; set; }

        public bool IsRead { get; set; }

        public List<EmailAttachment> Attachments { get; set; } = new();
}


public class EmailAttachment
{
        public string FileName { get; set; } = "";
        public string ContentType { get; set; } = "";
        public long Size { get; set; }
        public byte[] Data { get; set; } = [];
}