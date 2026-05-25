namespace RouterNode.Infrastructure.Files;

public class FileSystemPackageOptions
{
    public string InboxPath { get; set; } = null!;

    public string OutboxPath { get; set; } = null!;

    public string ProcessingPath { get; set; } = null!;

    public string ArchivePath { get; set; } = null!;

    public string SchemaPath { get; set; } = null!;

    public string PassportFileName { get; set; } = null!;
}
