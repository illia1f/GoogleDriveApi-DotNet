using GoogleDriveApi_DotNet.Types;
using Shouldly;

namespace GoogleDriveApi_DotNet.Tests.Unit
{
    public class MimeTypeTests
    {
        [Theory]
        [InlineData("application/pdf")]
        [InlineData("application/vnd.google-apps.document")]
        [InlineData(MimeType.Folder)]
        public void Create_WithValidValue_KeepsValue(string value)
        {
            MimeType.Create(value).Value.ShouldBe(value);
        }

        [Fact]
        public void Create_WithFolderValue_IsFolderIsTrue()
        {
            MimeType.Create(MimeType.Folder).IsFolder.ShouldBeTrue();
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

        [Theory]
        [InlineData(MimeType.Folder)]
        [InlineData(MimeType.Document)]
        [InlineData(MimeType.Spreadsheet)]
        [InlineData(MimeType.Presentation)]
        [InlineData(MimeType.Drawing)]
        public void IsGoogleWorkspace_OnWorkspaceType_IsTrue(string value)
        {
            MimeType.Create(value).IsGoogleWorkspace.ShouldBeTrue();
        }

        [Theory]
        [InlineData("application/pdf")]
        [InlineData("image/png")]
        public void IsGoogleWorkspace_OnNonWorkspaceType_IsFalse(string value)
        {
            MimeType.Create(value).IsGoogleWorkspace.ShouldBeFalse();
        }

        [Theory]
        [InlineData(MimeType.Document, "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
        [InlineData(MimeType.Spreadsheet, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        [InlineData(MimeType.Presentation, "application/vnd.openxmlformats-officedocument.presentationml.presentation")]
        [InlineData(MimeType.Drawing, "image/png")]
        public void GetExportMimeType_ForExportableType_ReturnsTarget(string value, string expected)
        {
            MimeType.Create(value).GetExportMimeType()!.Value.ShouldBe(expected);
        }

        [Theory]
        [InlineData("application/pdf")]
        [InlineData(MimeType.Folder)]
        public void GetExportMimeType_WithoutExportTarget_ReturnsNull(string value)
        {
            MimeType.Create(value).GetExportMimeType().ShouldBeNull();
        }
    }
}
