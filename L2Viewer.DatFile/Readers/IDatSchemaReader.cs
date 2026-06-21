namespace L2Viewer.DatFile;

public interface IDatSchemaReader
{
    string FileName { get; }

    Type DocumentType { get; }

    DatDocument Read(string path);
}

public interface IDatSchemaReader<out TDocument> : IDatSchemaReader
    where TDocument : DatDocument
{
    new TDocument Read(string path);
}
