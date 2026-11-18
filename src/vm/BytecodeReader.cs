namespace vm;

using System.Buffers.Binary;

public ref struct BytecodeReader
{
    private readonly ReadOnlySpan<byte> _data;
    private int _position;

    public BytecodeReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _position = 0;
    }

    public readonly bool End => _position >= _data.Length;

    public byte ReadByte()
    {
        return _data[_position++];
    }

    public ushort ReadUInt16LE()
    {
        ushort value =
            BinaryPrimitives.ReadUInt16LittleEndian(
                _data.Slice(_position, 2));

        _position += 2;
        return value;
    }
}