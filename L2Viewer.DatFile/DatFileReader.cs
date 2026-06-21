namespace L2Viewer.DatFile;

public static class DatFileReader
{
    public static DatSchemaDescriptor Resolve(string path)
    {
        return DatSchemaRegistry.Resolve(path);
    }

    public static IDatSchemaReader GetReader(string path)
    {
        return DatSchemaRegistry.GetReader(path);
    }

    public static DatDocument ReadDocument(string path)
    {
        return GetReader(path).Read(path);
    }

    public static TDocument ReadDocument<TDocument>(string path)
        where TDocument : DatDocument
    {
        return DatSchemaRegistry.GetReader<TDocument>(path).Read(path);
    }
}
