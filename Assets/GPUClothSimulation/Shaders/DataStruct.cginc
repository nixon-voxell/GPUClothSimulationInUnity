// struct Particle
// {
//   float3 pos;
//   float3 predictedPos;
//   float3 velocity;
//   float invMass;
//   float mass;
//   int idx;
//   int phase;
// };

// struct Edge
// {
//   int p0;
//   int p1;
//   float restLength;
//   int idx;
// };

// struct Triangle
// {
//   int p0;
//   int p1;
//   int p2;
//   int idx;
// };

// struct NeighborTriangles
// {
//   int p0;
//   int p1;
//   int p2;
//   int p3;
//   float restAngle;
// };

// RWStructuredBuffer<Particle> particles;
// RWStructuredBuffer<Edge> edges;
// RWStructuredBuffer<Triangle> triangeles;
// RWStructuredBuffer<NeighborTriangles> neighborTriangles;

RWStructuredBuffer<float3> pos;
RWStructuredBuffer<float3> predictedPos;
RWStructuredBuffer<float3> velocity;
RWStructuredBuffer<float> mass;
RWStructuredBuffer<float> invMass;

StructuredBuffer<int> edges;
StructuredBuffer<float> restLength;

StructuredBuffer<int> neighborTriangles;
StructuredBuffer<float> restAngle;

RWStructuredBuffer<uint3> deltaPosAsInt;
RWStructuredBuffer<int> deltaCount;

float deltaT;
float damping;
float gravity;
static const uint MAX_VERTICES_PER_BIN = 32;
static const float EPSILON = 0.00001;

int totalParticles;
int totalEdges;
int totalNeighborTriangles;

float stretchStiffness;
float compressionStiffness;
float bendingStiffness;
float thickness;