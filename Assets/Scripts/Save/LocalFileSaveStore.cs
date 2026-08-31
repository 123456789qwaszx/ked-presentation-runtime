using System.IO;

// {directory}/slot{n}.json.
// 경로는 조립 시점에 문자열로 받음.
public sealed class LocalFileSaveStore : ISaveStore
{
    private readonly string _directory;

    public LocalFileSaveStore(string directory)
    {
        _directory = directory;
    }

    public void Save(LocalSaveFile save) =>
        AtomicFile.WriteAllText(PathOf(save.SlotNo), SaveJson.SerializePretty(save));

    public LocalSaveFile Load(int slotNo)
    {
        string json = AtomicFile.ReadAllTextOrNull(PathOf(slotNo));

        return json == null 
            ? null 
            : SaveJson.Deserialize<LocalSaveFile>(json);
    }

    public void Delete(int slotNo) =>
        File.Delete(PathOf(slotNo));

    private string PathOf(int slotNo) =>
        Path.Combine(_directory, $"slot{slotNo}.json");
}