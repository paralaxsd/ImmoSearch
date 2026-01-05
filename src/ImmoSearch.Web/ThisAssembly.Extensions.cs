using ImmoSearch.Domain.Extensions;
#pragma warning disable IDE0130

partial class ThisAssembly
{
    /******************************************************************************************
     * FIELDS
     * ***************************************************************************************/
    static string? GitCommitUrlFld;

    /******************************************************************************************
     * PROPERTIES
     * ***************************************************************************************/
    public static string GitCommitUrl => GitCommitUrlFld ??= GetGitCommitUrl();

    public static string AssemblyShortFileVersion => Version.Parse(global::ThisAssembly.AssemblyFileVersion).ToString(3);

    /******************************************************************************************
     * METHODS
     * ***************************************************************************************/
    static string GetGitCommitUrl()
    {
        if (global::ThisAssembly.GitRepositoryUrl.NullOrWhitespace || global::ThisAssembly.GitCommitId.NullOrWhitespace) return string.Empty;

        var gitIdx = global::ThisAssembly.GitRepositoryUrl.LastIndexOf(".git", StringComparison.InvariantCulture);
        var repoUrlBase = gitIdx > 0 ? global::ThisAssembly.GitRepositoryUrl[..gitIdx] : global::ThisAssembly.GitRepositoryUrl.TrimEnd('/');
        return $"{repoUrlBase}/commit/{ThisAssembly.GitCommitId}";
    }

}