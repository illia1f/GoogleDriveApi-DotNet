using GoogleDriveApi_DotNet.Types;
using Shouldly;

namespace GoogleDriveApi_DotNet.Tests.Unit
{
    public class DriveFieldsTests
    {
        [Fact]
        public void Default_RendersInvariantCore()
        {
            DriveFields.Default.ToGetMask().ShouldBe("id,name,mimeType");
        }

        [Fact]
        public void Default_ToListMask_WrapsCoreInFilesAndPageToken()
        {
            DriveFields.Default.ToListMask().ShouldBe("nextPageToken, files(id,name,mimeType)");
        }

        [Fact]
        public void WithSize_AppendsSizeAfterCore()
        {
            DriveFields.Default.WithSize().ToGetMask().ShouldBe("id,name,mimeType,size");
        }

        [Fact]
        public void With_Chained_AppendsInOrder()
        {
            DriveFields.Default
                .WithSize()
                .WithModifiedTime()
                .WithWebViewLink()
                .ToGetMask()
                .ShouldBe("id,name,mimeType,size,modifiedTime,webViewLink");
        }

        [Fact]
        public void WithParents_RenderedInListMask()
        {
            DriveFields.Default.WithParents().ToListMask()
                .ShouldBe("nextPageToken, files(id,name,mimeType,parents)");
        }

        [Fact]
        public void With_RepeatedField_IsDeduplicated()
        {
            DriveFields.Default.WithSize().WithSize().ToGetMask().ShouldBe("id,name,mimeType,size");
        }

        [Fact]
        public void With_IsImmutable_OriginalUnchanged()
        {
            var core = DriveFields.Default;
            _ = core.WithSize();

            core.ToGetMask().ShouldBe("id,name,mimeType");
        }

        [Fact]
        public void WithRaw_AppendsTokensVerbatim()
        {
            DriveFields.Default.WithRaw("owners,capabilities").ToGetMask()
                .ShouldBe("id,name,mimeType,owners,capabilities");
        }

        [Fact]
        public void WithRaw_PreservesNestedSyntax()
        {
            DriveFields.Default.WithRaw("owners(emailAddress,displayName)").ToGetMask()
                .ShouldBe("id,name,mimeType,owners(emailAddress,displayName)");
        }

        [Theory]
        [InlineData("owners(a),size")]     // a closed group followed by a field
        [InlineData("owners(a, b)")]       // spaces after a separator are tolerated
        [InlineData("a(b(c))")]            // nested groups
        public void WithRaw_ValidSyntax_IsAccepted(string fields)
        {
            Should.NotThrow(() => DriveFields.Default.WithRaw(fields));
        }

        [Theory]
        [InlineData("size,")]              // trailing comma
        [InlineData(",size")]              // leading comma
        [InlineData("a,,b")]               // empty token between commas
        [InlineData("a, ,b")]              // whitespace-only token
        [InlineData("owners(")]            // unbalanced open paren
        [InlineData("size)")]              // unbalanced close paren
        [InlineData("owners()")]           // empty group
        [InlineData("owners(a,)")]         // trailing comma inside a group
        [InlineData("(size)")]             // group without a field name
        public void WithRaw_MalformedSyntax_Throws(string fields)
        {
            Should.Throw<ArgumentException>(() => DriveFields.Default.WithRaw(fields));
        }
    }
}
