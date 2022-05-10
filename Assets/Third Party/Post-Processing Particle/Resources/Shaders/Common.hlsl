#define THREAD_NUM_1D 256

float3 Min;
float CellLength;
StructuredBuffer<uint> HashCountBuffer;
StructuredBuffer<uint> HashOffsetBuffer;

/* compute morton code */
uint expandBits3D(uint v)
{
    v &= 0x000003ff; // x = ---- ---- ---- ---- ---- --98 7654 3210
    v = (v ^ (v << 16)) & 0xff0000ff; // x = ---- --98 ---- ---- ---- ---- 7654 3210
    v = (v ^ (v << 8)) & 0x0300f00f; // x = ---- --98 ---- ---- 7654 ---- ---- 3210
    v = (v ^ (v << 4)) & 0x030c30c3; // x = ---- --98 ---- 76-- --54 ---- 32-- --10
    v = (v ^ (v << 2)) & 0x09249249; // x = ---- 9--8 --7- -6-- 5--4 --3- -2-- 1--0
    return v;
}

uint computeMorton3D(uint3 vCellIndex3D)
{
    return ((expandBits3D(vCellIndex3D.z) << 2) +
        (expandBits3D(vCellIndex3D.y) << 1) +
        expandBits3D(vCellIndex3D.x)) % 218357;
}

uint computeMorton2D(uint2 vCellIndex3D)
{
    return ((expandBits3D(vCellIndex3D.y) << 1) + expandBits3D(vCellIndex3D.x)) % 218357;
}

int3 convertPos2CellIndex(float3 pos)
{
    if (any(pos < Min)) return int3(-1, 0, 0);
    return (pos - Min) / CellLength;
}