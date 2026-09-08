using System;
using UnityEngine;

public sealed class LocalAlbumProgressStore
    : IAlbumProgressStore
{
    private readonly string _path;

    public LocalAlbumProgressStore(string path)
    {
        _path = path;
    }

    public AlbumProgress Load()
    {
        string json =
            AtomicFile.ReadAllTextOrNull(_path);

        if (string.IsNullOrWhiteSpace(json))
            return new AlbumProgress();

        try
        {
            return SaveJson.Deserialize<AlbumProgress>(json)
                   ?? new AlbumProgress();
        }
        catch (Exception error)
        {
            Debug.LogWarning(
                $"[앨범] 저장 파일을 읽지 못했다. 새 상태로 시작.\n{error}");

            return new AlbumProgress();
        }
    }

    public void Save(AlbumProgress progress)
    {
        if (progress == null)
            return;

        string json =
            SaveJson.SerializePretty(progress);

        AtomicFile.WriteAllText(
            _path,
            json);
    }
}