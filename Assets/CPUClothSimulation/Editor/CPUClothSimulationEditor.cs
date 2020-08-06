using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.IO;
using Newtonsoft.Json;

using DataStruct;
using Utilities;

[CustomEditor(typeof(CPUClothSimulation))]
public class OpenFolderPanelExample : Editor
{
  CPUClothSimulation clothSim;
  GUIStyle boldTextStyle = new GUIStyle();

  List<Vector3> meshVerts;
  int[] meshTriangles;

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
        clothSim.meshData = JsonConvert.DeserializeObject<MeshData>(jsonString);

        if (clothSim.meshData.sequence != null)
        {
          meshTriangles = clothSim.meshData.sequence;
        }

        if (clothSim.meshData.particles != null)
        {
          meshVerts = new List<Vector3>();
          foreach (Particle p in clothSim.meshData.particles)
          {
            meshVerts.Add(_Convert.FloatToVector3(p.pos));
          }
          clothSim.totalVerts = clothSim.meshData.particles.Length;
        }

        if (clothSim.meshData.edges != null)
        {
          clothSim.totalEdges = clothSim.meshData.edges.Length;
        }
        
        if (clothSim.meshData.triangles != null)
        {
          clothSim.totalTriangles = clothSim.meshData.triangles.Length;
        }

        if (clothSim.meshData.neighborTriangles != null)
        {
          clothSim.totalNeighborTriangles = clothSim.meshData.neighborTriangles.Length;
        }
      }

      clothSim.originMeshData = clothSim.meshData;
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

    GUI.backgroundColor = Color.yellow;
    if (GUILayout.Button("Simulate"))
    {
      clothSim.SimulateOneTimeStep(clothSim.deltaTimeStep);
    }
    if (GUILayout.Button("Revert to Original"))
    {
      clothSim.meshData = clothSim.originMeshData;
    }
    GUILayout.EndVertical();
  }
}