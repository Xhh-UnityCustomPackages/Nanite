#ifndef LOD_INCLUDED
#define LOD_INCLUDED

int CalcLODLevel(float distanceFromCamera, float3 lodDistance)
{
    if (distanceFromCamera < lodDistance.x)
        return 0;
    else if (distanceFromCamera < lodDistance.y)
        return 1;
    else
        return 2;
}

int CalcLODLevel(float3 cameraPosWS, float positionWS, float3 lodDistance)
{
    float distanceFromCamera = distance(cameraPosWS, positionWS);
    return CalcLODLevel(distanceFromCamera, lodDistance);
}



#endif