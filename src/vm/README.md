

OP_CONSTANT      uint8 constantIndex
OP_NIL
OP_TRUE
OP_FALSE

OP_POP

OP_GET_LOCAL     uint8 slot
OP_SET_LOCAL     uint8 slot

OP_GET_GLOBAL    uint8 constantIndex
OP_DEFINE_GLOBAL uint8 constantIndex
OP_SET_GLOBAL    uint8 constantIndex

OP_GET_UPVALUE   uint8 slot
OP_SET_UPVALUE   uint8 slot

OP_GET_PROPERTY  uint8 constantIndex
OP_SET_PROPERTY  uint8 constantIndex

OP_GET_SUPER     uint8 constantIndex

OP_EQUAL
OP_GREATER
OP_LESS

OP_ADD
OP_SUBTRACT
OP_MULTIPLY
OP_DIVIDE

OP_NOT
OP_NEGATE

OP_PRINT

OP_JUMP          uint16 offset
OP_JUMP_IF_FALSE uint16 offset
OP_LOOP          uint16 offset

OP_CALL          uint8 argCount

OP_INVOKE        uint8 constantIndex, uint8 argCount
OP_SUPER_INVOKE  uint8 constantIndex, uint8 argCount

OP_CLOSURE       uint8 constantIndex
                 repeat(upvalueCount):
                    uint8 isLocal
                    uint8 index

OP_CLOSE_UPVALUE

OP_RETURN

OP_CLASS         uint8 constantIndex
OP_INHERIT

OP_METHOD        uint8 constantIndex
