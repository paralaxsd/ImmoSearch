using System;

partial class ThisAssembly
{
    /******************************************************************************************
     * FIELDS
     * ***************************************************************************************/
    static string? _gitCommitUrl;

    /******************************************************************************************
     * PROPERTIES
     * ***************************************************************************************/
    public static string GitCommitUrl => _gitCommitUrl ??= GetGitCommitUrl();

    public static string AssemblyShortFileVersion => Version.Parse(AssemblyFileVersion).ToString(3);

    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    static string GetGitCommitUrl()
    {
        if (string.IsNullOrWhiteSpace(GitRepositoryUrl) || string.IsNullOrWhiteSpace(GitCommitId)) return string.Empty;

        var gitIdx = GitRepositoryUrl.LastIndexOf(".git", StringComparison.InvariantCulture);
        var repoUrlBase = gitIdx > 0 ? GitRepositoryUrl[..gitIdx] : GitRepositoryUrl.TrimEnd('/');
        return $"{repoUrlBase}/commit/{GitCommitId}";
    }

}
