using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DataStruct;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GPUClothSimulation : MonoBehaviour
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
  public MeshData meshData;
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
  public float stretchStiffness = 1;
  [Range(0, 1)]
  public float bendingStiffness = 0.1f;
  public float thickness = 0.02f;
  [Range(0.9f, 1)]
  public float damping = 0.99f;
  #endregion

  #region Spatial Hashing
  [Header("Spatial Hashing")]
  public int gridSize = 1;
  [ConditionalHideAttribute("hide")]
  public float invGridSize;
  public int tableSize = 2000;
  #endregion

  #region Simulation
  [Header("Simulation")]
  public uint iterationSteps = 2;
  public float deltaTimeStep = 0.01f;
  public bool startSimulationOnPlay = true;
  #endregion

  #region Editor Stuffs
  [HideInInspector]
  public bool hide = false;
  [HideInInspector]
  public bool simulate = false;
  float timePassed = 0;
  #endregion

  void Start()
  {
    if (startSimulationOnPlay) simulate = true;
    meshData.particles[264].invMass = 0;
    meshData.particles[0].invMass = 0;
  }

  void Update()
  {
    timePassed += Time.deltaTime;
    if (timePassed >= deltaTimeStep) timePassed = 0.0f;
    if (simulate && timePassed == 0.0f)
    {
      SimulateOneTimeStep(deltaTimeStep);
      UpdateDataToMesh(deltaTimeStep);
    }
  }

  public void SimulateOneTimeStep(float dt)
  {
    #region Apply External Force
    #endregion

    #region Collision Constraints
    #endregion

    #region Project Constraints
    for (int iter=0; iter < iterationSteps; iter++)
    {
      #region Distance Constraint
      #endregion

      #region Dihedral Constraint
      #endregion
    }
    #endregion
  }

  public void UpdateDataToMesh(float dt)
  {
    List<Vector3> meshVerts = new List<Vector3>();
    for (int i=0; i < totalVerts; i++)
    {
      meshVerts.Add(meshData.particles[i].predictedPos);
      meshData.particles[i].velocity = (meshData.particles[i].predictedPos - meshData.particles[i].pos) / dt;
      meshData.particles[i].pos = meshData.particles[i].predictedPos;
    }

    mesh.SetVertices(meshVerts);
    mesh.RecalculateNormals();
    childMesh.SetVertices(meshVerts);
    childMesh.RecalculateNormals();
  }
}
