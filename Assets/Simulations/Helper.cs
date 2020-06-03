using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace Helper
{
  // we use float[3] rather than Vector3 in order to store precise position data in the format of JSON
  #region Structs
  public struct Triangleids
  {
    public int A;
    public int B;
    public int C;

    public float AB;
    public float BC;
    public float CA;
  }

  public struct VertexData
  {
    public List<float[]> position;
    public Dictionary<int, List<int>> custom2raw;
    public Dictionary<int, int> raw2custom;
    public List<Triangleids> sortedTriangles;
  }
  #endregion

  public class _Convert
  {

    static public float[] Vector3ToFloat(Vector3 vect)
    {
      return new float[3]{vect.x, vect.y, vect.z};
    }

    static public Vector3 FloatToVector3(float[] flts)
    {
      if (flts.Length == 3)
      {
        return new Vector3(flts[0], flts[1], flts[2]);
      } else
      {
        Debug.LogError("Float passed does not have exactly length of 3");
        return new Vector3(0, 0, 0);
      }
    }

    static public Vector3[] FloatListToVector3List(List<float[]> fltList)
    {
      Vector3[] vc3List = new Vector3[fltList.Count];

      for (int i=0; i < fltList.Count; i++)
      {
        vc3List[i] = FloatToVector3(fltList[i]);
      }

      return vc3List;
    }

    static public Vector3[] LocalVector3ListToWold(Transform transform, Vector3[] vc3List)
    {
      for (int i=0; i < vc3List.Length; i++)
      {
        vc3List[i] = transform.TransformPoint(vc3List[i]);
      }
      return vc3List;
    }

    static public Vector3[] WoldVector3ListToLocal(Transform transform, Vector3[] vc3List)
    {
      for (int i=0; i < vc3List.Length; i++)
      {
        vc3List[i] = transform.InverseTransformPoint(vc3List[i]);
      }
      return vc3List;
    }

  }
  
  public class _Check
  {

    static public bool ListContainsFloatArray(List<float[]> list, float[] arr, out int element)
    {
      // return element id if found similar array, return element length if no similar array found
      for (int i=0; i < list.Count; i++)
      {
        if (list[i].SequenceEqual(arr))
        {
          element = (int)i;
          return true;
        }
      }
      element = (int)list.Count;
      return false;
    }

    static public List<int[]> RemoveSimilarTriangleGrps(List<int[]> sortedTri)
    {
      for (int i=0; i < sortedTri.Count; i++)
      {
        for (int _i=0; _i < sortedTri.Count; _i++)
        {
          bool similar = true;
          for (int _ii=0; _ii < 3; _ii++)
          {
            if (!sortedTri[i].Contains(sortedTri[_i][_ii]))
            {
              similar = false;
              break;
            }
          }
          if (similar)
          {
            sortedTri.RemoveAt(_i);
          }
        }
      }
      return sortedTri;
    }

  }

  public class _Vertex
  {

    static public VertexData SortVerticesByPosition(int totalV, Vector3[] v)
    {
      VertexData vd = new VertexData();
      vd.position = new List<float[]>();
      vd.custom2raw = new Dictionary<int, List<int>>();
      vd.raw2custom = new Dictionary<int, int>();

      // sort the vertices
      for (int i=0; i < totalV; i++)
      {
        float[] vertFloats = _Convert.Vector3ToFloat(v[i]);
        int element = 0;
        if (!_Check.ListContainsFloatArray(vd.position, vertFloats, out element))
        {
          vd.position.Add(vertFloats);
          vd.custom2raw.Add(element, new List<int>());
        }
        vd.custom2raw[element].Add(i);
      }

      // used sorted vertices to populate raw2custom
      for (int i=0; i < vd.custom2raw.Count; i++)
      {
        foreach (int id in vd.custom2raw[i])
        {
          vd.raw2custom[id] = i;
        }
      }
      return vd;
    }

    static public VertexData SortTrianglesByGrp(int[] tri, int totalTrianglePoints, VertexData vd)
    {
      vd.sortedTriangles = new List<Triangleids>();

      for (int i=0; i < totalTrianglePoints; i+=3)
      {
        Triangleids t = new Triangleids();
        t.A = vd.raw2custom[tri[i]];
        t.B = vd.raw2custom[tri[i+1]];
        t.C = vd.raw2custom[tri[i+2]];

        t.AB = CalculateCustomidDistance(t.A, t.B, vd);
        t.BC = CalculateCustomidDistance(t.B, t.C, vd);
        t.CA = CalculateCustomidDistance(t.C, t.A, vd);
        vd.sortedTriangles.Add(t);
      }
      return vd;
    }

    static public void InitRawMesh(Mesh mesh, out Vector3[] verts, out int totalVerts, out int[] triangles, out int totalTrianglePoints)
    {
      verts = mesh.vertices;
      totalVerts = (int)verts.Length;
      triangles = mesh.triangles;
      totalTrianglePoints = (int)triangles.Length;
    }

    static public void ResetMeshData(VertexData vd, Vector3[] verts, Mesh mesh)
    {
      for (int i=0; i < vd.custom2raw.Count; i++)
      {
        foreach (int id in vd.custom2raw[(int)i])
        {
          verts[id] = _Convert.FloatToVector3(vd.position[i]);
        }
      }
      mesh.vertices = verts;
    }

    static public float CalculateCustomidDistance(int id1, int id2, VertexData vd)
    {
      return Vector3.Distance(_Convert.FloatToVector3(vd.position[id1]), _Convert.FloatToVector3(vd.position[id2]));
    }

    #region Saving and Loading
    static public bool CheckSaveFolder(string folder)
    {
      if (folder != "" && folder != null)
      {
        if (!Directory.Exists($"{Application.streamingAssetsPath}/{folder}"))
        {
          Directory.CreateDirectory($"{Application.streamingAssetsPath}/{folder}");
        }
        return true;
      } else
      {
        Debug.LogError("Please specify the folder you want to save at in the inspector");
        return false;
      }
    }

    static public void SaveData(string filename,  string folder, VertexData vd)
    {
      if (CheckSaveFolder(folder))
      {
        string savePath = $"{Application.streamingAssetsPath}/{folder}/{filename}.json";
        string json = JsonConvert.SerializeObject(vd);
        File.WriteAllText(savePath, json);
        Debug.Log($"Cached particles data at {savePath}");
      }
    }

    static public VertexData LoadData(string filename, string folder)
    {
      if (CheckSaveFolder(folder))
      {
        string savePath = $"{Application.streamingAssetsPath}/{folder}/{filename}.json";
        string json = File.ReadAllText(savePath);
        VertexData vd = JsonConvert.DeserializeObject<VertexData>(json);
        Debug.Log($"Loaded cached particles data at {savePath}");
        return vd;
      } else
      {
        Debug.LogError($"An error occured when loading VertexData data from {filename}");
        return new VertexData();
      }
    }
    #endregion

  }
}