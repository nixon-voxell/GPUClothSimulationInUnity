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
public class CPUClothSimulationEditor : Editor
{
  CPUClothSimulation clothSim;

  List<Vector3> meshVerts;
  int[] meshTriangles;

  const int SpaceA = 30, SpaceB = 10;

  GUIStyle centeredLabelStyle, foldoutStyle, subFoldoutStyle, notes, box;

  void EnsureStyles()
  {
    centeredLabelStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
    centeredLabelStyle.alignment = TextAnchor.UpperCenter;
    centeredLabelStyle.fontStyle = FontStyle.Bold;
    centeredLabelStyle.fontSize = 12;

    foldoutStyle = new GUIStyle(EditorStyles.foldout);
    foldoutStyle.fontStyle = FontStyle.Bold;
    foldoutStyle.fontSize = 14;
    foldoutStyle.normal.textColor = Color.gray;
    foldoutStyle.onNormal.textColor = new Color(0.7f, 1f, 1f, 1f);

    subFoldoutStyle = new GUIStyle(EditorStyles.foldout);
    subFoldoutStyle.fontStyle = FontStyle.Bold;
    subFoldoutStyle.fontSize = 12;
    subFoldoutStyle.normal.textColor = Color.gray;
    subFoldoutStyle.onNormal.textColor = new Color(0.7f, 1f, 1f, 1f);

    notes = new GUIStyle(GUI.skin.GetStyle("label"));
    notes.fontStyle = FontStyle.Italic;
    notes.fontSize = 10;
    notes.alignment = TextAnchor.MiddleRight;

    box = GUI.skin.box;
    box.padding = new RectOffset(10, 10, 10, 10);
  }

  void OnEnable()
  {
    clothSim = (CPUClothSimulation)target;
  }

  public override void OnInspectorGUI()
  {
    if (GUILayout.Button("Refresh Editor Layout")) EnsureStyles();
    if (centeredLabelStyle == null) EnsureStyles();

    if (clothSim.gridSize > 0) clothSim.invGridSize = 1.0f / clothSim.gridSize;
    else clothSim.gridSize = 0;

    GUILayout.Space(SpaceB);
    EditorGUI.BeginChangeCheck();

    #region Initialization
    clothSim.showInitialization = EditorGUILayout.Foldout(clothSim.showInitialization, "Initialization", true, foldoutStyle);
    if (clothSim.showInitialization)
    {
      GUILayout.BeginVertical(box);
      // JSON
      GUI.backgroundColor = Color.yellow;
      if (GUILayout.Button("Select JSON File")) SelectFile();
      GUI.backgroundColor = Color.white;
      EditorGUILayout.PropertyField(serializedObject.FindProperty("path"), new GUIContent("JSON File"));

      GUILayout.Space(SpaceB);

      GUI.backgroundColor = Color.yellow;
      if (GUILayout.Button("Load Data from JSON")) LoadDataFromJson();
      GUI.backgroundColor = Color.white;
      EditorGUILayout.PropertyField(serializedObject.FindProperty("totalVerts"), new GUIContent("Total Vertices/Particles"));
      EditorGUILayout.PropertyField(serializedObject.FindProperty("totalEdges"), new GUIContent("Total Edges"));
      EditorGUILayout.PropertyField(serializedObject.FindProperty("totalTriangles"), new GUIContent("Total Triangles"));
      EditorGUILayout.PropertyField(serializedObject.FindProperty("totalNeighborTriangles"), new GUIContent("Total Neighbor Triangles"));

      GUILayout.Space(SpaceB);

      // build front mesh
      GUI.backgroundColor = Color.magenta;
      if (clothSim.mesh == null)
      {
        if (GUILayout.Button("Build Mesh")) BuildMesh();
      } else
      {
        if (GUILayout.Button("Rebuild Mesh")) BuildMesh();
      }
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
        if (GUILayout.Button("Recreate Back Side")) BuildBackSide();
      }
      GUILayout.EndVertical();
      GUILayout.Space(SpaceA);
    }
    GUI.backgroundColor = Color.white;
    #endregion

    #region Materials
    clothSim.showMaterials = EditorGUILayout.Foldout(clothSim.showMaterials, "Materials", true, foldoutStyle);
    if (clothSim.showMaterials)
    {
      GUILayout.BeginVertical(box);
      EditorGUILayout.PropertyField(serializedObject.FindProperty("frontMaterial"), new GUIContent("Front Material"));
      EditorGUILayout.PropertyField(serializedObject.FindProperty("backMaterial"), new GUIContent("Back Material"));
      GUILayout.Space(SpaceB);
      // apply materials for front and back of the mesh
      GUI.backgroundColor = Color.cyan;
      if (GUILayout.Button("Apply Materials"))
      {
        clothSim.GetComponent<MeshRenderer>().material = clothSim.frontMaterial;
        clothSim.transform.GetChild(0).GetComponent<MeshRenderer>().material = clothSim.backMaterial;
      }
      GUILayout.EndVertical();
      GUILayout.Space(SpaceA);
    }
    GUI.backgroundColor = Color.white;
    #endregion

    #region Cloth Parameters
    clothSim.showClothParameters = EditorGUILayout.Foldout(clothSim.showClothParameters, "Cloth Parameters", true, foldoutStyle);
    if (clothSim.showClothParameters)
    {
      GUILayout.BeginVertical(box);
      EditorGUILayout.PropertyField(serializedObject.FindProperty("gravity"), new GUIContent("Gravity"));
      EditorGUILayout.PropertyField(serializedObject.FindProperty("damping"), new GUIContent("Veloctiy Damping"));
      EditorGUILayout.PropertyField(serializedObject.FindProperty("wind"), new GUIContent("Directional Wind Zone"));
      GUILayout.Space(SpaceB);
      EditorGUILayout.LabelField("Distance Constraint", notes);
      EditorGUILayout.PropertyField(serializedObject.FindProperty("compressionStiffness"), new GUIContent("Compression Stiffness"));
      EditorGUILayout.PropertyField(serializedObject.FindProperty("stretchStiffness"), new GUIContent("Stretch Stiffness"));
      GUILayout.Space(SpaceB);
      EditorGUILayout.LabelField("Bending Constraint", notes);
      EditorGUILayout.PropertyField(serializedObject.FindProperty("bendingStiffness"), new GUIContent("Bending Stiffness"));
      GUILayout.Space(SpaceB);
      EditorGUILayout.LabelField("Self Collision", notes);
      EditorGUILayout.PropertyField(serializedObject.FindProperty("thickness"), new GUIContent("Cloth Thickness"));
      GUILayout.Space(SpaceB*2);

      clothSim.showSpatialHashing = EditorGUILayout.Foldout(clothSim.showSpatialHashing, "Spatial Hashing", true, subFoldoutStyle);
      if (clothSim.showSpatialHashing)
      {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("gridSize"), new GUIContent("Grid Size"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("invGridSize"), new GUIContent("Invert Grid Size"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("tableSize"), new GUIContent("Hash Table Size"));
        
        GUILayout.Space(SpaceB);
      }

      clothSim.showSimulationSettings = EditorGUILayout.Foldout(clothSim.showSimulationSettings, "Simulation Settings", true, subFoldoutStyle);
      if (clothSim.showSimulationSettings)
      {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("iterationSteps"), new GUIContent("Iterations Steps"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("deltaTimeStep"), new GUIContent("Delta Time Step"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("startSimulationOnPlay"), new GUIContent("Start Simulation On Play?"));

        GUILayout.Space(SpaceB);
      }
      GUILayout.EndVertical();
      GUILayout.Space(SpaceA);
    }
    GUI.backgroundColor = Color.white;
    #endregion

    GUILayout.Space(SpaceB);

    #region Testing
    GUI.backgroundColor = Color.black;
    GUILayout.BeginVertical(box);
    // GUILayout.Space(SpaceB);
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
    GUI.backgroundColor = Color.white;
    // GUILayout.Space(SpaceB);
    GUILayout.EndVertical();
    #endregion

    GUILayout.Space(SpaceA);

    #region End
    EditorGUILayout.LabelField("~ DO NOT MODIFY BELOW ~", centeredLabelStyle);
    if(EditorGUI.EndChangeCheck()) EditorApplication.QueuePlayerLoopUpdate();
    serializedObject.ApplyModifiedProperties();
    clothSim.showDefault = EditorGUILayout.Foldout(clothSim.showDefault, "Default Inspector", subFoldoutStyle);
    if (clothSim.showDefault)
    {
      DrawDefaultInspector();
    }
    #endregion
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