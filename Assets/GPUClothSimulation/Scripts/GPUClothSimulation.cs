using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DataStruct;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GPUClothSimulation : MonoBehaviour
{
  #region Initializattion;
  [ConditionalHideAttribute("hide"), SerializeField]
  public string path;
  [ConditionalHideAttribute("hide"), SerializeField]
  public int totalVerts;
  [ConditionalHideAttribute("hide"), SerializeField]
  public int totalEdges;
  [ConditionalHideAttribute("hide"), SerializeField]
  public int totalTriangles;
  [ConditionalHideAttribute("hide"), SerializeField]
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
  [SerializeField]
  public Material frontMaterial;
  [SerializeField]
  public Material backMaterial;
  #endregion

  #region Cloth Simulation Parameters
  [SerializeField]
  public ComputeShader clothSolver;
  [SerializeField]
  public Vector3 gravity = new Vector3(0, -9.81f, 0);
  [Range(0, 1), SerializeField]
  public float compressionStiffness = 1;
  [Range(0, 1), SerializeField]
  public float stretchStiffness = 1;
  [Range(0, 1), SerializeField]
  public float bendingStiffness = 0.1f;
  public float thickness = 0.02f;
  [Range(0.9f, 1), SerializeField]
  public float damping = 0.99f;
  #endregion

  #region Spatial Hashing
  [SerializeField]
  public int gridSize = 1;
  [ConditionalHideAttribute("hide"), SerializeField]
  public float invGridSize;
  [SerializeField]
  public int tableSize = 2000;
  #endregion

  #region Simulation
  [SerializeField]
  public uint iterationSteps = 2;
  [SerializeField]
  public float deltaTimeStep = 0.01f;
  [SerializeField]
  public bool startSimulationOnPlay = true;
  #endregion

  #region Editor Stuffs
  [HideInInspector]
  public bool hide = false;
  [HideInInspector]
  public bool simulate = false;
  float timePassed = 0;
  [HideInInspector]
  public bool showInitialization, showMaterials, showClothParameters, showDefault;
  [HideInInspector]
  public bool showSpatialHashing, showSimulationSettings;
  #endregion

  #region Compute Buffers
  ComputeBuffer pos, predictedPos, velocity, mass, invMass, edge, restLength, neighborTriangle, restAngle, tri, deltaPosUint, deltaCount;
  #endregion

  #region Kernels
  int SolveExternalForce, SolveDistanceConstraint, SolveDihedralConstraint, AverageConstraintDeltas;
  #endregion

  void Start()
  {
    if (startSimulationOnPlay) simulate = true;
    meshData.particles[264].invMass = 0;
    meshData.particles[0].invMass = 0;

    InitKernels();
    InitBuffers();
    SetGPUConstants();
    SetGPUParameters();
    SetGPUBuffers();
  }

  void Update()
  {
    timePassed += Time.deltaTime;
    if (timePassed >= deltaTimeStep) timePassed = 0.0f;
    if (simulate && timePassed == 0.0f)
    {
      SimulateOneTimeStep();
      SetGPUBuffers();
    }
  }

  void OnDestroy()
  {
    pos.Release();
    predictedPos.Dispose();
    velocity.Dispose();
    mass.Dispose();
    invMass.Dispose();
    edge.Dispose();
    restLength.Dispose();
    neighborTriangle.Dispose();
    restAngle.Dispose();
    tri.Dispose();
    deltaPosUint.Dispose();
    deltaCount.Dispose();
  }

  public void SimulateOneTimeStep()
  {
    #region Apply External Force
    clothSolver.Dispatch(SolveExternalForce, totalVerts, 1, 1);
    #endregion

    #region Collision Constraints
    #endregion

    #region Project Constraints
    for (int iter=0; iter < iterationSteps; iter++)
    {
      #region Distance Constraint
      clothSolver.Dispatch(SolveDistanceConstraint, totalEdges, 1, 1);
      #endregion

      #region Dihedral Constraint
      clothSolver.Dispatch(SolveDihedralConstraint, totalNeighborTriangles, 1, 1);
      #endregion
    }
    #endregion

    #region Apply Changes
    clothSolver.Dispatch(AverageConstraintDeltas, totalVerts, 1, 1);
    #endregion
  }

  // TODO: update mesh data directly from compute buffers
  // public void UpdateDataToMesh(float dt)
  // {
  //   List<Vector3> meshVerts = new List<Vector3>();
  //   for (int i=0; i < totalVerts; i++)
  //   {
  //     meshVerts.Add(meshData.particles[i].predictedPos);
  //     meshData.particles[i].velocity = (meshData.particles[i].predictedPos - meshData.particles[i].pos) / dt;
  //     meshData.particles[i].pos = meshData.particles[i].predictedPos;
  //   }

  //   mesh.SetVertices(meshVerts);
  //   mesh.RecalculateNormals();
  //   childMesh.SetVertices(meshVerts);
  //   childMesh.RecalculateNormals();
  // }

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

    // Set GPU Buffers init data
    Vector3[] _pos = new Vector3[totalVerts], _predictedPos = new Vector3[totalVerts], _velocity = new Vector3[totalVerts];
    float[] _mass = new float[totalVerts], _invMass = new float[totalVerts];
    int[] _deltaCount = new int[totalVerts];
    uint3[] _deltaPosUint = new uint3[totalVerts];

    int[] _edge = new int[totalEdges * 2], _neighborTriangle = new int[totalNeighborTriangles * 4];
    float[] _restLength = new float[totalEdges], _restAngle = new float[totalNeighborTriangles];
    int[] _tri = new int[totalTriangles * 3];

    for (int i=0; i < totalVerts; i++)
    {
      _pos[i] = meshData.particles[i].pos;
      _predictedPos[i] = meshData.particles[i].predictedPos;
      _mass[i] = meshData.particles[i].mass;
      _invMass[i] = meshData.particles[i].invMass;
      _deltaCount[i] = 0;
      _deltaPosUint[i] = new uint3();
      _deltaPosUint[i].u1 = _deltaPosUint[i].u2 = _deltaPosUint[i].u3 = 0;
    }

    for (int i=0; i < totalEdges; i++)
    {
      _edge[i*2] = meshData.edges[i].p0;
      _edge[i*2 + 1] = meshData.edges[i].p1;
      _restLength[i] = meshData.edges[i].restLength;
    }

    for (int i=0; i < totalNeighborTriangles; i++)
    {
      _neighborTriangle[i*4] = meshData.neighborTriangles[i].p0;
      _neighborTriangle[i*4 + 1] = meshData.neighborTriangles[i].p1;
      _neighborTriangle[i*4 + 2] = meshData.neighborTriangles[i].p2;
      _neighborTriangle[i*4 + 3] = meshData.neighborTriangles[i].p3;
      _restAngle[i] = meshData.neighborTriangles[i].restAngle;
    }

    for (int i=0; i < totalTriangles; i++)
    {
      _tri[i*3] = meshData.triangles[i].p0;
      _tri[i*3 + 1] = meshData.triangles[i].p1;
      _tri[i*3 + 2] = meshData.triangles[i].p2;
    }

    pos.SetData(_pos);
    predictedPos.SetData(_predictedPos);
    velocity.SetData(_velocity);
    mass.SetData(_mass);
    invMass.SetData(_invMass);
    deltaCount.SetData(_deltaCount);
    deltaPosUint.SetData(_deltaPosUint);
    edge.SetData(_edge);
    restLength.SetData(_restLength);
    neighborTriangle.SetData(_neighborTriangle);
    restAngle.SetData(_restAngle);
    tri.SetData(_tri);
  }
  #endregion
}
