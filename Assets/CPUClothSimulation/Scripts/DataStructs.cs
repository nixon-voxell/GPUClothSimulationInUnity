using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace DataStruct
{
  [Serializable]
  public class Particle
  {
    public Vector3 pos;
    public Vector3 predictedPos;
    public Vector3 velocity;
    public float invMass;
    public int idx;
    public int phase;

    public Particle(
      float[] _pos,
      float[] _predictedPos,
      float[] _veloctiy,
      float _invMass,
      int _idx,
      int _phase)
    {
      pos = new Vector3(_pos[0], _pos[1], _pos[2]);
      predictedPos = new Vector3(_predictedPos[0], _predictedPos[1], _predictedPos[2]);
      velocity = new Vector3(_veloctiy[0], _veloctiy[1], _veloctiy[2]);
      invMass = _invMass;
      idx = _idx;
      phase = _phase;
    }
  }

  [Serializable]
  public struct Edge
  {
    public int p0;
    public int p1;
    public float restLength;
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