public interface IAlbumProgressStore
{
    AlbumProgress Load();

    void Save(AlbumProgress progress);
}