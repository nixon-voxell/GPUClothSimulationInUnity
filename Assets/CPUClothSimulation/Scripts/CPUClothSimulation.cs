using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DataStruct;
using Utilities;

using PositionBasedDynamics;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class CPUClothSimulation : MonoBehaviour
{
  #region Initializattion;
  [ConditionalHideAttribute("hide")]
  public string path;
  [ConditionalHideAttribute("hide")]
  public int totalVerts;
  [ConditionalHideAttribute("hide")]
  public int totalEdges;
  [ConditionalHideAttribute("hide")]
  public int totalTriangles;
  [ConditionalHideAttribute("hide")]
  public int totalNeighborTriangles;

  [HideInInspector]
  public Mesh mesh;
  [HideInInspector]
  public Mesh originMesh;
  [HideInInspector]
  public MeshData meshData;
  [HideInInspector]
  public MeshData originMeshData;
  [HideInInspector]
  public Mesh childMesh;
  [HideInInspector]
  public GameObject backSide;
  #endregion

  #region Materials
  [Header("Materials")]
  public Material frontMaterial;
  public Material backMaterial;
  #endregion

  #region Cloth Simulation Parameters
  [Header("Cloth Simulation Parameters")]
  public Vector3 gravity = new Vector3(0, -9.81f, 0);
  [Range(0, 1)]
  public float compressionStiffness = 1;
  [Range(0, 1)]
  public float strechStiffness = 1;
  [Range(0, 1)]
  public float bendingStiffness = 0.1f;
  public float thickness = 0.02f;
  [Range(0.9f, 1)]
  public float damping = 0.99f;
  #endregion

  #region Simulation
  [Header("Simulation")]
  public uint iterationSteps = 2;
  public float deltaTimeStep = 0.001f;
  public bool startSimulationOnPlay = true;
  #endregion

  #region Editor Stuffs
  [HideInInspector]
  public bool hide = false;
  [HideInInspector]
  public bool simulate = false;
  #endregion

  void Start()
  {
    if (startSimulationOnPlay) simulate = true;
  }

  void Update()
  {
  }

  public void SimulateOneTimeStep(float dt)
  {
  }
}
