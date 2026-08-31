using System.IO;
using System.Text;

// 임시 파일에 다 쓰고 한 번에 바꿔치기.
// 세이브 파일 쓰다가 꺼지는 경우 대비.(로컬 저장 파일이 진실이기 때문.)
public static class AtomicFile
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);

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
        File.Exists(path) 
            ? File.ReadAllText(path, Encoding.UTF8) 
            : null;
}