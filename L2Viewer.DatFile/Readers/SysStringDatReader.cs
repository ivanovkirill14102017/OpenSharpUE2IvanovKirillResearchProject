namespace L2Viewer.DatFile;

public sealed class SysStringDatReader : DatSchemaReader<SysStringDatDocument>
{
    public override string FileName => "sysstring-e.dat";

    public override SysStringDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<SysStringDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            entries.Add(new SysStringDatEntry(reader.ReadUInt32(), reader.ReadAsciiString()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new SysStringDatDocument(path, entries);
    }
}
