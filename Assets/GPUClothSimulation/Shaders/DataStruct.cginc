struct Particle
{
  float3 pos;
  float3 predictedPos;
  float3 velocity;
  int idx;
  int phase;
};

struct Edge
{
  int p0;
  int p1;
  int p2;
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