namespace L2Viewer.DatFile;

public sealed class LogoGrpDatReader : DatSchemaReader<LogoGrpDatDocument>
{
    public override string FileName => "logongrp.dat";

    public override LogoGrpDatDocument Read(string path)
    {
        var decoded = DatDecodedFileReader.ReadDecodedBytes(path);
        try
        {
            return ReadWithCount(path, decoded);
        }
        catch (EndOfStreamException)
        {
            return ReadFixed(path, decoded, 28);
        }
        catch (InvalidDataException)
        {
            return ReadFixed(path, decoded, 28);
        }
    }

    private static LogoGrpDatDocument ReadWithCount(string path, byte[] decoded)
    {
        var reader = new DatBinaryReader(decoded);
        var count = reader.ReadInt32();
        var entries = new List<LogoGrpDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            entries.Add(new LogoGrpDatEntry(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new LogoGrpDatDocument(path, entries);
    }

    private static LogoGrpDatDocument ReadFixed(string path, byte[] decoded, int count)
    {
        var reader = new DatBinaryReader(decoded);
        var entries = new List<LogoGrpDatEntry>(count);
        for (var i = 0; i < count; i++)
        {
            entries.Add(new LogoGrpDatEntry(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()));
        }

        reader.EnsureFullyConsumedOrSafePackage();
        return new LogoGrpDatDocument(path, entries);
    }
}
