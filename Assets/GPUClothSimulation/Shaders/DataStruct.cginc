struct Particle
{
  float3 pos;
  float3 predictedPos;
  float3 velocity;
  float invMass;
  int idx;
  int phase;
};

struct Edge
{
  int p0;
  int p1;
  float restLength;
  int idx;
};

struct Triangle
{
  int p0;
  int p1;
  int p2;
  int idx;
};

struct NeighborTriangles
{
  int p0;
  int p1;
  int p2;
  int p3;
  float restAngle;
};

RWStructuredBuffer<Particle> particles;
RWStructuredBuffer<Edge> edges;
RWStructuredBuffer<Triangle> triangeles;
RWStructuredBuffer<NeighborTriangles> neighborTriangles;
RWStructuredBuffer<uint3> deltaPosAsInt;
RWStructuredBuffer<int> deltaCount;

float deltaT;
float damping;
float gravity;
static const uint MAX_VERTICES_PER_BIN = 32;
static const float EPSILON = 0.00001;

float stretchStiffness;
float compressionStiffness;
float bendingStiffness;