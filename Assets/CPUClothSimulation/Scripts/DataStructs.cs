using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace DataStruct
{
  [Serializable]
  public struct Particle
  {
    public float[] pos;
    public float[] predicted_pos;
    public float[] velocity;
    public float invMass;
    public int idx;
    public int phase;
  }

  [Serializable]
  public struct Edge
  {
    public int p0;
    public int p1;
    public float dist;
    public int idx;
  }

  [Serializable]
  public struct Triangle
  {
    public int p0;
    public int p1;
    public int p2;
    public int idx;
  }

  [Serializable]
  public struct NeighborTriangles
  {
    public int p0;
    public int p1;
    public int p2;
    public int p3;
    public float restAngle;
  }

  [Serializable]
  public struct MeshData
  {
    public Particle[] particles;
    public Edge[] edges;
    public Triangle[] triangles;
    public NeighborTriangles[] neighborTriangles;
    public int[] sequence;
  }
}