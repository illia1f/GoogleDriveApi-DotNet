using GoogleDriveApi_DotNet.Abstractions;

namespace GoogleDriveApi_DotNet.Types
{
    /// <summary>
    /// Configuration options for creating a GoogleDriveApi instance.
    /// </summary>
    public record GoogleDriveApiOptions : IOptions
    {
        public const string DefaultRootFolderId = "root";
        public const string DefaultCredentialsPath = "credentials.json";
        public const string DefaultTokenFolderPath = "_metadata";
        public const string DefaultUserId = "user";  

        /// <summary>
        /// Gets or sets the root folder ID to use as default parent folder.
        /// Default value is "root".
        /// </summary>
        public string RootFolderId { get; init; } = DefaultRootFolderId;

        /// <summary>
        /// Gets or sets the path to the credentials JSON file.
        /// Default value is "credentials.json".
        /// </summary>
        public string CredentialsPath { get; init; } = DefaultCredentialsPath;

        /// <summary>
        /// Gets or sets the path to the token JSON file folder where it will store the token.
        /// Default value is "_metadata".
        /// </summary>
        public string TokenFolderPath { get; init; } = DefaultTokenFolderPath;

        /// <summary>
        /// Gets or sets the user identifier used to store and retrieve tokens in the token data store.
        /// <para>
        /// This value is used as part of the key under which tokens are cached. Change it to avoid token collisions
        /// when authorizing multiple accounts while sharing the same <see cref="TokenFolderPath"/>.
        /// </para>
        /// Default value is "user".
        /// </summary>
        public string UserId { get; init; } = DefaultUserId;

        /// <summary>
        /// Gets or sets the name of the application (optional).
        /// Default value is null.
        /// </summary>
        public string? ApplicationName { get; init; }
    }
}