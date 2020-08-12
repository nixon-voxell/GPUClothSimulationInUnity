using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DataStruct;
using Utilities;

namespace SpatialHashing
{
  public class SH
  {
    public static int Hash(Vector3 coordinate, int gridSize, int tableSize)
    {
      int p0 = 73856093;
      int p1 = 19349663;
      int p2 = 83492791;

      int x = Mathf.RoundToInt(coordinate.x / gridSize);
      int y = Mathf.RoundToInt(coordinate.y / gridSize);
      int z = Mathf.RoundToInt(coordinate.z / gridSize);

       return (x*p0 ^ y*p1 ^ z*p2) % tableSize;
    }
  }
}