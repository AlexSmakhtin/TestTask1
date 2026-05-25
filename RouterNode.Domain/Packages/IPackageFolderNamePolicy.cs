namespace RouterNode.Domain.Packages;

public interface IPackageFolderNamePolicy
{
    string CreateFolderName(string orderId);
}
