#ifndef DELAUNAY_FAST_FLUID_SIMULATION_CS_LIBRARY_INCLUDED
#define DELAUNAY_FAST_FLUID_SIMULATION_CS_LIBRARY_INCLUDED

#define offset_u int2(0, 1)
#define offset_d int2(0, -1)
#define offset_l int2(-1, 0)
#define offset_r int2(1, 0)

struct CellData
{
    float2 velocity;
    int4 edge; //xyzw:上下左右    0：边界
    float density;
    //float pressure;
    //float divergence;
    float obstacles; //固体：0或1     0-1有阻力    1为固体
    float veloDissipation;
    
    int leftStagIndex;
    int rightStagIndex;
    int downStagIndex;
    int upStagIndex;
};
struct StaggeredData
{
    float velocity;
    int nextLorDIndex;
    int RorUIndex;
};

int UvToIndex(int2 uv, int2 TexWH)//uv→[0, N]
{
    return uv.x + uv.y * (TexWH.x + 1);
}
//水平
int UvToIndex2(int2 uv, int2 TexWH)
{
    return uv.x + uv.y * (TexWH.x + 1 + 1);
}

#endif