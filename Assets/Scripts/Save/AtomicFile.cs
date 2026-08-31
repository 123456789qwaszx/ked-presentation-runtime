using System.IO;
using System.Text;

// 임시 파일에 다 쓰고 한 번에 바꿔치기 (M7). 본 파일은 언제나 "이전 완성본" 아니면 "새 완성본"이다.
public static class AtomicFile
{
    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

    public static void WriteAllText(string path, string contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        string tmp = path + ".tmp";

        File.WriteAllText(tmp, contents, Utf8NoBom);

        if (File.Exists(path))
            File.Replace(tmp, path, null);
        else
            File.Move(tmp, path);
    }

    // 없으면 null.
    public static string ReadAllTextOrNull(string path) =>
        File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : null;
}
