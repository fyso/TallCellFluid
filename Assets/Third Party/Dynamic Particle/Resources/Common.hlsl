#define THREAD_NUM_1D 512
#define THREAD_NUM_2D 32

#define ParticleXGridCountArgumentOffset 0
#define ParticleCountArgumentOffset 4
#define ParticleSplitPointArgumentOffset 7
#define DifferParticleCountArgumentOffset 11
#define FirstParticleXGridCountArgumentOffset 16
#define SecondParticleXGridCountArgumentOffset 19
#define ThirdParticleXGridCountArgumentOffset 22
#define ForthParticleXGridCountArgumentOffset 25
#define EPSILON 1e-7f
#define PI 3.14159274F
#define FLT_MAX 3.402823466e+38F
#define UINT_MAX 4294967295
#define Density0 1000.0f

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