using GoogleDriveApi_DotNet.Exceptions;
using GoogleDriveApi_DotNet.Extensions;
using GoogleDriveApi_DotNet.Types;
using Shouldly;

namespace GoogleDriveApi_DotNet.Tests.Unit
{
    public class MimeTypeExtensionsTests
    {
        private static readonly MimeType FileMime = MimeType.Create("application/pdf");
        private static readonly MimeType FolderMime = MimeType.Create(GDriveMimeTypes.Folder);

        [Fact]
        public void RequireFile_OnFile_DoesNotThrow()
        {
            Should.NotThrow(() => FileMime.RequireFile());
        }

        [Fact]
        public void RequireFile_OnFolder_Throws()
        {
            Should.Throw<InvalidMimeTypeException>(() => FolderMime.RequireFile());
        }

        [Fact]
        public void RequireFolder_OnFolder_DoesNotThrow()
        {
            Should.NotThrow(() => FolderMime.RequireFolder());
        }

        [Fact]
        public void RequireFolder_OnFile_Throws()
        {
            Should.Throw<InvalidMimeTypeException>(() => FileMime.RequireFolder());
        }
    }
}
