using GoogleDriveApi_DotNet.Types;

namespace GoogleDriveApi_DotNet.Extensions;

internal static class ConvertionExtensions
{
    public static DriveItem ToDriveItem(this GoogleFile file)
    {
        return new DriveItem
        {
            Id = file.Id,
            Name = file.Name,
            MimeType = MimeType.Create(file.MimeType),
            ParentIds = file.Parents?.ToList() ?? [],
        };
    }
}