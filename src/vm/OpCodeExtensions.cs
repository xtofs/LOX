namespace vm;

using System.Diagnostics;

public static class OpCodeExtensions
{
    static OpCodeExtensions()
    {
        var layouts = new Dictionary<OpCode, OpCodeArgLayout>()
        {
            [OpCode.Constant] = OpCodeArgLayout.OneU8,       // uint8 constantIndex
            [OpCode.Nil] = OpCodeArgLayout.None,
            [OpCode.True] = OpCodeArgLayout.None,
            [OpCode.False] = OpCodeArgLayout.None,

            [OpCode.Pop] = OpCodeArgLayout.None,

            [OpCode.GetLocal] = OpCodeArgLayout.OneU8,      // uint8 slot
            [OpCode.SetLocal] = OpCodeArgLayout.OneU8,      // uint8 slot

            [OpCode.GetGlobal] = OpCodeArgLayout.OneU8,     // uint8 constantIndex
            [OpCode.DefineGlobal] = OpCodeArgLayout.OneU8,  // uint8 constantIndex
            [OpCode.SetGlobal] = OpCodeArgLayout.OneU8,     // uint8 constantIndex

            [OpCode.GetUpvalue] = OpCodeArgLayout.OneU8,    // uint8 slot
            [OpCode.SetUpvalue] = OpCodeArgLayout.OneU8,    // uint8 slot

            [OpCode.GetProperty] = OpCodeArgLayout.OneU8,   // uint8 constantIndex
            [OpCode.SetProperty] = OpCodeArgLayout.OneU8,   // uint8 constantIndex

            [OpCode.GetSuper] = OpCodeArgLayout.OneU8,      // uint8 constantIndex

            [OpCode.Equal] = OpCodeArgLayout.None,
            [OpCode.Greater] = OpCodeArgLayout.None,
            [OpCode.Less] = OpCodeArgLayout.None,

            [OpCode.Add] = OpCodeArgLayout.None,
            [OpCode.Subtract] = OpCodeArgLayout.None,
            [OpCode.Multiply] = OpCodeArgLayout.None,
            [OpCode.Divide] = OpCodeArgLayout.None,

            [OpCode.Not] = OpCodeArgLayout.None,
            [OpCode.Negate] = OpCodeArgLayout.None,

            [OpCode.Print] = OpCodeArgLayout.None,

            [OpCode.Jump] = OpCodeArgLayout.OneU16,           // uint16 offset
            [OpCode.JumpIfFalse] = OpCodeArgLayout.OneU16,  // uint16 offset
            [OpCode.Loop] = OpCodeArgLayout.OneU16,           // uint16 offset

            [OpCode.Call] = OpCodeArgLayout.OneU8,           // uint8 argCount

            [OpCode.Invoke] = OpCodeArgLayout.TwoU8,        // uint8 constantIndex, uint8 argCount
            [OpCode.SuperInvoke] = OpCodeArgLayout.TwoU8,   // uint8 constantIndex, uint8 argCount

            [OpCode.Closure] = OpCodeArgLayout.None,        // uint8 constantIndex
                                                            //   repeat(upvalueCount):
                                                            // , // uint8 isLocal
                                                            // , // uint8 index

            [OpCode.CloseUpvalue] = OpCodeArgLayout.None,

            [OpCode.Return] = OpCodeArgLayout.None,

            [OpCode.Class] = OpCodeArgLayout.OneU8,          // uint8 constantIndex
            [OpCode.Inherit] = OpCodeArgLayout.None,

            [OpCode.Method] = OpCodeArgLayout.OneU8,         // uint8 constantIndex

        };
        var count = layouts.Keys.Max(k => (int)k) + 1;
        ArgLayouts = new OpCodeArgLayout[count];
        foreach (var kvp in layouts)
        {
            ArgLayouts[(int)kvp.Key] = kvp.Value;
        }
    }

    private static readonly OpCodeArgLayout[] ArgLayouts;

    extension(OpCode opCode)
    {
        internal void AssertArgLayout(OpCodeArgLayout layout)
        {
            Debug.Assert(ArgLayouts[(int)opCode] == layout);
        }
    }
}
