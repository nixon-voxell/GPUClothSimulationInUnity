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
  public ComputeShader clothSolver;
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

  #region Compute Buffers
  ComputeBuffer pos;
  ComputeBuffer predictedPos;
  ComputeBuffer velocity;
  ComputeBuffer mass;
  ComputeBuffer invMass;
  ComputeBuffer edge;
  ComputeBuffer restLength;
  ComputeBuffer neighborTriangle;
  ComputeBuffer restAngle;
  ComputeBuffer tri;
  ComputeBuffer deltaPosUint;
  ComputeBuffer deltaCount;
  #endregion

  #region Kernels
  int SolveExternalForce;
  int SolveDistanceConstraint;
  int SolveDihedralConstraint;
  int AverageConstraintDeltas;
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

  #region GPU Kernel & Buffer Initialization
  void InitKernels()
  {
    SolveExternalForce = clothSolver.FindKernel("SolveExternalForce");
    SolveDistanceConstraint = clothSolver.FindKernel("SolveDistanceConstraint");
    SolveDihedralConstraint = clothSolver.FindKernel("SolveDihedralConstraint");
    AverageConstraintDeltas = clothSolver.FindKernel("AverageConstraintDeltas");
  }

  void InitBuffers()
  {
    // strides
    int strideFloat3 = System.Runtime.InteropServices.Marshal.SizeOf(typeof(float)) * 3;
    int strideFloat = System.Runtime.InteropServices.Marshal.SizeOf(typeof(float));
    int strideInt = System.Runtime.InteropServices.Marshal.SizeOf(typeof(int));
    int strideUint3 = System.Runtime.InteropServices.Marshal.SizeOf(typeof(uint)) * 3;

    // compute buffers
    pos = new ComputeBuffer(totalVerts, strideFloat3);
    predictedPos = new ComputeBuffer(totalVerts, strideFloat3);
    velocity = new ComputeBuffer(totalVerts, strideFloat3);
    mass = new ComputeBuffer(totalVerts, strideFloat);
    invMass = new ComputeBuffer(totalVerts, strideFloat);
    edge = new ComputeBuffer(totalEdges * 2, strideInt);
    restLength = new ComputeBuffer(totalEdges, strideFloat);
    neighborTriangle = new ComputeBuffer(totalNeighborTriangles * 4, strideInt);
    restAngle = new ComputeBuffer(totalNeighborTriangles, strideFloat);
    tri = new ComputeBuffer(totalTriangles * 3, strideInt);
    deltaPosUint = new ComputeBuffer(totalVerts, strideUint3);
    deltaCount = new ComputeBuffer(totalVerts, strideInt);
  }
  #endregion

  #region Set GPU Variables
  void SetGPUConstants()
  {
    clothSolver.SetFloat("deltaT", deltaTimeStep);
    clothSolver.SetInt("totalVerts", totalVerts);
    clothSolver.SetInt("totalEdges", totalEdges);
    clothSolver.SetInt("totalTriangles", totalTriangles);
    clothSolver.SetInt("totalNeighborTriangles", totalNeighborTriangles);
  }

  void SetGPUParameters()
  {
    clothSolver.SetFloat("damping", damping);
    clothSolver.SetVector("gravity", gravity);
    clothSolver.SetFloat("stretchStiffness", stretchStiffness);
    clothSolver.SetFloat("compressionStiffness", compressionStiffness);
    clothSolver.SetFloat("bendingStiffness", bendingStiffness);
    clothSolver.SetFloat("thickness", thickness);
  }

  void SetGPUBuffers()
  {
    // SolveExternalForce
    clothSolver.SetBuffer(SolveExternalForce, "predictedPos", predictedPos);
    clothSolver.SetBuffer(SolveExternalForce, "velocity", velocity);
    clothSolver.SetBuffer(SolveExternalForce, "invMass", invMass);
    clothSolver.SetBuffer(SolveExternalForce, "mass", mass);

    // SolveDistanceConstraint
    clothSolver.SetBuffer(SolveDistanceConstraint, "predictedPos", predictedPos);
    clothSolver.SetBuffer(SolveDistanceConstraint, "invMass", invMass);
    clothSolver.SetBuffer(SolveDistanceConstraint, "edge", edge);
    clothSolver.SetBuffer(SolveDistanceConstraint, "restLength", restLength);
    clothSolver.SetBuffer(SolveDistanceConstraint, "deltaPosUint", deltaPosUint);
    clothSolver.SetBuffer(SolveDistanceConstraint, "deltaCount", deltaCount);

    // SolveDihedralConstraint
    clothSolver.SetBuffer(SolveDihedralConstraint, "predictedPos", predictedPos);
    clothSolver.SetBuffer(SolveDihedralConstraint, "invMass", invMass);
    clothSolver.SetBuffer(SolveDihedralConstraint, "neighborTriangle", neighborTriangle);
    clothSolver.SetBuffer(SolveDihedralConstraint, "restAngle", restAngle);
    clothSolver.SetBuffer(SolveDihedralConstraint, "deltaPosUint", deltaPosUint);
    clothSolver.SetBuffer(SolveDihedralConstraint, "deltaCount", deltaCount);

    // AverageConstraintDeltas
    clothSolver.SetBuffer(AverageConstraintDeltas, "predictedPos", predictedPos);
    clothSolver.SetBuffer(AverageConstraintDeltas, "deltaPosUint", deltaPosUint);
    clothSolver.SetBuffer(AverageConstraintDeltas, "deltaCount", deltaCount);
  }
  #endregion
}
