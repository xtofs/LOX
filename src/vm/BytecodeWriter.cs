namespace vm;

using System.Buffers;
using System.Buffers.Binary;

public sealed class BytecodeWriter(ArrayBufferWriter<byte> writer)
{
    public BytecodeWriter() : this(new ArrayBufferWriter<byte>())
    {
    }

    private readonly ArrayBufferWriter<byte> _writer = writer;

    public int Count => _writer.WrittenCount;

    public byte this[int index] => _writer.WrittenSpan[index];

    public void Write(OpCode opCode)
    {
        opCode.AssertArgLayout(OpCodeArgLayout.None);
        _writer.GetSpan(1)[0] = (byte)opCode;
        _writer.Advance(1);
    }

    public void Write(OpCode opCode, byte arg1)
    {
        opCode.AssertArgLayout(OpCodeArgLayout.OneU8);

        var span = _writer.GetSpan(2);
        span[0] = (byte)opCode;
        span[1] = arg1;
        _writer.Advance(2);
    }

    public void Write(OpCode opCode, byte arg1, byte arg2)
    {
        opCode.AssertArgLayout(OpCodeArgLayout.TwoU8);

        var span = _writer.GetSpan(3);
        span[0] = (byte)opCode;
        span[1] = arg1;
        span[2] = arg2;
        _writer.Advance(3);
    }

    public void Write(OpCode opCode, ushort arg1)
    {
        opCode.AssertArgLayout(OpCodeArgLayout.OneU16);

        var span = _writer.GetSpan(2);
        span[0] = (byte)opCode;
        BinaryPrimitives.WriteUInt16LittleEndian(span[1..], arg1);
        _writer.Advance(3);
    }
}
