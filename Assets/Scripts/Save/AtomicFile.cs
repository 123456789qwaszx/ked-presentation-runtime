using System;
using System.IO;
using System.Text;

namespace Ked.Save
{
    // 임시 파일에 다 쓰고 나서 한 번에 바꿔치기 (M7-2).
    //
    // File.WriteAllText를 곧장 쓰면 "쓰다 만 파일"이 생길 수 있다 — 전원이 나가면
    // 반 토막 JSON이 남고, 다음 실행의 역직렬화가 죽는다. 임시 파일 → 교체 순서면
    // 본 파일은 언제나 "이전 완성본" 아니면 "새 완성본" 둘 중 하나다.
    public static class AtomicFile
    {
        // BOM 없는 UTF-8. WriteAllText의 기본도 BOM 없음이지만 명시한다 —
        // 서버 레포에서 BOM·재인코딩으로 두 번 데였다(F35·F45).
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        public static void WriteAllText(string path, string contents)
        {
            string directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string tmp = path + ".tmp";

            File.WriteAllText(tmp, contents, Utf8NoBom);

            // File.Replace가 원자적 교체의 정석이지만 대상 파일이 없으면 던지고,
            // 일부 플랫폼(초기 Android 등)이 지원하지 않았다. 없으면 Move면 충분하고
            // (새 파일 생성엔 경쟁이 없다), Replace가 안 되는 곳에서만 삭제 후 Move로
            // 물러선다 — 그 한순간의 공백은 감수한다(다음 저장이 곧 메운다).
            if (!File.Exists(path))
            {
                File.Move(tmp, path);
                return;
            }

            try
            {
                File.Replace(tmp, path, null);
            }
            catch (PlatformNotSupportedException)
            {
                File.Delete(path);
                File.Move(tmp, path);
            }
        }

        // 없거나 깨졌으면 null — "저장이 없다"로 취급한다. 예외 종류를 밖에 알릴 필요가
        // 없는 이유: 어느 쪽이든 호출자가 할 일은 같다(새로 시작, 다음 저장이 덮는다).
        public static string ReadAllTextOrNull(string path)
        {
            try
            {
                return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : null;
            }
            catch (IOException)
            {
                return null;
            }
        }
    }
}
