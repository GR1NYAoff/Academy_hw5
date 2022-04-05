namespace Common;

public class FileSystemProvider : IFileSystemProvider
{
    public bool Exists(string filename)
    {
        return File.Exists(filename);
    }

    public Stream Read(string filename)
    {
        return File.Open(filename, FileMode.Open, FileAccess.Read);
    }

    public void Write(string filename, Stream stream)
    {
        using var sr = new StreamReader(stream);
        var result = sr.ReadToEnd();

        using var outputStream = File.Open(filename, FileMode.Truncate, FileAccess.Write);
        using var sw = new StreamWriter(outputStream);
        sw.Write(result);

    }
}
