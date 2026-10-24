using System.Buffers.Binary;
using System.Text;

namespace RevolutionAssociates.Excel;

/// <summary>Minimal OLE2 / Compound File Binary (CFB) reader and single-stream writer, used for .xls.</summary>
internal static class CompoundFile
{
    private const uint EndOfChain = 0xFFFFFFFE;
    private const uint FreeSect = 0xFFFFFFFF;
    private const uint FatSect = 0xFFFFFFFD;
    private const uint DifSect = 0xFFFFFFFC;
    private const uint NoStream = 0xFFFFFFFF;

    private static readonly byte[] Signature = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };

    // ======================================================================
    // Reader
    // ======================================================================

    /// <summary>Returns the bytes of the first stream whose name matches one of <paramref name="names"/>, or null.</summary>
    public static byte[]? ReadStream(byte[] data, params string[] names)
    {
        if (data.Length < 512 || !data.AsSpan(0, 8).SequenceEqual(Signature))
            throw new InvalidDataException("The file is not an OLE2 compound document (.xls).");

        int sectorShift = U16(data, 0x1E);
        int miniShift = U16(data, 0x20);
        if (sectorShift < 7 || sectorShift > 16 || miniShift < 2 || miniShift > sectorShift)
            throw new InvalidDataException("Corrupt compound file header.");

        int sectorSize = 1 << sectorShift;
        int miniSize = 1 << miniShift;
        int numFat = (int)U32(data, 0x2C);
        uint dirStart = U32(data, 0x30);
        uint cutoff = U32(data, 0x38);
        uint miniFatStart = U32(data, 0x3C);
        uint difStart = U32(data, 0x44);
        int numDif = (int)U32(data, 0x48);

        long SectorOffset(uint s) => ((long)s + 1) << sectorShift;

        // ---- collect FAT sector ids (header DIFAT + DIFAT chain) ----
        var fatIds = new List<uint>();
        for (int i = 0; i < 109 && fatIds.Count < numFat; i++)
        {
            uint id = U32(data, 0x4C + 4 * i);
            if (id != FreeSect && id != EndOfChain) fatIds.Add(id);
        }

        uint next = difStart;
        int entriesPerDif = sectorSize / 4 - 1;
        for (int d = 0; d < numDif && next != EndOfChain && next != FreeSect; d++)
        {
            long off = SectorOffset(next);
            if (off + sectorSize > data.Length) break;
            for (int i = 0; i < entriesPerDif && fatIds.Count < numFat; i++)
            {
                uint id = U32(data, (int)off + 4 * i);
                if (id != FreeSect && id != EndOfChain) fatIds.Add(id);
            }
            next = U32(data, (int)off + 4 * entriesPerDif);
        }

        // ---- FAT ----
        int perSector = sectorSize / 4;
        var fat = new uint[fatIds.Count * perSector];
        for (int f = 0; f < fatIds.Count; f++)
        {
            long off = SectorOffset(fatIds[f]);
            if (off + sectorSize > data.Length)
            {
                // truncated file: fill what exists, mark the rest free
                for (int i = 0; i < perSector; i++) fat[f * perSector + i] = FreeSect;
                continue;
            }
            for (int i = 0; i < perSector; i++) fat[f * perSector + i] = U32(data, (int)off + 4 * i);
        }

        byte[] ReadChain(uint start)
        {
            using var ms = new MemoryStream();
            uint s = start;
            int guard = 0;
            while (s != EndOfChain && s != FreeSect && s < fat.Length && guard++ <= fat.Length)
            {
                long off = SectorOffset(s);
                if (off >= data.Length) break;
                int take = (int)Math.Min(sectorSize, data.Length - off);
                ms.Write(data, (int)off, take);
                s = fat[s];
            }
            return ms.ToArray();
        }

        // ---- directory ----
        var dir = ReadChain(dirStart);
        int count = dir.Length / 128;
        uint rootStart = EndOfChain;
        var found = new List<(string Name, byte Type, uint Start, long Size)>();
        for (int i = 0; i < count; i++)
        {
            int e = i * 128;
            int nameLen = U16(dir, e + 0x40);
            int chars = Math.Clamp(nameLen / 2 - 1, 0, 31);
            var name = Encoding.Unicode.GetString(dir, e, chars * 2);
            byte type = dir[e + 0x42];
            uint start = U32(dir, e + 0x74);
            long size = U32(dir, e + 0x78);
            if (type == 5 && rootStart == EndOfChain) rootStart = start;
            found.Add((name, type, start, size));
        }

        foreach (var wanted in names)
        {
            foreach (var entry in found)
            {
                if (entry.Type != 2 || !string.Equals(entry.Name, wanted, StringComparison.OrdinalIgnoreCase)) continue;

                if (entry.Size >= cutoff)
                {
                    var bytes = ReadChain(entry.Start);
                    return bytes.Length > entry.Size ? bytes.AsSpan(0, (int)entry.Size).ToArray() : bytes;
                }

                // small stream: lives in the mini stream
                var miniStream = ReadChain(rootStart);
                var miniFatBytes = ReadChain(miniFatStart);
                var miniFat = new uint[miniFatBytes.Length / 4];
                for (int i = 0; i < miniFat.Length; i++) miniFat[i] = U32(miniFatBytes, i * 4);

                var result = new byte[(int)entry.Size];
                int written = 0;
                uint m = entry.Start;
                int guard = 0;
                while (m != EndOfChain && m != FreeSect && m < miniFat.Length && written < result.Length && guard++ <= miniFat.Length)
                {
                    long off = (long)m * miniSize;
                    if (off >= miniStream.Length) break;
                    int take = (int)Math.Min(Math.Min(miniSize, result.Length - written), miniStream.Length - off);
                    Buffer.BlockCopy(miniStream, (int)off, result, written, take);
                    written += take;
                    m = miniFat[m];
                }
                return result;
            }
        }
        return null;
    }

    private static int U16(byte[] d, int o) => BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(o, 2));
    private static uint U32(byte[] d, int o) => BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(o, 4));

    // ======================================================================
    // Writer (one stream, version 3 / 512-byte sectors, no mini stream)
    // ======================================================================

    /// <summary>Writes a compound file that contains one stream. The stream is zero-padded to at least 4096 bytes
    /// so that it is stored in regular sectors (no mini stream needed).</summary>
    public static void WriteSingleStream(Stream output, string streamName, byte[] content)
    {
        const int sectorSize = 512;
        const int entriesPerFat = sectorSize / 4;        // 128
        const int difPayload = entriesPerFat - 1;        // 127 FAT ids per DIFAT sector

        int streamLen = Math.Max(content.Length, 4096);
        int dataSectors = (streamLen + sectorSize - 1) / sectorSize;
        streamLen = dataSectors * sectorSize;
        const int dirSectors = 1;

        // number of FAT (f) and DIFAT (d) sectors so that every sector is addressable
        int f = 1, d = 0;
        while (true)
        {
            d = f > 109 ? (f - 109 + difPayload - 1) / difPayload : 0;
            int total = dataSectors + dirSectors + f + d;
            int needed = (total + entriesPerFat - 1) / entriesPerFat;
            if (needed <= f) break;
            f = needed;
        }

        int dirSector = dataSectors;
        int firstFat = dataSectors + dirSectors;
        int firstDif = firstFat + f;
        int totalSectors = firstDif + d;

        // ---- header ----
        var header = new byte[sectorSize];
        Signature.CopyTo(header, 0);
        W16(header, 0x18, 0x003E);                // minor version
        W16(header, 0x1A, 3);                     // major version
        W16(header, 0x1C, 0xFFFE);                // little endian
        W16(header, 0x1E, 9);                     // sector shift (512)
        W16(header, 0x20, 6);                     // mini sector shift (64)
        W32(header, 0x28, 0);                     // directory sectors (0 for v3)
        W32(header, 0x2C, (uint)f);               // FAT sectors
        W32(header, 0x30, (uint)dirSector);       // first directory sector
        W32(header, 0x34, 0);                     // transaction signature
        W32(header, 0x38, 4096);                  // mini stream cutoff
        W32(header, 0x3C, EndOfChain);            // first mini FAT sector
        W32(header, 0x40, 0);                     // mini FAT sector count
        W32(header, 0x44, d > 0 ? (uint)firstDif : EndOfChain);
        W32(header, 0x48, (uint)d);
        for (int i = 0; i < 109; i++)
            W32(header, 0x4C + 4 * i, i < f ? (uint)(firstFat + i) : FreeSect);
        output.Write(header, 0, header.Length);

        // ---- stream data ----
        output.Write(content, 0, content.Length);
        if (streamLen > content.Length) output.Write(new byte[streamLen - content.Length], 0, streamLen - content.Length);

        // ---- directory ----
        var dir = new byte[sectorSize];
        WriteDirEntry(dir, 0, "Root Entry", type: 5, left: NoStream, right: NoStream, child: 1,
            start: EndOfChain, size: 0);
        WriteDirEntry(dir, 1, streamName, type: 2, left: NoStream, right: NoStream, child: NoStream,
            start: 0, size: (uint)streamLen);
        WriteDirEntry(dir, 2, null, type: 0, left: NoStream, right: NoStream, child: NoStream, start: 0, size: 0);
        WriteDirEntry(dir, 3, null, type: 0, left: NoStream, right: NoStream, child: NoStream, start: 0, size: 0);
        output.Write(dir, 0, dir.Length);

        // ---- FAT ----
        var fat = new uint[f * entriesPerFat];
        Array.Fill(fat, FreeSect);
        for (int i = 0; i < dataSectors; i++) fat[i] = i == dataSectors - 1 ? EndOfChain : (uint)(i + 1);
        fat[dirSector] = EndOfChain;
        for (int i = 0; i < f; i++) fat[firstFat + i] = FatSect;
        for (int i = 0; i < d; i++) fat[firstDif + i] = DifSect;

        var fatBytes = new byte[f * sectorSize];
        for (int i = 0; i < fat.Length; i++) W32(fatBytes, i * 4, fat[i]);
        output.Write(fatBytes, 0, fatBytes.Length);

        // ---- DIFAT ----
        for (int k = 0; k < d; k++)
        {
            var sector = new byte[sectorSize];
            for (int i = 0; i < difPayload; i++)
            {
                int fatIndex = 109 + k * difPayload + i;
                W32(sector, i * 4, fatIndex < f ? (uint)(firstFat + fatIndex) : FreeSect);
            }
            W32(sector, difPayload * 4, k == d - 1 ? EndOfChain : (uint)(firstDif + k + 1));
            output.Write(sector, 0, sector.Length);
        }

        _ = totalSectors; // layout bookkeeping only
    }

    private static void WriteDirEntry(byte[] dir, int index, string? name, byte type, uint left, uint right,
        uint child, uint start, uint size)
    {
        int e = index * 128;
        if (name != null)
        {
            var nameBytes = Encoding.Unicode.GetBytes(name);
            Buffer.BlockCopy(nameBytes, 0, dir, e, Math.Min(nameBytes.Length, 62));
            W16(dir, e + 0x40, Math.Min(nameBytes.Length, 62) + 2);
            dir[e + 0x43] = 1; // black
        }
        dir[e + 0x42] = type;
        W32(dir, e + 0x44, left);
        W32(dir, e + 0x48, right);
        W32(dir, e + 0x4C, child);
        W32(dir, e + 0x74, start);
        W32(dir, e + 0x78, size);
    }

    private static void W16(byte[] d, int o, int v) => BinaryPrimitives.WriteUInt16LittleEndian(d.AsSpan(o, 2), (ushort)v);
    private static void W32(byte[] d, int o, uint v) => BinaryPrimitives.WriteUInt32LittleEndian(d.AsSpan(o, 4), v);
}
