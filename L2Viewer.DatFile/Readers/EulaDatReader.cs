namespace L2Viewer.DatFile;

public sealed class EulaDatReader : DatSchemaReader<EulaDatDocument>
{
    public override string FileName => "eula-e.dat";

    public override EulaDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        try
        {
            return ReadWithCount(path, decoded);
        }
        catch (EndOfStreamException)
        {
            return ReadSingle(path, decoded);
        }
        catch (InvalidDataException)
        {
            return ReadSingle(path, decoded);
        }
    }

    private static EulaDatDocument ReadWithCount(string path, byte[] decoded)
    {
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<EulaDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            entries.Add(new EulaDatEntry(reader.ReadAsciiString(), reader.ReadAsciiString(), reader.ReadAsciiString()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new EulaDatDocument(path, entries);
    }

    private static EulaDatDocument ReadSingle(string path, byte[] decoded)
    {
        var reader = new DatBinaryReader(decoded);
        var entries = new[]
        {
            new EulaDatEntry(reader.ReadAsciiString(), reader.ReadAsciiString(), reader.ReadAsciiString())
        };

        reader.EnsureFullyConsumedOrSafePackage();
        return new EulaDatDocument(path, entries);
    }
}
