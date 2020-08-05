using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
  #endregion

  #region Cloth Simulation Parameters
  [Header("Cloth Simulation Parameters")]
  [Range(0, 1)]
  public float compressionStiffness = 1;
  [Range(0, 1)]
  public float strechStiffness = 1;
  #endregion

  #region Editor Stuffs
  [HideInInspector]
  public bool hide = false;
  #endregion

  void Start()
  {
    // print(Application.dataPath);
  }

  public void ShowMesh()
  {
    this.GetComponent<SkinnedMeshRenderer>().sharedMesh = mesh;
  }
}
