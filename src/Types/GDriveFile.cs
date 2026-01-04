namespace GoogleDriveApi_DotNet.Types;

public record struct GDriveFile
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required List<string> ParentIds { get; set; }
}