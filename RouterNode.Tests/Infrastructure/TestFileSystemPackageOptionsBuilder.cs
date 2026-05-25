using RouterNode.Infrastructure.Files;

namespace RouterNode.Tests.Infrastructure;

public static class TestFileSystemPackageOptionsBuilder
{
    public static FileSystemPackageOptions Create()
    {
        var path = Path.Combine(Path.GetTempPath(), $"router-node-tests-{Guid.NewGuid():N}");

        return new FileSystemPackageOptions
        {
            InboxPath = Path.Combine(path, "inbox"),
            OutboxPath = Path.Combine(path, "outbox"),
            ProcessingPath = Path.Combine(path, "processing"),
            ArchivePath = Path.Combine(path, "archive"),
            DeadLetterPath = Path.Combine(path, "dead-letter"),
            SchemaPath = Path.Combine(path, "schema.xsd"),
            PassportFileName = "passport.xml"
        };
    }
}
