
#define THREAD_COUNT_1D 256
#define THREAD_COUNT_2D 16
#define THREAD_COUNT_3D 4

#define MAX_RESOLUTION_X 128
#define MAX_RESOLUTION_Y 128
#define MAX_RESOLUTION_Z 64

#define SDF_RESOLUTION 32

#define MIN_WARP_COUNT 32

#define PI 3.1415926535f
#define E 2.7182818284f
#define EPSILON 1e-7f
#define FLT_MAX 3.402823466e+38F
#define UINT_MAX 4294967295
#define Density0 1000.0f

float TimeStep;
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

float3 sampleTallCellValue(Texture2D<float3> vTopCellVelocity, Texture2D<float3> vBottomCellVelocity, float vCenterTerrianHeight, float vCenterTallCellHeight, int2 vXZ, float vY)
{
    float3 TopValue = vTopCellVelocity[vXZ];
    float3 BottomValue = vBottomCellVelocity[vXZ];
        
    float3 a = (TopValue - BottomValue) / vCenterTallCellHeight;
    float3 b = BottomValue - a * (vCenterTerrianHeight + 0.5 * CellLength);
    
    return a * vY + b;
}

float sampleTallCellValue(Texture2D<float> vTopCellVelocity, Texture2D<float> vBottomCellVelocity, float vCenterTerrianHeight, float vCenterTallCellHeight, int2 vXZ, float vY)
{
    float TopValue = vTopCellVelocity[vXZ];
    float BottomValue = vBottomCellVelocity[vXZ];
        
    float a = (TopValue - BottomValue) / vCenterTallCellHeight;
    float b = BottomValue - a * (vCenterTerrianHeight + 0.5 * CellLength);
    
    return a * vY + b;
}

static int2 OffsetVector2D[3 * 3] =
{
    { -1, 0 },
    { 1, 0 },
    { 0, -1 },
    { 0, 1 },
    { 1, 1 },
    { -1, -1 },
    { -1, 1 },
    { 1, -1 },
    { 0, 0 }
};

static int3 OffsetVector3D[3 * 3 * 3] =
{
    { -1, 0, 0 },
    { 1, 0, 0 },
    { 0, -1, 0 },
    { 0, 1, 0 },
    { 1, 1, 0 },
    { -1, -1, 0 },
    { -1, 1, 0 },
    { 1, -1, 0 },
    { 0, 0, 0 },
    { -1, 0, 1 },
    { 1, 0, 1 },
    { 0, -1, 1 },
    { 0, 1, 1 },
    { 1, 1, 1 },
    { -1, -1, 1 },
    { -1, 1, 1 },
    { 1, -1, 1 },
    { 0, 0, 1 },
    { -1, 0, -1 },
    { 1, 0, -1 },
    { 0, -1, -1 },
    { 0, 1, -1 },
    { 1, 1, -1 },
    { -1, -1, -1 },
    { -1, 1, -1 },
    { 1, -1, -1 },
    { 0, 0, -1 }
};