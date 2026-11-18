namespace vm;

using System.Buffers;

internal partial class Program
{
    private static int Main(string[] args)
    {

        var buffer = new BytecodeWriter();

        buffer.Write(OpCode.Nil);
        buffer.Write(OpCode.Invoke, (byte)1, (byte)2);


        for (int i = 0; i < buffer.Count; i++)
        {
            Console.WriteLine("{0:X2}", buffer[i]);
        }

        return 0;
    }
}

