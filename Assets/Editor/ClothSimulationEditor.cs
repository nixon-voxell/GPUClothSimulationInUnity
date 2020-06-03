using UnityEngine;
using UnityEditor;
using Helper;

[CustomEditor(typeof(ClothSimulation))]
class ClothSimulationEditor : Editor {

  ClothSimulation clothSim;

  void OnEnable() {
    clothSim = (ClothSimulation)target;
  }

  public override void OnInspectorGUI()
  {
    clothSim.mesh = clothSim.GetComponent<SkinnedMeshRenderer>().sharedMesh;
    clothSim.particleInvertMass = 1 / clothSim.particleMass;

    _Vertex.InitRawMesh(clothSim.mesh,
    out clothSim.verts,
    out clothSim.totalVerts,
    out clothSim.triangles,
    out clothSim.totalTrianglePoints);


    DrawDefaultInspector();
    GUILayout.Space(20);

    if (clothSim.mesh == null) GUILayout.Label("Please add a SkinnedMeshRenderer component first");
    else
    {
      GUILayout.BeginVertical("box");
      GUILayout.BeginHorizontal();
      GUI.backgroundColor = Color.cyan;
      if (GUILayout.Button("Sort Mesh Data"))
      {
        clothSim.SortMeshData();
      }
      GUI.backgroundColor = Color.yellow;
      if (GUILayout.Button("Reload Mesh Data"))
      {
        clothSim.ReloadMeshData();
      }
      GUILayout.EndHorizontal();
      GUI.backgroundColor = Color.green;
      if (GUILayout.Button("Save Mesh Data"))
      {
        clothSim.SaveMeshData();
      }
      GUILayout.EndVertical();
    }

  }

}