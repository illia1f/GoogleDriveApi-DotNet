using GoogleDriveApi_DotNet.Types;
using Shouldly;

namespace GoogleDriveApi_DotNet.Tests.Unit
{
    public class MimeTypeTests
    {
        [Theory]
        [InlineData("application/pdf")]
        [InlineData("application/vnd.google-apps.document")]
        [InlineData(GDriveMimeTypes.Folder)]
        public void Create_WithValidValue_KeepsValue(string value)
        {
            MimeType.Create(value).Value.ShouldBe(value);
        }

        [Fact]
        public void Create_WithFolderValue_IsFolderIsTrue()
        {
            MimeType.Create(GDriveMimeTypes.Folder).IsFolder.ShouldBeTrue();
        }

        [Theory]
        [InlineData("application/pdf")]
        [InlineData("application/vnd.google-apps.document")]
        public void Create_WithNonFolderValue_IsFolderIsFalse(string value)
        {
            MimeType.Create(value).IsFolder.ShouldBeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("notamimetype")]
        public void Create_WithInvalidValue_Throws(string? value)
        {
            Should.Throw<ArgumentException>(() => MimeType.Create(value));
        }
    }
}
