using BrainRotDoctor.Core.Configuration;

namespace BrainRotDoctor.App.Runtime;

internal sealed class LoadedConfiguration
{
    public LoadedConfiguration(BlockerConfiguration configuration, string source, string json, string? filePath)
    {
        Configuration = configuration;
        Source = source;
        Json = json;
        FilePath = filePath;
    }

    public BlockerConfiguration Configuration { get; }
    public string Source { get; }
    public string Json { get; }
    public string? FilePath { get; }
}
