using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DataStruct;
using Utilities;

[RequireComponent(typeof(SkinnedMeshRenderer))]
public class CPUClothSimulation : MonoBehaviour
{
  #region Initializattion;
  [ConditionalHideAttribute("hide")]
  public string path;
  [ConditionalHideAttribute("hide")]
  public Mesh mesh;
  [ConditionalHideAttribute("hide")]
  public int totalVerts;
  [ConditionalHideAttribute("hide")]
  public int totalEdges;
  [ConditionalHideAttribute("hide")]
  public int totalTriangles;
  [ConditionalHideAttribute("hide")]
  public int totalNeighborTriangles;

  [HideInInspector]
  public MeshData meshData;
  [HideInInspector]
  public MeshData originMeshData;
  #endregion

  #region Cloth Simulation Parameters
  [Header("Cloth Simulation Parameters")]
  public Vector3 gravity = new Vector3(0, -9.81f, 0);
  [Range(0, 1)]
  public float compressionStiffness = 1;
  [Range(0, 1)]
  public float strechStiffness = 1;
  #endregion

  #region Simulation
  [Header("Simulation")]
  public float deltaTimeStep = 0.001f;
  #endregion

  #region Editor Stuffs
  [HideInInspector]
  public bool hide = false;
  #endregion

  void Start()
  {
  }

  public void ShowMesh()
  {
    this.GetComponent<SkinnedMeshRenderer>().sharedMesh = mesh;
  }

  public void ExternalForce(float dt)
  {
    // we calculate the force based gravity first
    Vector3 force = gravity;
    for (int i=0; i < meshData.particles.Length; i++)
    {
      meshData.particles[i].velocity = _Math.AddFloatArray(meshData.particles[i].velocity, _Convert.Vector3ToFloat(dt * meshData.particles[i].invMass * force));
    }
  }

  public void SimulateOneTimeStep(float dt)
  {
    ExternalForce(dt);
  }
}
