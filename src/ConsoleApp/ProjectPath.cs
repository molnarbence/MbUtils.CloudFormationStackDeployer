namespace ConsoleApp;

public class ProjectPath
{
    public ProjectPath(string filePath)
    {
        // construct file path using current directory if relative path is provided
        var absolutePath = Path.IsPathRooted(filePath)
            ? filePath
            : Path.Combine(Environment.CurrentDirectory, filePath);
        
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException($"Project file not found: {absolutePath}");
        }
        FullPath = absolutePath;
    }

    public string FullPath => Path.GetFullPath(field);
    public string Directory => Path.GetDirectoryName(FullPath) ?? ".";

    public string GetTemplateFilePath(string templatePath)
    {
        return Path.Combine(Directory, templatePath);
    }
}