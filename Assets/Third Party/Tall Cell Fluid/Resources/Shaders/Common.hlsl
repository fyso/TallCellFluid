
#define THREAD_COUNT_1D 256
#define THREAD_COUNT_2D 16
#define THREAD_COUNT_3D 4

#define MAX_RESOLUTION_X 128
#define MAX_RESOLUTION_Y 128
#define MAX_RESOLUTION_Z 64

#define SDF_RESOLUTION 32

#define PI 3.1415926535f
#define E 2.7182818284f

#define MIN_WARP_COUNT 32

#define EPSILON 1e-7f
#define PI 3.14159274F
#define FLT_MAX 3.402823466e+38F
#define UINT_MAX 4294967295
#define Density0 1000.0f

float3 Min;
float CellLength;
uint2 XZResolution;
uint ConstantCellNum;

float CubicKernel(float vX)
{
    vX = abs(vX);
    
    float Result = 0;
    if (vX >= 0.0f && vX < 1.0f)
        Result = 0.5f * pow(vX, 3.0f) - pow(vX, 2.0f) + 2.0f / 3.0f;
    else if (vX >= 1.0f && vX < 2.0f)
        Result = (1.0f / 6.0f) * pow((2.0f - vX), 3.0f);
    else
        Result = 0;

    return Result;
}

void computeAdjCell3DIndex(int3 voAdjCell3DIndex[8], int XOffset, int YOffset, int ZOffset, int3 CurrCell3DIndex)
{
    voAdjCell3DIndex[0] = CurrCell3DIndex + int3(0, YOffset, ZOffset);
    voAdjCell3DIndex[1] = CurrCell3DIndex + int3(0, YOffset, 0);
    voAdjCell3DIndex[2] = CurrCell3DIndex + int3(0, 0, ZOffset);
    voAdjCell3DIndex[3] = CurrCell3DIndex + int3(0, 0, 0);
    voAdjCell3DIndex[4] = CurrCell3DIndex + int3(XOffset, YOffset, ZOffset);
    voAdjCell3DIndex[5] = CurrCell3DIndex + int3(XOffset, YOffset, 0);
    voAdjCell3DIndex[6] = CurrCell3DIndex + int3(XOffset, 0, ZOffset);
    voAdjCell3DIndex[7] = CurrCell3DIndex + int3(XOffset, 0, 0);
}

void computeWeight(
    const int3 vAdjCell3DIndex[8], float voWeight[8], float3 vCurrParticlePosition, 
    float3 vMin, float vTerrianHeight, float vTallCellHeight, float vCellLength)
{
    for (int i = 0; i < 8; i++)
    {
        int3 AdjCell3DIndex = vAdjCell3DIndex[i];
        float2 XZ = vMin.xz + AdjCell3DIndex.xz * vCellLength;
        float3 AdjCellPos = float3(XZ.x, vTerrianHeight + vTallCellHeight + AdjCell3DIndex.y * vCellLength, XZ.y);
        float3 OffsetRatio = AdjCellPos - vCurrParticlePosition;
        voWeight[i] = abs(OffsetRatio.x) * abs(OffsetRatio.y) * abs(OffsetRatio.z);
    }
}

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