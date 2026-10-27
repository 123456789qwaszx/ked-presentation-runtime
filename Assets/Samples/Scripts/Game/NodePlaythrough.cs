using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// 연출 단독 예제의 회차 — 노드 하나를 처음부터, 또는 마지막으로 본 줄부터.
///
/// 진행을 모른다. 파일 하나에 두는 것은 노드 이름과, 연출 기억(PlaybackMemory)이 꺼내 준 문자열 둘뿐이다.
/// 진행·저장 예제가 같은 문자열을 진행 저장의 HostState 칸에 넣는다 — 다른 것은 저장소뿐이다.
///
///   저장       줄이 다 보일 때마다(LineDisplayed) 재개 지점을 쓴다. 노드 끝까지 가면 지운다.
///   끝         노드 끝까지 가면 Completed를 알린다(끝 카드). 타이틀로 나가서 끊긴 것은 끝이 아니다.
///   이어하기   같은 노드를 루트(노드 시작)부터 다시 달린다. 기록된 Yarn 선택은 자동 응답하고,
///              그 줄까지는 보이지 않게 빨리 감은 뒤 거기서 플레이어에게 넘긴다.
///
/// Yarn 변수는 저장하지 않는다 — 루트부터 다시 달리며 재구성된다(진행·저장 예제와 같은 전제).
/// 그래서 대본이 바뀌어 그 줄을 찾지 못하면 시크가 선택지에서 멈추고 일반 재생으로 돌아간다.
/// </summary>
public sealed class NodePlaythrough : IPlaythroughEntry
{
    // 형식을 바꾸면 올린다. 다른 판의 파일은 읽지 않고 처음부터 시작한다.
    private const int FormatVersion = 1;

    private static readonly UTF8Encoding Utf8NoBom = new(false);

    private sealed class ResumeFile
    {
        public int Version;
        public string Node;
        public string Backlog;
        public string ResumePoint;
    }

    /// <summary>노드를 끝까지 봤다 — 이어하기 파일을 지운 뒤. 끝내기 요청으로 끊긴 경우에는 오지 않는다.</summary>
    public event Action Completed;

    private readonly NodePlaybackHost _host;
    private readonly PlaybackMemory _memory;
    private readonly string _nodeName;
    private readonly string _path;

    public NodePlaythrough(
        NodePlaybackHost host,
        PlaybackMemory memory,
        string nodeName,
        string path)
    {
        _host = host;
        _memory = memory;
        _nodeName = nodeName;
        _path = path;
    }

    #region 타이틀

    public void StartNew()
    {
        if (_host.IsRunning)
            return;

        Delete();
        _memory.Forget();

        _ = PlayAsync();
    }

    public void Resume()
    {
        if (_host.IsRunning)
            return;

        if (!TryRestore())
        {
            StartNew();
            return;
        }

        _ = PlayAsync();
    }

    /// <summary>
    /// 이어할 파일이 있고 이 노드·이 형식의 것인가. 타이틀이 이어하기 버튼을 막을지 정할 때 쓴다.
    /// 읽기만 한다 — 연출 기억을 되돌리지 않는다. 있다고 해도 Resume에서 기억이 깨져 있으면 처음부터다.
    /// </summary>
    public bool HasResumePoint
    {
        get
        {
            try
            {
                if (!File.Exists(_path))
                    return false;

                ResumeFile file = JsonConvert.DeserializeObject<ResumeFile>(File.ReadAllText(_path, Encoding.UTF8));

                return file != null &&
                       file.Version == FormatVersion &&
                       string.Equals(file.Node, _nodeName, StringComparison.Ordinal) &&
                       !string.IsNullOrEmpty(file.ResumePoint);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is JsonException)
            {
                return false;
            }
        }
    }

    #endregion

    #region 저장

    /// <summary>줄이 다 보였을 때 부른다(VNLinePresentationFlow.LineDisplayed). 시크 중이면 아무것도 쓰지 않는다.</summary>
    public void SaveCurrentLine()
    {
        if (!_host.IsRunning)
            return;

        if (!_memory.TryCaptureResumePoint(out string resumePoint, out _))
            return;

        var file = new ResumeFile
        {
            Version = FormatVersion,
            Node = _nodeName,
            Backlog = _memory.CaptureBacklogBeforeUnit(),
            ResumePoint = resumePoint,
        };

        try
        {
            Write(JsonConvert.SerializeObject(file, Formatting.Indented));
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            Debug.LogWarning($"[연출 단독] 이어하기 지점을 쓰지 못했다. — {error.Message}");
        }
    }

    #endregion

    private async Task PlayAsync()
    {
        try
        {
            bool completed = await _host.PlayAsync(_nodeName);

            // 끝까지 본 노드에는 이어할 것이 없다. 타이틀로 나가서 끊긴 경우는 남긴다.
            if (completed)
            {
                Delete();
                Completed?.Invoke();
            }
        }
        catch (Exception error)
        {
            Debug.LogException(error);
        }
    }

    private bool TryRestore()
    {
        string json;

        try
        {
            json = File.Exists(_path) ? File.ReadAllText(_path, Encoding.UTF8) : null;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            Debug.LogWarning($"[연출 단독] 이어하기 파일을 읽지 못했다. 처음부터. — {error.Message}");
            return false;
        }

        if (json == null)
        {
            Debug.Log("[연출 단독] 이어할 것이 없다. 처음부터.");
            return false;
        }

        ResumeFile file;

        try
        {
            file = JsonConvert.DeserializeObject<ResumeFile>(json);
        }
        catch (JsonException error)
        {
            Debug.LogWarning($"[연출 단독] 이어하기 파일이 깨졌다. 처음부터. — {error.Message}");
            return false;
        }

        if (file == null || file.Version != FormatVersion)
        {
            Debug.LogWarning("[연출 단독] 다른 형식의 이어하기 파일이다. 처음부터.");
            return false;
        }

        if (!string.Equals(file.Node, _nodeName, StringComparison.Ordinal))
        {
            Debug.Log($"[연출 단독] 이어하기 파일은 '{file.Node}' 노드의 것이다. 지금 노드는 '{_nodeName}' — 처음부터.");
            return false;
        }

        try
        {
            _memory.Restore(file.Backlog ?? "", file.ResumePoint);
        }
        catch (InvalidDataException error)
        {
            Debug.LogWarning($"[연출 단독] 저장된 연출 기억을 읽지 못했다. 처음부터. — {error.Message}");
            return false;
        }

        Debug.Log($"[연출 단독] '{_nodeName}' 이어하기.");
        return true;
    }

    // 임시 파일에 다 쓴 뒤 바꿔 끼운다 — 쓰는 도중 꺼져도 앞의 파일은 남는다.
    private void Write(string contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path));

        string tmp = _path + ".tmp";
        File.WriteAllText(tmp, contents, Utf8NoBom);

        if (File.Exists(_path))
            File.Replace(tmp, _path, null);
        else
            File.Move(tmp, _path);
    }

    private void Delete()
    {
        try
        {
            if (File.Exists(_path))
                File.Delete(_path);
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            Debug.LogWarning($"[연출 단독] 이어하기 파일을 지우지 못했다. — {error.Message}");
        }
    }
}
