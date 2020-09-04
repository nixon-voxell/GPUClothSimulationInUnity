// We use SoA instead of AoS
#ifdef SHADER_API_D3D11
RWStructuredBuffer<float3> pos;
RWStructuredBuffer<float3> predictedPos;
RWStructuredBuffer<float3> velocity;
RWStructuredBuffer<float> mass;
RWStructuredBuffer<float> invMass;

StructuredBuffer<int> edge;
StructuredBuffer<float> restLength;

StructuredBuffer<int> neighborTriangle;
StructuredBuffer<float> restAngle;

StructuredBuffer<int> tri;

RWStructuredBuffer<uint3> deltaPosUint;
RWStructuredBuffer<int> deltaCount;
#endif

float deltaT;
static const uint MAX_VERTICES_PER_BIN = 32;
static const float EPSILON = 0.00001;

uint totalVerts;
uint totalEdges;
uint totalTriangles;
uint totalNeighborTriangles;

float damping;
float3 gravity;
float stretchStiffness;
float compressionStiffness;
float bendingStiffness;
float thickness;