using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.IO;
using Newtonsoft.Json;

using DataStruct;

[CustomEditor(typeof(CPUClothSimulation))]
public class OpenFolderPanelExample : Editor
{
  CPUClothSimulation clothSim;
  GUIStyle boldTextStyle = new GUIStyle();

  List<Vector3> meshVerts;
  int[] meshTriangles;
  MeshData meshData;

  void OnEnable()
  {
    clothSim = (CPUClothSimulation)target;
    boldTextStyle.normal.textColor = Color.black;
    boldTextStyle.fontStyle = FontStyle.Bold;
  }

  public override void OnInspectorGUI()
  {
    GUI.backgroundColor = new Color(0, 0, 0, 0.1f);
    GUILayout.BeginVertical("box");
    GUILayout.Label("Initializaion", boldTextStyle);
    GUI.backgroundColor = Color.cyan;
    if (GUILayout.Button("Select JSON File"))
    {
      clothSim.path = EditorUtility.OpenFilePanel("Load png Textures", "", "");
      if (clothSim.path.StartsWith(Application.dataPath))
        clothSim.path = clothSim.path.Substring(Application.dataPath.Length);
    }
    if (GUILayout.Button("Load Data from Json"))
    {
      if (!File.Exists(Application.dataPath + clothSim.path))
      {
        Debug.LogWarning("Json file does not exsits...");
      } else
      {
        string jsonString = File.ReadAllText(Application.dataPath + clothSim.path);
        meshData = JsonConvert.DeserializeObject<MeshData>(jsonString);

        if (meshData.sequence != null)
        {
          meshTriangles = meshData.sequence;
        }

        if (meshData.particles != null)
        {
          meshVerts = new List<Vector3>();
          foreach (Particle p in meshData.particles)
          {
            meshVerts.Add(_Convert.FloatToVector3(p.pos));
          }
          clothSim.totalVerts = meshData.particles.Length;
        }

        if (meshData.edges != null)
        {
          clothSim.totalEdges = meshData.edges.Length;
        }
        
        if (meshData.triangles != null)
        {
          clothSim.totalTriangles = meshData.triangles.Length;
        }

        if (meshData.neighborTriangles != null)
        {
          clothSim.totalNeighborTriangles = meshData.neighborTriangles.Length;
        }
      }
    }
    if (GUILayout.Button("Build Mesh"))
    {
      if (meshVerts != null && meshTriangles != null)
      {
        clothSim.mesh = new Mesh();
        clothSim.mesh.SetVertices(meshVerts);
        clothSim.mesh.SetTriangles(meshTriangles, 0);
        clothSim.mesh.RecalculateNormals();
        clothSim.ShowMesh();
      } else
      {
        Debug.LogWarning("Mesh Data has not been loaded yet...");
      }
    }
    GUI.backgroundColor = Color.white;
    DrawDefaultInspector();
    GUILayout.EndVertical();
  }
}


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