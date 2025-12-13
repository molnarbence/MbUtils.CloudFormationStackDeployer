namespace ConsoleApp;

public class ProjectPath
{
    private readonly string _filePath;

    public ProjectPath(string filePath)
    {
        // check if file exists
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Project file not found: {filePath}");
        }
        _filePath = filePath;
    }

    public string FullPath => Path.GetFullPath(_filePath);
    public string Directory => Path.GetDirectoryName(FullPath) ?? ".";

    public string GetTemplateFilePath(string templatePath)
    {
        return Path.Combine(Directory, templatePath);
    }
}