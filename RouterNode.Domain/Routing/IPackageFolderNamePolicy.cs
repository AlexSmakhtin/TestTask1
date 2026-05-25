namespace RouterNode.Domain.Routing;

public interface IPackageFolderNamePolicy
{
    string CreateFolderName(string orderId);
}
