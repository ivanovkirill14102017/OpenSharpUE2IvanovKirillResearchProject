namespace L2Viewer.DatFile;

public abstract class DatSchemaReader<TDocument> : IDatSchemaReader<TDocument>
    where TDocument : DatDocument
{
    public abstract string FileName { get; }

    public Type DocumentType => typeof(TDocument);

    public abstract TDocument Read(string path);

    DatDocument IDatSchemaReader.Read(string path)
    {
        return Read(path);
    }
}
