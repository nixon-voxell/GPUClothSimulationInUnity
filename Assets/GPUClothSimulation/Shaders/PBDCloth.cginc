struct Triangleids
{
  int A;
  int B;
  int C;

  float AB;
  float BC;
  float CA;
};

struct NeighborTriangleids
{
  int A;
  int B;
  int C;
  int D;
};

#define Threads_8 [numthreads(8, 1, 1)]

// uniform data
float deltaT;
float time;

half3 gravity;
half stiffness, bendingStiffness;
float restAngle;
half clothThickness;
half meshThickness;

float particleMass, particleInvertMass;

half3 windVelocity;
half windSpeed, turbulence;
half drag, lift;

uint totalSimulationVerts, totalSimulationTriangles, totalMeshVerts, totalSphereColliders;

// buffers
RWStructuredBuffer<float3> positions, projectedPositions, velocities;

RWStructuredBuffer<float3> deltaPos;
RWStructuredBuffer<uint3> deltaPosAsInt;
RWStructuredBuffer<int> deltaCount;

StructuredBuffer<float> boneWeight;

StructuredBuffer<Triangleids> sortedTriangles;
StructuredBuffer<NeighborTriangleids> neighborTriangles;

RWStructuredBuffer<float3> skinnedMeshPositions;
StructuredBuffer<float3> projectedSkinnedMeshPositions;
StructuredBuffer<float3> skinnedMeshNormals;

RWStructuredBuffer<float3> sphereColliderPositions;
StructuredBuffer<float3> projectedSphereColliderPositions;
StructuredBuffer<float> sphereColliderRadius;