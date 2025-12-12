using GoogleDriveApi_DotNet.Models;

namespace GoogleDriveApi_DotNet.Helpers
{
    public static class ConvertionExtensions
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
}