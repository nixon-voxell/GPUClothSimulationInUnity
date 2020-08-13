using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DataStruct;
using Utilities;

namespace SpatialHashing
{
  public class SH
  {
    int p0 = 73856093;
    int p1 = 19349663;
    int p2 = 83492791;

    int gridSize;
    float invGridSize;
    int tableSize;

    public SH(int _gridSize, float _invGridSize, int _tableSize)
    {
      gridSize = _gridSize;
      invGridSize = _invGridSize;
      tableSize = _tableSize;
    }

    public int Hash(Vector3 coordinate)
    {
      int x = Mathf.RoundToInt(coordinate.x * invGridSize);
      int y = Mathf.RoundToInt(coordinate.y * invGridSize);
      int z = Mathf.RoundToInt(coordinate.z * invGridSize);

       return (x*p0 ^ y*p1 ^ z*p2) % tableSize;
    }

    public List<int> TriangleBoundingBoxHashes(Vector3 p0, Vector3 p1, Vector3 p2)
    {
      int minX = Mathf.RoundToInt(Mathf.Min(new float[3]{p0.x, p1.x, p2.x}));
      int minY = Mathf.RoundToInt(Mathf.Min(new float[3]{p0.y, p1.y, p2.y}));
      int minZ = Mathf.RoundToInt(Mathf.Min(new float[3]{p0.z, p1.z, p2.z}));

      int maxX = Mathf.RoundToInt(Mathf.Max(new float[3]{p0.x, p1.x, p2.x}));
      int maxY = Mathf.RoundToInt(Mathf.Max(new float[3]{p0.y, p1.y, p2.y}));
      int maxZ = Mathf.RoundToInt(Mathf.Max(new float[3]{p0.z, p1.z, p2.z}));

      List<int> hashes = new List<int>();
      for (int x=minX; x <= maxX; x+=gridSize)
      {
        for (int y=minY; y <= maxY; y+=gridSize)
        {
          for (int z=minZ; z <= maxZ; z+=gridSize)
          {
            hashes.Add(Mathf.Abs(Hash(new Vector3(x, y, z))));
          }
        }
      }

      return hashes;
    }

    // public static int[] CollisionSearchTriangle(Vector3 p0, Vector3 p1, Vector3 p2, )
  }
}