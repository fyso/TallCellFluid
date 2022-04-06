#define THREAD_COUNT_1D 512
#define THREAD_COUNT_2D 32

#define PI 3.1415926535f
#define E 2.7182818284f

uint GridLow;
uint GridAbove;
uint D;

float3 Min;
float CellLength;
float SeaLevel;
uint2 XZResolution;
uint ConstantCellNum;

uint expandBits3D(uint v)
{
    v &= 0x000003ff;
    v = (v ^ (v << 16)) & 0xff0000ff;
    v = (v ^ (v << 8)) & 0x0300f00f;
    v = (v ^ (v << 4)) & 0x030c30c3;
    v = (v ^ (v << 2)) & 0x09249249;
    return v;
}

uint computeMorton3D(uint3 vCellIndex3D)
{
    return (expandBits3D(vCellIndex3D.z) << 2) + (expandBits3D(vCellIndex3D.y) << 1) + expandBits3D(vCellIndex3D.x);
}