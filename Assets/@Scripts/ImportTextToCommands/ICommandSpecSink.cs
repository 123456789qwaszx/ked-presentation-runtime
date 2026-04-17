public interface ICommandSpecSink
{
    void BeginHold();
    void EndHold();
    void Enqueue(CommandSpecBase spec);
}