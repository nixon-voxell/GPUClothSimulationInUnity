using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Helper;

[RequireComponent(typeof(SkinnedMeshRenderer))]
public class ClothSimulation : MonoBehaviour
{
  // TODO: use triangle data instead of vertex data
  #region Mesh Data
  [Header("Mesh Data")]
  [ConditionalHideAttribute("hide")]
  public Mesh mesh;
  [ConditionalHideAttribute("hide")]
  public int totalVerts;
  [ConditionalHideAttribute("hide")]
  public int totalTrianglePoints;
  [ConditionalHideAttribute("hide")]
  public int totalSimulationVerts;
  [ConditionalHideAttribute("hide")]
  public int totalSimulationTriangles;

  [HideInInspector]
  public Vector3[] verts;
  [HideInInspector]
  public int[] triangles;

  [HideInInspector]
  public VertexData vertexData;

  GameObject backSide;
  Mesh backMesh;
  #endregion

  #region Cloth Simulation Parameters
  [Header("Cloth Simulation Parameters")]
  public ComputeShader PBDClothSolver;
  [Tooltip("Keep this as low as possible to increase performance. " +
  "Increase this to increase quality of cloth (ex: more detailed folds and less clipping chance).")]
  public uint iterationSteps = 2;
  [Tooltip("This should be higher than iteration steps.")]
  public uint subSteps = 10;
  [Tooltip("Constant acceleration on all particles.")]
  public Vector3 gravity = new Vector3(0, -9.81f, 0);
  [Range(0, 1)]
  public float damping = 0.1f;
  [Range(0.1f, 1)]
  public float stiffness = 0.9f;
  [Range(0, 1)]
  public float bendiness = 0.85f;
  [Range(0, 1)]
  public float bendingStiffness = 0.9f;
  // public float restAngle = ;
  public float collisionRadius = 0.02f;

  [Tooltip("Mass of each particle representing each vertices.")]
  public float particleMass = 0.01f;
  [ConditionalHideAttribute("hide")]
  [Tooltip("1/particle mass")]
  public float particleInvertMass;
  #endregion

  #region Wind Parameters
  [Header("Wind Parameters")]
  [Tooltip("Direction of where the wind will blow, please keep this between 0 to 1.")]
  public Vector3 windDirection = new Vector3(1, -0.5f, 1);
  [Tooltip("A multiplier to the gradient noise.")]
  public float windStrength = 1;
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
  // mesh collision
  public SkinnedMesh skinnedMeshCollider;
  public float meshCollisionRadius = 0.018f;
  public float meshDampRadius = 0.022f;
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
  Triangleids[] tri;
  // Vector3[] skinnedPos;
  float[] bw;
  float time = 0;

  ComputeBuffer positions;
  ComputeBuffer projectedPositions;
  ComputeBuffer velocities;

  ComputeBuffer deltaPositions;
  ComputeBuffer deltaPositionsUInt;
  ComputeBuffer deltaCounter;
  
  ComputeBuffer boneWeight;
  ComputeBuffer sortedTriangles;
  ComputeBuffer skinnedMeshPositions;
  ComputeBuffer skinnedMeshNormals;

  // forces
  int ExternalForce;
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
  #endregion

  #region Unity Callbacks
  void Start()
  {
    skinnedMeshCollider.Init();
    skinnedMeshCollider.BakeMeshData();

    print(skinnedMeshCollider.vertexCount);

    vertexData = _Vertex.LoadData(filename, saveFolder);
    _Vertex.InitRawMesh(mesh,
    out verts,
    out totalVerts,
    out triangles,
    out totalTrianglePoints);
    mesh.MarkDynamic();

    // Compute Shader Initialization
    InitKernels();
    InitBuffers();
    InitVariables();
    backSide = _Mesh.CreateBackSide(this.gameObject);
    backMesh = backSide.GetComponent<SkinnedMeshRenderer>().sharedMesh;
  }

  void FixedUpdate()
  {
    UpdateVariables();
    DispatchKernels();
    // PBDOnCPU();
    UpdatePositionToMesh();
  }

  void OnDestroy()
  {
    ReleaseBuffers();
    ReloadMeshData();
    mesh.RecalculateNormals();
  }
  #endregion

  #region ComputeShader Helpers
  void InitKernels()
  {
    // forces
    ExternalForce = PBDClothSolver.FindKernel("ExternalForce");
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
  }

  void InitBuffers()
  {
    // strides
    int strideFloat3 = System.Runtime.InteropServices.Marshal.SizeOf(typeof(float)) * 3;
    int strideFloat = System.Runtime.InteropServices.Marshal.SizeOf(typeof(float));
    int strideInt = System.Runtime.InteropServices.Marshal.SizeOf(typeof(int));
    int strideUint3 = System.Runtime.InteropServices.Marshal.SizeOf(typeof(uint)) * 3;
    int strideTriangleids = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Triangleids));

    positions = new ComputeBuffer(totalSimulationVerts, strideFloat3);
    projectedPositions = new ComputeBuffer(totalSimulationVerts, strideFloat3);
    velocities = new ComputeBuffer(totalSimulationVerts, strideFloat3);

    deltaPositions = new ComputeBuffer(totalSimulationVerts, strideFloat3);
    deltaPositionsUInt = new ComputeBuffer(totalSimulationVerts, strideUint3);
    deltaCounter = new ComputeBuffer(totalSimulationVerts, strideInt);

    boneWeight = new ComputeBuffer(totalSimulationVerts, strideFloat);
    sortedTriangles = new ComputeBuffer(totalSimulationTriangles, strideTriangleids);
    skinnedMeshPositions = new ComputeBuffer(skinnedMeshCollider.vertexCount, strideFloat3);
    skinnedMeshNormals = new ComputeBuffer(skinnedMeshCollider.vertexCount, strideFloat3);

    // forces------------------------------------------
    PBDClothSolver.SetBuffer(ExternalForce, "velocities", velocities);
    PBDClothSolver.SetBuffer(ExternalForce, "boneWeight", boneWeight);

    PBDClothSolver.SetBuffer(DampVelocities, "velocities", velocities);

    PBDClothSolver.SetBuffer(ApplyExplicitEuler, "positions", positions);
    PBDClothSolver.SetBuffer(ApplyExplicitEuler, "projectedPositions", projectedPositions);
    PBDClothSolver.SetBuffer(ApplyExplicitEuler, "velocities", velocities);
    // forces------------------------------------------

    // constraints-------------------------------------
    PBDClothSolver.SetBuffer(DistanceConstraint, "projectedPositions", projectedPositions);
    PBDClothSolver.SetBuffer(DistanceConstraint, "deltaPos", deltaPositions);
    PBDClothSolver.SetBuffer(DistanceConstraint, "deltaPosAsInt", deltaPositionsUInt);
    PBDClothSolver.SetBuffer(DistanceConstraint, "deltaCount", deltaCounter);
    PBDClothSolver.SetBuffer(DistanceConstraint, "boneWeight", boneWeight);
    PBDClothSolver.SetBuffer(DistanceConstraint, "sortedTriangles", sortedTriangles);

    PBDClothSolver.SetBuffer(AverageConstraintDeltas, "projectedPositions", projectedPositions);
    PBDClothSolver.SetBuffer(AverageConstraintDeltas, "deltaPos", deltaPositions);
    PBDClothSolver.SetBuffer(AverageConstraintDeltas, "deltaPosAsInt", deltaPositionsUInt);
    PBDClothSolver.SetBuffer(AverageConstraintDeltas, "deltaCount", deltaCounter);

    PBDClothSolver.SetBuffer(SelfCollision, "projectedPositions", projectedPositions);
    PBDClothSolver.SetBuffer(SelfCollision, "deltaPos", deltaPositions);
    PBDClothSolver.SetBuffer(SelfCollision, "deltaPosAsInt", deltaPositionsUInt);
    PBDClothSolver.SetBuffer(SelfCollision, "deltaCount", deltaCounter);
    PBDClothSolver.SetBuffer(SelfCollision, "boneWeight", boneWeight);

    PBDClothSolver.SetBuffer(MeshCollision, "positions", positions);
    PBDClothSolver.SetBuffer(MeshCollision, "velocities", velocities);
    PBDClothSolver.SetBuffer(MeshCollision, "deltaPos", deltaPositions);
    PBDClothSolver.SetBuffer(MeshCollision, "deltaPosAsInt", deltaPositionsUInt);
    PBDClothSolver.SetBuffer(MeshCollision, "deltaCount", deltaCounter);
    PBDClothSolver.SetBuffer(MeshCollision, "boneWeight", boneWeight);
    PBDClothSolver.SetBuffer(MeshCollision, "skinnedMeshPositions", skinnedMeshPositions);
    PBDClothSolver.SetBuffer(MeshCollision, "skinnedMeshNormals", skinnedMeshNormals);
    // constraints-------------------------------------

    // update positions--------------------------------
    PBDClothSolver.SetBuffer(UpdatePositions, "positions", positions);
    PBDClothSolver.SetBuffer(UpdatePositions, "projectedPositions", projectedPositions);
    PBDClothSolver.SetBuffer(UpdatePositions, "velocities", velocities);
    // update positions--------------------------------

    // set data to buffers
    pos = new Vector3[totalSimulationVerts];
    vel = new Vector3[totalSimulationVerts];
    deltaP = new Vector3[totalSimulationVerts];
    deltaPuint = new UInt3Struct[totalSimulationVerts];
    deltaC = new int[totalSimulationVerts];
    tri = new Triangleids[totalSimulationTriangles];
    bw = new float[totalSimulationVerts];

    for (int i=0; i < totalSimulationVerts; i++)
    {
      vel[i] = Vector3.zero;
      deltaP[i] = Vector3.zero;
      deltaPuint[i].deltaXInt = 0;
      deltaPuint[i].deltaYInt = 0;
      deltaPuint[i].deltaZInt = 0;
      deltaC[i] = 0;
      // bw[i] = 1;
      if (mesh.boneWeights[vertexData.custom2raw[i][0]].boneIndex0 != 0)
      {
        bw[i] = 1 - mesh.boneWeights[vertexData.custom2raw[i][0]].weight0;
      } else
      {
        bw[i] = 1;
      }
      // print(mesh.boneWeights[vertexData.custom2raw[i][0]].weight1);
      // print(mesh.boneWeights[vertexData.custom2raw[i][0]].weight2);
      // print(mesh.boneWeights[vertexData.custom2raw[i][0]].weight3);
    }
    bw[0] = 0;
    // bw[1000] = 0;
    pos = _Convert.FloatListToVector3List(vertexData.position);
    tri = vertexData.sortedTriangles.ToArray();
    // skinnedPos = skinnedMeshCollider.sharedMesh.vertices;

    positions.SetData(pos);
    projectedPositions.SetData(pos);
    velocities.SetData(vel);
    deltaPositions.SetData(deltaP);
    deltaPositionsUInt.SetData(deltaPuint);
    deltaCounter.SetData(deltaC);
    boneWeight.SetData(bw);
    sortedTriangles.SetData(tri);
    skinnedMeshPositions.SetData(skinnedMeshCollider.bakedVertices.ToArray());
    skinnedMeshNormals.SetData(skinnedMeshCollider.bakedNormals.ToArray());
  }

  void InitVariables()
  {
    PBDClothSolver.SetFloat("deltaT", 0);
    PBDClothSolver.SetFloat("time", time);
    PBDClothSolver.SetVector("gravity", gravity);
    PBDClothSolver.SetFloat("stiffness", stiffness);
    PBDClothSolver.SetFloat("bendiness", bendiness);
    PBDClothSolver.SetFloat("collisionRadius", collisionRadius);
    PBDClothSolver.SetFloat("meshCollisionRadius", meshCollisionRadius);
    PBDClothSolver.SetFloat("meshDampRadius", meshDampRadius);

    PBDClothSolver.SetFloat("particleMass", particleMass);
    PBDClothSolver.SetFloat("particleInvertMass", particleInvertMass);

    PBDClothSolver.SetVector("windDirection", windDirection);
    PBDClothSolver.SetFloat("windStrength", windStrength);
    PBDClothSolver.SetFloat("windSpeed", windSpeed);
    PBDClothSolver.SetFloat("turbulence", turbulence);
    PBDClothSolver.SetFloat("drag", drag);
    PBDClothSolver.SetFloat("lift", lift);

    PBDClothSolver.SetInt("totalSimulationVerts", totalSimulationVerts);
    PBDClothSolver.SetInt("totalTriangles", totalSimulationTriangles);
    PBDClothSolver.SetInt("totalMeshVerts", skinnedMeshCollider.vertexCount);
  }

  void UpdateVariables()
  {
    time += Time.deltaTime;
    PBDClothSolver.SetFloat("deltaT", 0.01f);
    PBDClothSolver.SetFloat("time", time);
    PBDClothSolver.SetVector("gravity", gravity);
    PBDClothSolver.SetFloat("stiffness", stiffness);
    PBDClothSolver.SetFloat("meshCollisionRadius", meshCollisionRadius);
    PBDClothSolver.SetFloat("meshDampRadius", meshDampRadius);

    PBDClothSolver.SetVector("windDirection", windDirection);
    PBDClothSolver.SetFloat("windStrength", windStrength);
    PBDClothSolver.SetFloat("windSpeed", windSpeed);
    PBDClothSolver.SetFloat("turbulence", turbulence);
    PBDClothSolver.SetFloat("drag", drag);
    PBDClothSolver.SetFloat("lift", lift);
  }

  void DispatchKernels()
  {
    positions.GetData(pos);
    PBDClothSolver.Dispatch(ExternalForce, totalSimulationVerts, 1, 1);
    PBDClothSolver.Dispatch(MeshCollision, totalSimulationVerts, 1, 1);
    PBDClothSolver.Dispatch(DampVelocities, totalSimulationVerts, 1, 1);
    // PBDClothSolver.Dispatch(ApplyExplicitEuler, totalSimulationVerts, 1, 1);
    PBDClothSolver.Dispatch(ApplyExplicitEuler, totalSimulationVerts, 1, 1);
    // PBDClothSolver.Dispatch(UpdatePositions, totalSimulationVerts, 1, 1);
    for(int i=0; i < iterationSteps; i++)
    {
      PBDClothSolver.Dispatch(DistanceConstraint, totalSimulationTriangles, 1, 1);
      PBDClothSolver.Dispatch(AverageConstraintDeltas, totalSimulationVerts, 1, 1);
    }
    // PBDClothSolver.Dispatch(SelfCollision, totalSimulationVerts, 1, 1);
    // PBDClothSolver.Dispatch(AverageConstraintDeltas, totalSimulationVerts, 1, 1);
    // PBDClothSolver.Dispatch(UpdatePositions, totalSimulationVerts, 1, 1);
    // PBDClothSolver.Dispatch(AverageConstraintDeltas, totalSimulationVerts, 1, 1);
    PBDClothSolver.Dispatch(UpdatePositions, totalSimulationVerts, 1, 1);
    skinnedMeshCollider.BakeMeshData();
    skinnedMeshPositions.SetData(skinnedMeshCollider.bakedVertices.ToArray());
    skinnedMeshNormals.SetData(skinnedMeshCollider.bakedNormals.ToArray());
  }

  void PBDOnCPU()
  {
    // gravity
    for (int i=0; i < totalSimulationVerts; i++)
    {
      vel[i] += gravity * particleInvertMass * Time.deltaTime * bw[i];
      pos[i] += vel[i] * Time.deltaTime;
    }
    Vector3 BinaryDistanceConstraint(Vector3 pI, Vector3 pJ, float wI, float wJ, float restd)
    {
      // returns where pI should go
      if (wI == 0) return Vector3.zero;
      else return (wI/(wI + wJ)) * (Vector3.Distance(pI, pJ) - restd) * Vector3.Normalize(pJ-pI);
    }

    for (int _i=0; _i < iterationSteps; _i++)
    {
      for (int i=0; i < totalSimulationTriangles; i++)
      {
        int idxA = tri[i].A;
        int idxB = tri[i].B;
        int idxC = tri[i].C;

        Vector3 A = pos[idxA];
        Vector3 B = pos[idxB];
        Vector3 C = pos[idxC];

        float AB = tri[i].AB;
        float BC = tri[i].BC;
        float CA = tri[i].CA;

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

        A += dA * stiffness;
        B += dB * stiffness;
        C += dC * stiffness;
        
        pos[idxA] = A;
        pos[idxB] = B;
        pos[idxC] = C;
        vel[idxA] += dA/Time.deltaTime/iterationSteps;
        vel[idxB] += dB/Time.deltaTime/iterationSteps;
        vel[idxC] += dC/Time.deltaTime/iterationSteps;
      }
    }
  }

  void UpdatePositionToMesh()
  {
    for (int i=0; i < totalSimulationVerts; i++)
    {
      List<int> pid = vertexData.custom2raw[i];
      for (int _i=0; _i < pid.Count; _i++)
      {
        verts[pid[_i]] = pos[i];
      }
    }
    mesh.vertices = verts;
    mesh.RecalculateNormals();
    
    backMesh.vertices = verts;
    backMesh.normals = _Mesh.ReverseNormals(mesh.normals);
  }

  void ReleaseBuffers()
  {
    if (positions != null) positions.Release();
    if (projectedPositions != null) projectedPositions.Release();
    if (velocities != null) velocities.Release();
    if (deltaPositions != null) deltaPositions.Release();
    if (deltaPositionsUInt != null) deltaPositionsUInt.Release();
    if (deltaCounter != null) deltaCounter.Release();
    if (sortedTriangles != null) sortedTriangles.Release();
    if (boneWeight != null) boneWeight.Release();
    if (skinnedMeshPositions != null) skinnedMeshPositions.Release();
    if (skinnedMeshNormals != null) skinnedMeshNormals.Release();
  }
  #endregion

  #region Mesh Data
  public void SortMeshData()
  {
    vertexData = _Vertex.SortVerticesByPosition(totalVerts, verts);
    vertexData = _Vertex.SortTrianglesByGrp(triangles, totalTrianglePoints, vertexData);

    totalSimulationVerts = vertexData.position.Count;
    totalSimulationTriangles = vertexData.sortedTriangles.Count;
  }

  public void ReloadMeshData()
  {
    if (vertexData.position == null)
    {
      vertexData = _Vertex.LoadData(filename, saveFolder);
    }
    _Vertex.ResetMeshData(vertexData, verts, mesh);
    totalSimulationVerts = vertexData.position.Count;
    totalSimulationTriangles = vertexData.sortedTriangles.Count;
  }

  public void SaveMeshData()
  {
    _Vertex.SaveData(filename, saveFolder, vertexData);
  }
  #endregion
}
