internal class StreamTracer
{
    private StreamWriter streamWriter;

    public StreamTracer(StreamWriter streamWriter)
    {
        this.streamWriter = streamWriter;
    }

    internal void WriteLine(string v)
    {
        Console.WriteLine(v);
        streamWriter.WriteLine(v);
    }

    internal void WriteLine()
    {
        Console.WriteLine();
        streamWriter.WriteLine();
    }
}