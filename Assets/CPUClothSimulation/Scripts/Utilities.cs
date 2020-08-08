using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Utilities
{
  public class _Convert
  {

    public static float[] Vector3ToFloat(Vector3 vect)
    {
      return new float[3]{vect.x, vect.y, vect.z};
    }

    public static Vector3 FloatToVector3(float[] flts)
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

    public static Vector3[] FloatArrayToVector3Array(float[][] fltArray)
    {
      if (fltArray != null)
      {
        Vector3[] vc3Array = new Vector3[fltArray.Length];

        for (int i=0; i < fltArray.Length; i++)
        {
          vc3Array[i] = FloatToVector3(fltArray[i]);
        }

        return vc3Array;
      } else
      {
        return new Vector3[0];
      }
    }

    public static float[][] Vector3ArrayToFloatArray(Vector3[] vc3Array)
    {
      if (vc3Array != null)
      {
        float[][] fltArray = new float[vc3Array.Length][];

        for (int i=0; i < vc3Array.Length; i++)
        {
          fltArray[i] = Vector3ToFloat(vc3Array[i]);
        }

        return fltArray;
      } else
      {
        return new float[0][];
      }
    }

    public static Vector3[] LocalVector3ListToWold(Transform transform, Vector3[] vc3List)
    {
      for (int i=0; i < vc3List.Length; i++)
      {
        vc3List[i] = transform.TransformPoint(vc3List[i]);
      }
      return vc3List;
    }

    public static Vector3[] WoldVector3ListToLocal(Transform transform, Vector3[] vc3List)
    {
      for (int i=0; i < vc3List.Length; i++)
      {
        vc3List[i] = transform.InverseTransformPoint(vc3List[i]);
      }
      return vc3List;
    }

  }

  public class _Math
  {
    public static float[] AddFloatArray(float[] floatArray1, float[] floatArray2)
    {
      if (floatArray1.Length == floatArray2.Length)
      {
        float[] finalFloatArray = new float[floatArray1.Length];
        for (int i=0; i < floatArray1.Length; i++)
        {
          finalFloatArray[i] = floatArray1[i] + floatArray2[i];
        }
        return finalFloatArray;
      } else
      {
        return null;
      }
    }

    public static float[] SubtractFloatArray(float[] floatArray1, float[] floatArray2)
    {
      if (floatArray1.Length == floatArray2.Length)
      {
        float[] finalFloatArray = new float[floatArray1.Length];
        for (int i=0; i < floatArray1.Length; i++)
        {
          finalFloatArray[i] = floatArray1[i] - floatArray2[i];
        }
        return finalFloatArray;
      } else
      {
        return null;
      }
    }

  }

  public class _Mesh
  {
    public static Vector3[] ReverseNormals(Vector3[] normals)
    {
      Vector3[] reverseNormals = normals;
      for (int i = 0; i < reverseNormals.Length; i++)
      {
        reverseNormals[i] *= -1;
      }
      return reverseNormals;
    }

    public static Mesh DeepCopyMesh(Mesh mesh)
    {
      Mesh newMesh = new Mesh();
      newMesh.vertices = mesh.vertices;
      newMesh.triangles = mesh.triangles;
      newMesh.RecalculateBounds();
      newMesh.RecalculateNormals();
      newMesh.RecalculateTangents();
      return newMesh;
    }
  }

}