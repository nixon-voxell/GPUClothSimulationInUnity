using System.Linq;
using UnityEngine;
using Helper;

[RequireComponent(typeof(SkinnedMeshRenderer))]
public class GPUClothSimulationLegacy : MonoBehaviour
{
  bool scriptEnabled = false;
  // TODO: use triangle data instead of vertex data
  #region Mesh Data
  [Header("Mesh Data")]
  [ConditionalHideAttribute("hide")]
  [Tooltip("Make sure that your mesh is Read/Write enabled!")]
  public Mesh mesh;
  [ConditionalHideAttribute("hide")]
  public int totalVerts;
  [ConditionalHideAttribute("hide")]
  public int totalTrianglePoints;
  [ConditionalHideAttribute("hide")]
  public int totalTriangles;
  [ConditionalHideAttribute("hide")]
  public int totalTriangleNeighbors;

  public Material frontMat;
  public Material backMat;

  // [HideInInspector]
  // public Vector3[] verts;
  // [HideInInspector]
  // public int[] triangles;

  [HideInInspector]
  public VertexData vertexData;

  GameObject backSide;
  Mesh backMesh;
  #endregion

  #region Cloth Simulation Parameters
  [Header("Cloth Simulation Parameters")]
  [SerializeField] public ComputeShader PBDClothSolver;
  [Tooltip("Merge any 2 vertices within this distance.")]
  public float weldDistance = 0.0001f;
  [Tooltip("Keep this as low as possible to increase performance. " +
  "Increase this to increase quality of cloth (ex: more detailed folds and less clipping chance).")]
  public uint iterationSteps = 10;
  [Tooltip("This should be higher than iteration steps.")]
  public uint collisionSteps = 5;
  [Tooltip("Mass of each particle representing each vertices.")]
  public float particleMass = 0.01f;
  [ConditionalHideAttribute("hide")]
  [Tooltip("1/particle mass")]
  public float particleInvertMass;
  #endregion

  #region External Force
  [Header("External Force")]
  [Tooltip("Constant acceleration on all particles.")]
  public Vector3 gravity = new Vector3(0, -9.81f, 0);
  [Range(0, 1)]
  public float damping = 0.1f;
  #endregion

  #region Constraints
  [Header("Constraints")]
  // distance constraint
  [Range(0.1f, 1)]
  public float stiffness = 0.9f;

  // bending constraint
  [Range(0.01f, 1)]
  public float bendingStiffness = 0.05f;
  [Range(-1, 1)]
  public float bendiness = 1f;
  [ConditionalHideAttribute("hide")]
  [Tooltip("Acos(bendiness)")]
  public float restAngle = 0f;

  // self collision
  public float clothThickness = 0.02f;
  #endregion

  #region Wind Parameters
  [Header("Wind Parameters")]
  [Tooltip("Direction of where the wind will blow, please keep this between 0 to 1.")]
  public Vector3 windVelocity = new Vector3(1, -0.5f, 1);
  [Tooltip("Controls how fast we scroll through the gradient noise.")]
  public float windSpeed = 1;
  [Tooltip("Scale of gradient noise.")]
  public float turbulence = 0.1f;
  [Tooltip("Coefficient of drag force acting on the cloth.")]
  public float drag = 0.01f;
  [Tooltip("Coefficient of lift force acting on the cloth.")]
  public float lift = 0.1f;
  #endregion

  #region Collision Parameters
  [Header("Collision Parameters")]
  public SkinnedMesh skinnedMeshCollider;
  public float meshThickness = 0.03f;
  public SphereCollider[] SphereColliders;
  public BoxCollider[] BoxColliders;
  public CapsuleCollider[] CapsuleColliders;
  #endregion

  #region Save Path
  [Header("Save Path")]
  public string saveFolder = "ClothData";
  public string filename = "Cloak";
  #endregion

  #region Editor Stuffs
  [HideInInspector]
  public bool hide = false;
  #endregion

  #region ComputeShader Variables
  Vector3[] pos;
  Vector3[] vel;
  Vector3[] deltaP;
  UInt3Struct[] deltaPuint;
  int[] deltaC;
  // Triangleids[] vertexData.sortedTriangles;
  // NeighborTriangleids[] neighborTri;
  // Vector3[] skinnedPos;
  float[] bw;
  float time = 0;

  ComputeBuffer positions;
  ComputeBuffer projectedPositions;
  ComputeBuffer velocities;

  ComputeBuffer deltaPositions;
  ComputeBuffer deltaPositionsUInt;
  ComputeBuffer deltaCount;
  
  ComputeBuffer boneWeight;
  ComputeBuffer sortedTriangles;
  ComputeBuffer neighborTriangles;
  ComputeBuffer skinnedMeshPositions;
  ComputeBuffer projectedSkinnedMeshPositions;
  ComputeBuffer skinnedMeshNormals;

  // forces
  int ExternalForce;
  int Wind;
  int DampVelocities;
  int ApplyExplicitEuler;

  // collisions
  int MeshCollision;
  int SphereCollision;
  int BoxCollision;
  int CapsuleCollision;

  // constraints
  int DistanceConstraint;
  int BendConstraint;
  int AverageConstraintDeltas;
  int SelfCollision;
  int UpdatePositions;
  int UpdateSkinnedMeshPositions;
  #endregion

  #region Unity Callbacks
  void Start()
  {
    scriptEnabled = true;
    skinnedMeshCollider.Init();
    skinnedMeshCollider.BakeMeshData();

    vertexData = _Vertex.LoadData(filename, saveFolder);

    _Mesh.AutoWeld(mesh, weldDistance);
    _Vertex.InitRawMesh(mesh,
    out totalVerts,
    out totalTrianglePoints);
    mesh.MarkDynamic();

    totalTriangles = vertexData.sortedTriangles.Length;
    totalTriangleNeighbors = vertexData.neighborTriangles.Length;

    // Compute Shader Initialization
    InitKernels();
    InitBuffers();
    InitVariables();

    // init SkinnedMeshRenderer for front face
    this.GetComponent<SkinnedMeshRenderer>().material = frontMat;
    this.GetComponent<SkinnedMeshRenderer>().receiveShadows = false;
    this.GetComponent<SkinnedMeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

    // init SkinnedMeshRenderer for back face
    backSide = _Mesh.CreateBackSide(this.gameObject);
    backMesh = backSide.GetComponent<SkinnedMeshRenderer>().sharedMesh;
    backSide.GetComponent<SkinnedMeshRenderer>().material = backMat;
    backSide.GetComponent<SkinnedMeshRenderer>().receiveShadows = false;
    backSide.GetComponent<SkinnedMeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
  }

  void FixedUpdate()
  {
    UpdateVariables();
    DispatchKernels();
    UpdatePositionsToMesh();
    // PBDOnCPU();
  }

  void OnDestroy()
  {
    if (scriptEnabled) ReloadMeshData();
    ReleaseBuffers();
  }
  #endregion

  #region ComputeShader Helpers
  void InitKernels()
  {
    // forces
    ExternalForce = PBDClothSolver.FindKernel("ExternalForce");
    Wind = PBDClothSolver.FindKernel("Wind");
    DampVelocities = PBDClothSolver.FindKernel("DampVelocities");
    ApplyExplicitEuler = PBDClothSolver.FindKernel("ApplyExplicitEuler");

    // collisions
    MeshCollision = PBDClothSolver.FindKernel("MeshCollision");
    SphereCollision = PBDClothSolver.FindKernel("SphereCollision");
    BoxCollision = PBDClothSolver.FindKernel("BoxCollision");
    CapsuleCollision = PBDClothSolver.FindKernel("CapsuleCollision");

    // constraints
    DistanceConstraint = PBDClothSolver.FindKernel("DistanceConstraint");
    AverageConstraintDeltas = PBDClothSolver.FindKernel("AverageConstraintDeltas");
    BendConstraint = PBDClothSolver.FindKernel("BendConstraint");
    SelfCollision = PBDClothSolver.FindKernel("SelfCollision");
    UpdatePositions = PBDClothSolver.FindKernel("UpdatePositions");
    UpdateSkinnedMeshPositions = PBDClothSolver.FindKernel("UpdateSkinnedMeshPositions");
  }

  void InitBuffers()
  {
    // strides
    int strideFloat3 = System.Runtime.InteropServices.Marshal.SizeOf(typeof(float)) * 3;
    int strideFloat = System.Runtime.InteropServices.Marshal.SizeOf(typeof(float));
    int strideInt = System.Runtime.InteropServices.Marshal.SizeOf(typeof(int));
    int strideUint3 = System.Runtime.InteropServices.Marshal.SizeOf(typeof(uint)) * 3;
    int strideTriangleids = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Triangleids));
    int strideNeighborTriangleids = System.Runtime.InteropServices.Marshal.SizeOf(typeof(NeighborTriangleids));

    positions = new ComputeBuffer(totalVerts, strideFloat3);
    projectedPositions = new ComputeBuffer(totalVerts, strideFloat3);
    velocities = new ComputeBuffer(totalVerts, strideFloat3);

    deltaPositions = new ComputeBuffer(totalVerts, strideFloat3);
    deltaPositionsUInt = new ComputeBuffer(totalVerts, strideUint3);
    deltaCount = new ComputeBuffer(totalVerts, strideInt);

    boneWeight = new ComputeBuffer(totalVerts, strideFloat);
    sortedTriangles = new ComputeBuffer(totalTriangles, strideTriangleids);
    neighborTriangles = new ComputeBuffer(totalTriangleNeighbors, strideNeighborTriangleids);
    skinnedMeshPositions = new ComputeBuffer(skinnedMeshCollider.vertexCount, strideFloat3);
    projectedSkinnedMeshPositions = new ComputeBuffer(skinnedMeshCollider.vertexCount, strideFloat3);
    skinnedMeshNormals = new ComputeBuffer(skinnedMeshCollider.vertexCount, strideFloat3);

    // forces------------------------------------------
    PBDClothSolver.SetBuffer(ExternalForce, "velocities", velocities);
    PBDClothSolver.SetBuffer(ExternalForce, "boneWeight", boneWeight);

    PBDClothSolver.SetBuffer(Wind, "velocities", velocities);
    PBDClothSolver.SetBuffer(Wind, "projectedPositions", projectedPositions);
    PBDClothSolver.SetBuffer(Wind, "boneWeight", boneWeight);
    PBDClothSolver.SetBuffer(Wind, "sortedTriangles", sortedTriangles);

    PBDClothSolver.SetBuffer(DampVelocities, "velocities", velocities);

    PBDClothSolver.SetBuffer(ApplyExplicitEuler, "positions", positions);
    PBDClothSolver.SetBuffer(ApplyExplicitEuler, "projectedPositions", projectedPositions);
    PBDClothSolver.SetBuffer(ApplyExplicitEuler, "velocities", velocities);
    // forces------------------------------------------

    // constraints-------------------------------------
    PBDClothSolver.SetBuffer(DistanceConstraint, "projectedPositions", projectedPositions);
    PBDClothSolver.SetBuffer(DistanceConstraint, "deltaPos", deltaPositions);
    PBDClothSolver.SetBuffer(DistanceConstraint, "deltaPosAsInt", deltaPositionsUInt);
    PBDClothSolver.SetBuffer(DistanceConstraint, "deltaCount", deltaCount);
    PBDClothSolver.SetBuffer(DistanceConstraint, "boneWeight", boneWeight);
    PBDClothSolver.SetBuffer(DistanceConstraint, "sortedTriangles", sortedTriangles);

    PBDClothSolver.SetBuffer(BendConstraint, "projectedPositions", projectedPositions);
    PBDClothSolver.SetBuffer(BendConstraint, "deltaPos", deltaPositions);
    PBDClothSolver.SetBuffer(BendConstraint, "deltaPosAsInt", deltaPositionsUInt);
    PBDClothSolver.SetBuffer(BendConstraint, "deltaCount", deltaCount);
    PBDClothSolver.SetBuffer(BendConstraint, "boneWeight", boneWeight);
    PBDClothSolver.SetBuffer(BendConstraint, "neighborTriangles", neighborTriangles);

    PBDClothSolver.SetBuffer(AverageConstraintDeltas, "projectedPositions", projectedPositions);
    PBDClothSolver.SetBuffer(AverageConstraintDeltas, "deltaPos", deltaPositions);
    PBDClothSolver.SetBuffer(AverageConstraintDeltas, "deltaPosAsInt", deltaPositionsUInt);
    PBDClothSolver.SetBuffer(AverageConstraintDeltas, "deltaCount", deltaCount);

    PBDClothSolver.SetBuffer(SelfCollision, "projectedPositions", projectedPositions);
    PBDClothSolver.SetBuffer(SelfCollision, "deltaPos", deltaPositions);
    PBDClothSolver.SetBuffer(SelfCollision, "deltaPosAsInt", deltaPositionsUInt);
    PBDClothSolver.SetBuffer(SelfCollision, "deltaCount", deltaCount);
    PBDClothSolver.SetBuffer(SelfCollision, "boneWeight", boneWeight);
    PBDClothSolver.SetBuffer(SelfCollision, "sortedTriangles", sortedTriangles);

    PBDClothSolver.SetBuffer(MeshCollision, "positions", positions);
    PBDClothSolver.SetBuffer(MeshCollision, "velocities", velocities);
    PBDClothSolver.SetBuffer(MeshCollision, "boneWeight", boneWeight);
    PBDClothSolver.SetBuffer(MeshCollision, "skinnedMeshPositions", skinnedMeshPositions);
    PBDClothSolver.SetBuffer(MeshCollision, "projectedSkinnedMeshPositions", projectedSkinnedMeshPositions);
    PBDClothSolver.SetBuffer(MeshCollision, "skinnedMeshNormals", skinnedMeshNormals);
    // constraints-------------------------------------

    // update positions--------------------------------
    PBDClothSolver.SetBuffer(UpdatePositions, "positions", positions);
    PBDClothSolver.SetBuffer(UpdatePositions, "projectedPositions", projectedPositions);
    PBDClothSolver.SetBuffer(UpdatePositions, "velocities", velocities);

    PBDClothSolver.SetBuffer(UpdateSkinnedMeshPositions, "skinnedMeshPositions", skinnedMeshPositions);
    PBDClothSolver.SetBuffer(UpdateSkinnedMeshPositions, "projectedSkinnedMeshPositions", projectedSkinnedMeshPositions);
    // update positions--------------------------------

    // set data to buffers
    pos = new Vector3[totalVerts];
    vel = new Vector3[totalVerts];
    deltaP = new Vector3[totalVerts];
    deltaPuint = new UInt3Struct[totalVerts];
    deltaC = new int[totalVerts];
    bw = new float[totalVerts];

    for (int i=0; i < totalVerts; i++)
    {
      vel[i] = Vector3.zero;
      deltaP[i] = Vector3.zero;
      deltaPuint[i].deltaXInt = 0;
      deltaPuint[i].deltaYInt = 0;
      deltaPuint[i].deltaZInt = 0;
      deltaC[i] = 0;
      // bw[i] = 1;
      if (mesh.boneWeights[i].boneIndex0 != 0)
      {
        bw[i] = 1 - mesh.boneWeights[i].weight0;
      } else
      {
        bw[i] = 1;
      }
    }
    bw[0] = 0;
    // print(bw[0]);
    pos = _Convert.FloatArrayToVector3Array(vertexData.positions);

    positions.SetData(pos);
    projectedPositions.SetData(pos);
    velocities.SetData(vel);
    deltaPositions.SetData(deltaP);
    deltaPositionsUInt.SetData(deltaPuint);
    deltaCount.SetData(deltaC);
    boneWeight.SetData(bw);
    sortedTriangles.SetData(vertexData.sortedTriangles.ToArray());
    neighborTriangles.SetData(vertexData.neighborTriangles.ToArray());
    skinnedMeshPositions.SetData(skinnedMeshCollider.bakedVertices.ToArray());
    projectedSkinnedMeshPositions.SetData(skinnedMeshCollider.bakedVertices.ToArray());
    skinnedMeshNormals.SetData(skinnedMeshCollider.bakedNormals.ToArray());

    // for (int i=0; i < vertexData.sortedTriangles.Length; i++)
    // {
    //   if (vertexData.sortedTriangles[i].A == 0 || vertexData.sortedTriangles[i].B == 0 || vertexData.sortedTriangles[i].C == 0)
    //   {
    //     print(vertexData.sortedTriangles[i].A + " " + vertexData.sortedTriangles[i].B + " " + vertexData.sortedTriangles[i].C);
    //     print(vertexData.sortedTriangles[i].AB + " " + vertexData.sortedTriangles[i].BC + " " + vertexData.sortedTriangles[i].CA);
    //   }
    // }

    print(pos[0]);
  }

  void InitVariables()
  {
    PBDClothSolver.SetFloat("deltaT", 0);
    PBDClothSolver.SetFloat("time", time);
    PBDClothSolver.SetVector("gravity", gravity);
    PBDClothSolver.SetFloat("stiffness", stiffness);
    PBDClothSolver.SetFloat("bendiness", bendiness);
    PBDClothSolver.SetFloat("clothThickness", clothThickness);
    PBDClothSolver.SetFloat("meshThickness", meshThickness);

    PBDClothSolver.SetFloat("particleMass", particleMass);
    PBDClothSolver.SetFloat("particleInvertMass", particleInvertMass);

    PBDClothSolver.SetVector("windVelocity", windVelocity);
    PBDClothSolver.SetFloat("windSpeed", windSpeed);
    PBDClothSolver.SetFloat("turbulence", turbulence);
    PBDClothSolver.SetFloat("drag", drag);
    PBDClothSolver.SetFloat("lift", lift);

    PBDClothSolver.SetInt("totalSimulationVerts", totalVerts);
    PBDClothSolver.SetInt("totalSimulationTriangles", totalTriangles);
    PBDClothSolver.SetInt("totalMeshVerts", skinnedMeshCollider.vertexCount);
  }

  void UpdateVariables()
  {
    time += Time.deltaTime;
    PBDClothSolver.SetFloat("deltaT", 0.01f);
    PBDClothSolver.SetFloat("time", time);
    PBDClothSolver.SetVector("gravity", gravity);
    PBDClothSolver.SetFloat("stiffness", stiffness);
    PBDClothSolver.SetFloat("bendingStiffness", bendingStiffness);
    PBDClothSolver.SetFloat("restAngle", restAngle);
    PBDClothSolver.SetFloat("meshThickness", meshThickness);
    PBDClothSolver.SetFloat("ClothThickness", clothThickness);

    PBDClothSolver.SetVector("windVelocity", windVelocity);
    PBDClothSolver.SetFloat("windSpeed", windSpeed);
    PBDClothSolver.SetFloat("turbulence", turbulence);
    PBDClothSolver.SetFloat("drag", drag);
    PBDClothSolver.SetFloat("lift", lift);
  }

  void DispatchKernels()
  {
    PBDClothSolver.Dispatch(ExternalForce, totalVerts, 1, 1);
    // PBDClothSolver.Dispatch(Wind, totalTriangles, 1, 1);
    PBDClothSolver.Dispatch(MeshCollision, totalVerts, 1, 1);
    PBDClothSolver.Dispatch(DampVelocities, totalVerts, 1, 1);
    PBDClothSolver.Dispatch(ApplyExplicitEuler, totalVerts, 1, 1);

    // for (int i=0; i < collisionSteps; i++)
    // {
      PBDClothSolver.Dispatch(SelfCollision, totalTriangles, 1, 1);
      PBDClothSolver.Dispatch(AverageConstraintDeltas, totalVerts, 1, 1);
    // }

    for (int i=0; i < iterationSteps; i++)
    {
      PBDClothSolver.Dispatch(DistanceConstraint, totalTriangles, 1, 1);
      PBDClothSolver.Dispatch(AverageConstraintDeltas, totalVerts, 1, 1);
      PBDClothSolver.Dispatch(BendConstraint, totalTriangleNeighbors, 1, 1);
      PBDClothSolver.Dispatch(AverageConstraintDeltas, totalVerts, 1, 1);
    }

    PBDClothSolver.Dispatch(UpdatePositions, totalVerts, 1, 1);
    PBDClothSolver.Dispatch(UpdateSkinnedMeshPositions, skinnedMeshCollider.vertexCount, 1, 1);
    skinnedMeshCollider.BakeMeshData();
    projectedSkinnedMeshPositions.SetData(skinnedMeshCollider.bakedVertices.ToArray());
    skinnedMeshNormals.SetData(skinnedMeshCollider.bakedNormals.ToArray());
  }

  void PBDOnCPU()
  {
    // gravity
    for (int i=0; i < totalVerts; i++)
    {
      vel[i] += gravity * particleInvertMass * Time.deltaTime * bw[i];
      pos[i] += vel[i] * Time.deltaTime;
    }
    Vector3 BinaryDistanceConstraint(Vector3 pI, Vector3 pJ, float wI, float wJ, float restd)
    {
      // returns where pI should go
      if (wI == 0) return Vector3.zero;
      else return (wI/(wI + wJ)) * (Vector3.Distance(pI, pJ) - restd) * Vector3.Normalize(pJ-pI) * stiffness;
    }

    for (int _i=0; _i < iterationSteps; _i++)
    {
      for (int i=0; i < totalTriangles; i++)
      {
        int idxA = vertexData.sortedTriangles[i].A;
        int idxB = vertexData.sortedTriangles[i].B;
        int idxC = vertexData.sortedTriangles[i].C;

        Vector3 A = pos[idxA];
        Vector3 B = pos[idxB];
        Vector3 C = pos[idxC];

        float AB = vertexData.sortedTriangles[i].AB;
        float BC = vertexData.sortedTriangles[i].BC;
        float CA = vertexData.sortedTriangles[i].CA;

        float wA = particleInvertMass * bw[idxA];
        float wB = particleInvertMass * bw[idxB];
        float wC = particleInvertMass * bw[idxC];

        Vector3 dA = BinaryDistanceConstraint(A, B, wA, wB, AB);
        dA += BinaryDistanceConstraint(A, C, wA, wC, CA);
        // dA *= 0.5f;

        Vector3 dB = BinaryDistanceConstraint(B, C, wB, wC, BC);
        dB += BinaryDistanceConstraint(B, A, wB, wA, AB);
        // dB *= 0.5f;

        Vector3 dC = BinaryDistanceConstraint(C, A, wC, wA, CA);
        dC += BinaryDistanceConstraint(C, B, wC, wB, BC);
        // dC *= 0.5f;

        A += dA;
        B += dB;
        C += dC;
        
        pos[idxA] = A;
        pos[idxB] = B;
        pos[idxC] = C;
        vel[idxA] += dA/Time.deltaTime/iterationSteps;
        vel[idxB] += dB/Time.deltaTime/iterationSteps;
        vel[idxC] += dC/Time.deltaTime/iterationSteps;
      }
    }
  }

  void UpdatePositionsToMesh()
  {
    positions.GetData(pos);
    // update backMesh
    backMesh.vertices = pos;
    backMesh.normals = _Mesh.ReverseNormals(mesh.normals);

    mesh.vertices = pos;
    // for (int i=0; i < totalVerts; i++)
    // {
    //   if (bw[i] > 0)
    //   {
    //     mesh.vertices[i] = pos[i];
    //   }
    // }
    mesh.RecalculateNormals();

    // positions.SetData(pos);
    // projectedPositions.SetData(pos);
  }

  void ReleaseBuffers()
  {
    if (positions != null) positions.Release();
    if (projectedPositions != null) projectedPositions.Release();
    if (velocities != null) velocities.Release();
    if (deltaPositions != null) deltaPositions.Release();
    if (deltaPositionsUInt != null) deltaPositionsUInt.Release();
    if (deltaCount != null) deltaCount.Release();
    if (sortedTriangles != null) sortedTriangles.Release();
    if (neighborTriangles != null) neighborTriangles.Release();
    if (boneWeight != null) boneWeight.Release();
    if (skinnedMeshPositions != null) skinnedMeshPositions.Release();
    if (projectedSkinnedMeshPositions != null) projectedSkinnedMeshPositions.Release();
    if (skinnedMeshNormals != null) skinnedMeshNormals.Release();
  }
  #endregion

  #region Mesh Data
  public void SortMeshData()
  {
    _Mesh.AutoWeld(mesh, weldDistance);
    vertexData.positions = _Convert.Vector3ArrayToFloatArray(mesh.vertices);
    print(vertexData.positions.Length);
    vertexData = _Vertex.SortTrianglesByGrp(mesh.triangles, totalTrianglePoints, vertexData);
    vertexData = _Vertex.SortTrianglesByNeighbor(vertexData.sortedTriangles, vertexData);

    totalVerts = vertexData.positions.Length;
    totalTriangles = vertexData.sortedTriangles.Length;
    totalTriangleNeighbors = vertexData.neighborTriangles.Length;
  }

  public void ReloadMeshData()
  {
    if (vertexData.positions == null) vertexData = _Vertex.LoadData(filename, saveFolder);

    if (vertexData.positions != null)
    {
      _Vertex.ResetMeshData(vertexData.positions, mesh);
      totalVerts = vertexData.positions.Length;
      totalTriangles = vertexData.sortedTriangles.Length;
    }
  }

  public void SaveMeshData()
  {
    _Vertex.SaveData(filename, saveFolder, vertexData);
  }
  #endregion
}
