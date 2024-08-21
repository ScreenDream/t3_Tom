#include "shared/hash-functions.hlsl"
#include "shared/point.hlsl"
#include "shared/quat-functions.hlsl"
#include "shared/bias-functions.hlsl"


cbuffer Params : register(b0)
{
    float __padding;
    float Radius;
    float RadiusOffset;
    float __padding1;

    float3 Center;
    float __padding2;

    float3 CenterOffset;
    float __padding3;

    float StartAngle;
    float Cycles;
    float2 __padding4;
    
    float3 Axis;
    float PointScale;

    float ScaleOffset;
    float CloseCircle;    
    float2 __padding5;

    float3 OrientationAxis;
    float1 OrientationAngle;

    float4 Color;
    float2 GainAndBias;

}


RWStructuredBuffer<Point> ResultPoints : u0;    // output

float3 RotatePointAroundAxis(float3 In, float3 Axis, float Rotation)
{
    float s = sin(Rotation);
    float c = cos(Rotation);
    float one_minus_c = 1.0 - c;

    Axis = normalize(Axis);
    float3x3 rot_mat = 
    {   one_minus_c * Axis.x * Axis.x + c, one_minus_c * Axis.x * Axis.y - Axis.z * s, one_minus_c * Axis.z * Axis.x + Axis.y * s,
        one_minus_c * Axis.x * Axis.y + Axis.z * s, one_minus_c * Axis.y * Axis.y + c, one_minus_c * Axis.y * Axis.z - Axis.x * s,
        one_minus_c * Axis.z * Axis.x - Axis.y * s, one_minus_c * Axis.y * Axis.z + Axis.x * s, one_minus_c * Axis.z * Axis.z + c
    };
    return mul(rot_mat,  In);
}

[numthreads(256,4,1)]
void main(uint3 i : SV_DispatchThreadID)
{
    uint pointCount,stride;
    ResultPoints.GetDimensions(pointCount, stride);

    if(i.x >= pointCount)
        return;

    bool closeCircle =  CloseCircle > 0.5;
    float angleStepCount = closeCircle ? (pointCount -2) : pointCount;

    float f = (float)(i.x)/angleStepCount;
    f = ApplyBiasAndGain(f, GainAndBias.x, GainAndBias.y);

    float l = Radius + RadiusOffset * f;
    float angle = (StartAngle * 3.141578/180 + Cycles * 2 * 3.141578 * f);
    float3 up = Axis.y > 0.7 ? float3(0,0,1) :  float3(0,1,0);
    float3 direction = normalize(cross(Axis, up));

    float3 v2 = RotatePointAroundAxis(direction * l , Axis, angle);

    float4 lookat = qLookAt(Axis, up);

    float4 spin = qFromAngleAxis( (OrientationAngle) / 180 * 3.141578, normalize(OrientationAxis));
    float4 spin2 = qFromAngleAxis( angle, float3(Axis));

    ResultPoints[i.x].Position = v2 + Center + CenterOffset * f;
    ResultPoints[i.x].Weight1 = (closeCircle && i.x == pointCount -1) ? NAN : 1.0;
    ResultPoints[i.x].Size = PointScale + ScaleOffset * f;
    ResultPoints[i.x].Rotation = qMul(normalize(qMul(spin2, lookat)), spin);
    ResultPoints[i.x].Color = Color;
    ResultPoints[i.x].Weight2 = 1;

}

