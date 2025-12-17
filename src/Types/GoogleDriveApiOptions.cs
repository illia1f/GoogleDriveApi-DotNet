using GoogleDriveApi_DotNet.Abstractions;

namespace GoogleDriveApi_DotNet.Types
{
    /// <summary>
    /// Configuration options for creating a GoogleDriveApi instance.
    /// </summary>
    public record GoogleDriveApiOptions : IOptions
    {
        /// <summary>
        /// Gets or sets the path to the credentials JSON file.
        /// Default value is "credentials.json".
        /// </summary>
        public string CredentialsPath { get; init; } = "credentials.json";

        /// <summary>
        /// Gets or sets the path to the token JSON file folder where it will store the token.
        /// Default value is "_metadata".
        /// </summary>
        public string TokenFolderPath { get; init; } = "_metadata";

        /// <summary>
        /// Gets or sets the name of the application (optional).
        /// Default value is null.
        /// </summary>
        public string? ApplicationName { get; init; }

        /// <summary>
        /// Gets or sets the root folder ID to use as default parent folder.
        /// Default value is "root".
        /// </summary>
        public string RootFolderId { get; init; } = "root";
    }
}