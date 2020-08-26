using UnityEngine;
using UnityEditor;
using Helper;

[CustomEditor(typeof(GPUClothSimulationLegacy))]
class GPUClothSimulationLegacyEditor : Editor {

  GPUClothSimulationLegacy clothSim;

  void OnEnable()
  {
    clothSim = (GPUClothSimulationLegacy)target;
  }

  public override void OnInspectorGUI()
  {
    clothSim.mesh = clothSim.GetComponent<SkinnedMeshRenderer>().sharedMesh;
    clothSim.particleInvertMass = 1 / clothSim.particleMass;
    clothSim.restAngle = Mathf.Acos(clothSim.bendiness);

    _Vertex.InitRawMesh(clothSim.mesh,
    out clothSim.totalVerts,
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