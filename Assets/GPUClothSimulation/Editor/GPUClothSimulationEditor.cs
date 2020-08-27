using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.IO;
using Newtonsoft.Json;

using DataStruct;
using Utilities;

[CustomEditor(typeof(GPUClothSimulation))]
public class GPUClothSimulationEditor : Editor
{
  GPUClothSimulation clothSim;
  GUIStyle boldTextStyle = new GUIStyle();

  List<Vector3> meshVerts;
  int[] meshTriangles;

  void OnEnable()
  {
    clothSim = (GPUClothSimulation)target;
    if (EditorGUIUtility.isProSkin) boldTextStyle.normal.textColor = new Color(1, 1, 1, 0.8f);
    else boldTextStyle.normal.textColor = Color.black;
    boldTextStyle.fontStyle = FontStyle.Bold;
  }

  public override void OnInspectorGUI()
  {
    if (clothSim.gridSize > 0) clothSim.invGridSize = 1.0f / clothSim.gridSize;
    else clothSim.gridSize = 0;

    GUILayout.Label("Initializaion", boldTextStyle);

    // JSON
    GUI.backgroundColor = Color.yellow;
    if (GUILayout.Button("Select JSON File"))
    {
      SelectFile();
    }
    if (GUILayout.Button("Load Data from JSON"))
    {
      LoadDataFromJson();
    }

    // build front mesh
    GUI.backgroundColor = Color.magenta;
    if (clothSim.mesh == null)
    {
      if (GUILayout.Button("Build Mesh")) BuildMesh();
    } else
    {
      if (GUILayout.Button("Rebuild Mesh")) BuildMesh();
    }
    

    GUILayout.Space(10);

    // Create mesh for the other side
    GUI.backgroundColor = Color.magenta;
    if (clothSim.transform.childCount == 0)
    {
      if (GUILayout.Button("Create Back Side"))
      {
        clothSim.backSide = new GameObject("BackSide");
        clothSim.backSide.transform.parent = clothSim.transform;
        BuildBackSide();
      }
    } else
    {
      if (GUILayout.Button("Recreate Back Side"))
      {
        BuildBackSide();
      }
    }
    
    // apply materials for front and back of the mesh
    GUI.backgroundColor = Color.yellow;
    if (GUILayout.Button("Apply Materials"))
    {
      clothSim.GetComponent<MeshRenderer>().material = clothSim.frontMaterial;
      clothSim.transform.GetChild(0).GetComponent<MeshRenderer>().material = clothSim.backMaterial;
    }

    // draw the default inspector with all the public variables
    GUI.backgroundColor = Color.white;
    DrawDefaultInspector();

    GUILayout.Space(10);

    // simulate one time step in editor and play mode
    GUI.backgroundColor = new Color(0.8f, 0.9f, 1f, 1f);
    if (GUILayout.Button("Simulate 1 Time Step"))
    {
      clothSim.SimulateOneTimeStep(clothSim.deltaTimeStep);
      clothSim.UpdateDataToMesh(clothSim.deltaTimeStep);
    }

    // allow user to control if we want to start or stop stimulating the cloth in play mode
    if (clothSim.simulate)
    {
      GUI.backgroundColor = Color.red;
      if (GUILayout.Button("Stop Simulation"))
      {
        clothSim.simulate = false;
      }
    } else
    {
      GUI.backgroundColor = Color.cyan;
      if (GUILayout.Button("Start Simulation"))
      {
        clothSim.simulate = true;
      }
    }

    GUI.backgroundColor = Color.green;
    if (GUILayout.Button("Revert to Original"))
    {
      LoadDataFromJson(); BuildMesh(); BuildBackSide();
    }
  }

  #region Button Functions
  void LoadDataFromJson()
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
          meshVerts.Add(p.pos);
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
  }

  void BuildMesh()
  {
    if (meshVerts != null && meshTriangles != null)
    {
      if (clothSim.mesh == null) clothSim.mesh = new Mesh();
      else clothSim.mesh.Clear();
      clothSim.mesh.SetVertices(meshVerts);
      clothSim.mesh.SetTriangles(meshTriangles, 0);
      clothSim.mesh.RecalculateNormals();
      clothSim.mesh.MarkDynamic();
      clothSim.mesh.name = clothSim.path.Split('/')[clothSim.path.Split('/').Length-1].Split('.')[0];

      clothSim.GetComponent<MeshFilter>().sharedMesh = clothSim.mesh;
    } else
    {
      Debug.LogWarning("Mesh Data has not been loaded yet...");
    }
  }

  void SelectFile()
  {
    clothSim.path = EditorUtility.OpenFilePanel("Load png Textures", "", "");
    if (clothSim.path.StartsWith(Application.dataPath))
      clothSim.path = clothSim.path.Substring(Application.dataPath.Length);
  }

  void BuildBackSide()
  {
    clothSim.backSide.transform.localPosition = Vector3.zero;
    clothSim.backSide.transform.localRotation = Quaternion.Euler(0, 0, 0);
    clothSim.backSide.transform.localScale = new Vector3(1, 1, 1);

    if (clothSim.backSide.GetComponent<MeshFilter>() == null) clothSim.backSide.AddComponent(typeof(MeshFilter));
    if (clothSim.backSide.GetComponent<MeshRenderer>() == null) clothSim.backSide.AddComponent(typeof(MeshRenderer));

    clothSim.childMesh = _Mesh.DeepCopyMesh(clothSim.mesh);
    clothSim.childMesh.MarkDynamic();
    clothSim.childMesh.name = clothSim.mesh.name + "Back";
    // clothSim.childMesh.normals = _Mesh.ReverseNormals(clothSim.mesh.normals);

    // reverse the triangle order
    for (int m = 0; m < clothSim.childMesh.subMeshCount; m++) {
      int[] triangles = clothSim.childMesh.GetTriangles(m);
      for (int i = 0; i < triangles.Length; i += 3) {
        int temp = triangles[i + 0];
        triangles[i + 0] = triangles[i + 1];
        triangles[i + 1] = temp;
      }
      clothSim.childMesh.SetTriangles(triangles, m);
    }

    clothSim.childMesh.RecalculateNormals();
    clothSim.backSide.GetComponent<MeshFilter>().sharedMesh = clothSim.childMesh;
  }
  #endregion
}