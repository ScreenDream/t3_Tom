// Points are particles share the same structure and stride,
// But some attributes change their meaning:
//   Weight1 -> Radius
//   Size -> Velocity
//   Weight2 -> BirthTime

struct Point
{
    float3 Position;
    float Weight1;
    float4 Rotation;
    float4 Color;
    float3 Size;
    float Weight2;
};

struct Particle
{
    float3 Position;
    float Radius;
    float4 Rotation;
    float4 Color;
    float3 Velocity;
    float BirthTime;
};
