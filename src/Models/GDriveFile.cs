namespace GoogleDriveApi_DotNet.Models
{
    public record struct GDriveFile
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required List<string> ParentIds { get; set; }
    }
}