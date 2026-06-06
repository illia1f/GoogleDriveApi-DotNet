using GoogleDriveApi_DotNet.Types;

namespace GoogleDriveApi_DotNet.Extensions;

internal static class ConvertionExtensions
{
    public static GDriveFile ToGDriveFile(this GoogleFile file)
    {
        return new GDriveFile
        {
            Id = file.Id,
            Name = file.Name,
            ParentIds = file.Parents?.ToList() ?? new List<string>(),
        };
    }
}