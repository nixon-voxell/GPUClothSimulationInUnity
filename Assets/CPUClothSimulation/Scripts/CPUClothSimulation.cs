using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DataStruct;
using Utilities;
using SpatialHashing;

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
    for (int i=0; i < totalVerts; i++)
    {
      Vector3 force = Vector3.zero;
      Vector3 corr;
      // add gravity acceleration
      force += gravity / meshData.particles[i].invMass;
      
      if (PBD.ExternalForce(
        dt,
        meshData.particles[i].predictedPos,
        meshData.particles[i].velocity,
        meshData.particles[i].invMass,
        force,
        damping,
        out corr)) meshData.particles[i].predictedPos += corr;
    }
    #endregion

    #region Collision Constraints
    // SH sh = new SH(gridSize, invGridSize, tableSize);
    // SPHash[] spHash = new SPHash[tableSize];

    // for (int v=0; v < totalVerts; v++)
    // {
    //   int hash = Mathf.Abs(sh.Hash(meshData.particles[v].predictedPos));
    //   if (spHash[hash].indices == null) spHash[hash].indices = new List<int>();
    //   spHash[hash].indices.Add(meshData.particles[v].idx);
    // }

    // for (int t=0; t < totalTriangles; t++)
    // {
    //   Triangle tri = meshData.triangles[t];
    //   Vector3 p0 = meshData.particles[tri.p0].predictedPos;
    //   Vector3 p1 = meshData.particles[tri.p1].predictedPos;
    //   Vector3 p2 = meshData.particles[tri.p2].predictedPos;

    //   float w0 = meshData.particles[tri.p0].invMass;
    //   float w1 = meshData.particles[tri.p1].invMass;
    //   float w2 = meshData.particles[tri.p2].invMass;

    //   List<int> hashes = sh.TriangleBoundingBoxHashes(p0, p1, p2);

    //   for (int h=0; h < hashes.Count; h++)
    //   {
    //     if (spHash[h].indices != null)
    //     {
    //       for (int sph=0; sph < spHash[h].indices.Count ; sph++)
    //       {
    //         int idx = spHash[h].indices[sph];
    //         if (
    //           idx != meshData.particles[tri.p0].idx &&
    //           idx != meshData.particles[tri.p1].idx &&
    //           idx != meshData.particles[tri.p2].idx)
    //         {
    //           Vector3 p = meshData.particles[idx].predictedPos;
    //           float w = meshData.particles[idx].invMass;

    //           Vector3 corr, corr0, corr1, corr2;
    //           if (PBD.TrianglePointDistanceConstraint(
    //             p, w,
    //             p0, w0,
    //             p1, w1,
    //             p2, w2,
    //             thickness, 1f, 0.0f,
    //             out corr, out corr0, out corr1, out corr2))
    //           {
    //             meshData.particles[idx].predictedPos += corr;
    //             meshData.particles[tri.p0].predictedPos += corr0;
    //             meshData.particles[tri.p1].predictedPos += corr1;
    //             meshData.particles[tri.p2].predictedPos += corr2;
    //           }
    //         }
    //       }
    //     }
    //   }
    // }
    #endregion

    #region Project Constraints
    for (int iter=0; iter < iterationSteps; iter++)
    {
      #region Distance Constraint
      for (int e=0; e < totalEdges; e++)
      {
        Vector3 corr0, corr1;
        Edge edge = meshData.edges[e];
        Vector3 p0 = meshData.particles[edge.p0].predictedPos;
        Vector3 p1 = meshData.particles[edge.p1].predictedPos;

        float w0 = meshData.particles[edge.p0].invMass;
        float w1 = meshData.particles[edge.p1].invMass;

        if (PBD.DistanceConstraint(
          p0, w0,
          p1, w1,
          edge.restLength,
          stretchStiffness,
          compressionStiffness,
          out corr0, out corr1))
        {
          meshData.particles[edge.p0].predictedPos += corr0;
          meshData.particles[edge.p1].predictedPos += corr1;
        }
      }
      #endregion

      #region Dihedral Constraint
      for (int n=0; n < totalNeighborTriangles; n++)
      {
        Vector3 corr0, corr1, corr2, corr3;
        NeighborTriangles neighbor = meshData.neighborTriangles[n];
        Vector3 p0 = meshData.particles[neighbor.p0].predictedPos;
        Vector3 p1 = meshData.particles[neighbor.p1].predictedPos;
        Vector3 p2 = meshData.particles[neighbor.p2].predictedPos;
        Vector3 p3 = meshData.particles[neighbor.p3].predictedPos;

        float w0 = meshData.particles[neighbor.p0].invMass;
        float w1 = meshData.particles[neighbor.p1].invMass;
        float w2 = meshData.particles[neighbor.p2].invMass;
        float w3 = meshData.particles[neighbor.p3].invMass;

        if (PBD.DihedralConstraint(
          p0, w0,
          p1, w1,
          p2, w2,
          p3, w3,
          neighbor.restAngle,
          bendingStiffness,
          out corr0, out corr1, out corr2, out corr3))
        {
          meshData.particles[neighbor.p0].predictedPos += corr0;
          meshData.particles[neighbor.p1].predictedPos += corr1;
          meshData.particles[neighbor.p2].predictedPos += corr2;
          meshData.particles[neighbor.p3].predictedPos += corr3;
        }
      }
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
