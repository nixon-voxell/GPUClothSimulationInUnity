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
  Vector3[] pos;
  Vector3[] vel;
  Triangleids[] tri;
  float[] rl;
  float[] bw;
  float time = 0;

  [HideInInspector]
  public VertexData vertexData;
  #endregion

  #region Cloth Simulation Parameters
  [Header("Cloth Simulation Parameters")]
  public ComputeShader solver;
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
  public float collisionRadius = 0.1f;

  [Tooltip("Mass of each particle representing each vertices.")]
  public float particleMass = 0.1f;
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
  public SkinnedMeshRenderer[] skinnedMeshColliders;
  public SphereCollider[] SphereColliders;
  public BoxCollider[] BoxColliders;
  public CapsuleCollider[] CapsuleColliders;
  #endregion

  #region Debug Settings
  [Header("Debug Settings")]
  public GameObject parentObject;
  public GameObject particlePrefab;
  public Vector3 particleScale = new Vector3(0.01f, 0.01f, 0.01f);
  GameObject[] particles;
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
  ComputeBuffer positions;
  ComputeBuffer projectedPositions;
  ComputeBuffer velocities;
  ComputeBuffer boneWeight;
  ComputeBuffer sortedTriangles;

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
  int SelfCollision;
  int UpdatePositions;
  #endregion

  #region Unity Callbacks
  void Start()
  {
    vertexData = _Vertex.LoadData(filename, saveFolder);
    _Vertex.InitRawMesh(mesh,
    out verts,
    out totalVerts,
    out triangles,
    out totalTrianglePoints);
    mesh.MarkDynamic();
    InitKernels();
    InitBuffers();
    InitVariables();
  }

  void FixedUpdate()
  {
    UpdateVariables();
    // DispatchKernels();
    PBDOnCPU();
    UpdatePositionToMesh();
  }

  void OnDestroy()
  {
    DestroyBuffers();
    ReloadMeshData();
  }
  #endregion

  // void CreateBackSide()
  // {
  //   GameObject newCloth = new GameObject("back");
  //   newCloth.transform.parent = transform;
  //   newCloth.transform.localPosition = Vector3.zero;
  //   newCloth.transform.localRotation = Quaternion.identity;
  //   newCloth.transform.localScale = new Vector3(1, 1, 1);
  //   newCloth.AddComponent<MeshRenderer>();
  //   newCloth.GetComponent<MeshRenderer>().material = GetComponent<MeshRenderer>().material;
  //   newCloth.AddComponent<MeshFilter>();
  //   newCloth.AddComponent<MeshCollider>();
  //   reverseMesh = Utility.DeepCopyMesh(mesh);
  //   reverseMesh.MarkDynamic();

  //   // reverse the triangle order
  //   for (int m = 0; m < reverseMesh.subMeshCount; m++) {
  //     int[] triangles = reverseMesh.GetTriangles(m);
  //     for (int i = 0; i < triangles.Length; i += 3) {
  //       int temp = triangles[i + 0];
  //       triangles[i + 0] = triangles[i + 1];
  //       triangles[i + 1] = temp;
  //     }
  //     reverseMesh.SetTriangles(triangles, m);
  //   }
  //   newCloth.GetComponent<MeshFilter>().mesh = reverseMesh;
  //   GetComponent<MeshCollider>().sharedMesh = reverseMesh;
  // }

  #region ComputeShader Helpers
  void InitKernels()
  {
    // forces
    ExternalForce = solver.FindKernel("ExternalForce");
    DampVelocities = solver.FindKernel("DampVelocities");
    ApplyExplicitEuler = solver.FindKernel("ApplyExplicitEuler");

    // collisions
    MeshCollision = solver.FindKernel("MeshCollision");
    SphereCollision = solver.FindKernel("SphereCollision");
    BoxCollision = solver.FindKernel("BoxCollision");
    CapsuleCollision = solver.FindKernel("CapsuleCollision");

    // constraints
    DistanceConstraint = solver.FindKernel("DistanceConstraint");
    BendConstraint = solver.FindKernel("BendConstraint");
    SelfCollision = solver.FindKernel("SelfCollision");
    UpdatePositions = solver.FindKernel("UpdatePositions");
  }

  void InitBuffers()
  {
    // strides
    int strideVector3 = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector3));
    int strideInt = System.Runtime.InteropServices.Marshal.SizeOf(typeof(int));
    int strideFloat = System.Runtime.InteropServices.Marshal.SizeOf(typeof(float));
    int strideTriangleids = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Triangleids));

    positions = new ComputeBuffer(totalSimulationVerts, strideVector3);
    projectedPositions = new ComputeBuffer(totalSimulationVerts, strideVector3);
    velocities = new ComputeBuffer(totalSimulationVerts, strideVector3);
    boneWeight = new ComputeBuffer(totalSimulationVerts, strideFloat);
    sortedTriangles = new ComputeBuffer(totalTrianglePoints, strideTriangleids);

    // forces
    solver.SetBuffer(ExternalForce, "velocities", velocities);
    solver.SetBuffer(ExternalForce, "boneWeight", boneWeight);

    solver.SetBuffer(DampVelocities, "velocities", velocities);

    solver.SetBuffer(ApplyExplicitEuler, "positions", positions);
    solver.SetBuffer(ApplyExplicitEuler, "projectedPositions", projectedPositions);
    solver.SetBuffer(ApplyExplicitEuler, "velocities", velocities);

    // constraints
    solver.SetBuffer(DistanceConstraint, "projectedPositions", projectedPositions);
    solver.SetBuffer(DistanceConstraint, "sortedTriangles", sortedTriangles);
    solver.SetBuffer(DistanceConstraint, "boneWeight", boneWeight);

    // apply changes buffers (we just need curr and updated position)
    solver.SetBuffer(UpdatePositions, "positions", positions);
    solver.SetBuffer(UpdatePositions, "projectedPositions", projectedPositions);
    solver.SetBuffer(UpdatePositions, "velocities", velocities);

    // set data to buffers
    pos = new Vector3[totalSimulationVerts];
    vel = new Vector3[totalSimulationVerts];
    tri = new Triangleids[totalSimulationTriangles];
    bw = new float[totalSimulationVerts];

    for (int i=0; i < totalSimulationVerts; i++)
    {
      vel[i] = Vector3.zero;
      bw[i] = 1;
    }
    bw[0] = 0;
    bw[10] = 0;
    pos = _Convert.FloatListToVector3List(vertexData.position);
    // pos = _Convert.WoldVector3ListToLocal(transform, pos);
    tri = vertexData.sortedTriangles.ToArray();

    positions.SetData(pos);
    projectedPositions.SetData(pos);
    velocities.SetData(vel);
    sortedTriangles.SetData(tri);
    boneWeight.SetData(bw);
  }

  void InitVariables()
  {
    solver.SetFloat("deltaT", 0);
    solver.SetFloat("time", time);
    solver.SetVector("gravity", gravity);
    solver.SetFloat("stiffness", (1 - Mathf.Pow((1 - stiffness), (float)1/iterationSteps)));
    solver.SetFloat("bendiness", bendiness);
    solver.SetFloat("collisionRadius", collisionRadius);

    solver.SetFloat("particleMass", particleMass);
    solver.SetFloat("particleInvertMass", particleInvertMass);

    solver.SetVector("windDirection", windDirection);
    solver.SetFloat("windStrength", windStrength);
    solver.SetFloat("windSpeed", windSpeed);
    solver.SetFloat("turbulence", turbulence);
    solver.SetFloat("drag", drag);
    solver.SetFloat("lift", lift);

    solver.SetInt("totalSimulationVerts", totalSimulationVerts);
    solver.SetInt("totalTriangles", totalSimulationTriangles);

    // solver.SetInt("iterationSteps", (int)iterationSteps);
  }

  void UpdateVariables()
  {
    time += Time.deltaTime;
    solver.SetFloat("deltaT", Time.deltaTime);
    solver.SetFloat("time", time);
  }

  void DispatchKernels()
  {
    solver.Dispatch(ExternalForce, totalSimulationVerts, 1, 1);
    solver.Dispatch(DampVelocities, totalSimulationVerts, 1, 1);
    solver.Dispatch(ApplyExplicitEuler, totalSimulationVerts, 1, 1);
    // for(int i=0; i < iterationSteps; i++)
    // {
    //   solver.Dispatch(DistanceConstraint, totalSimulationTriangles, 1, 1);
    // }
    solver.Dispatch(UpdatePositions, totalSimulationVerts, 1, 1);
    positions.GetData(pos);
  }

  void PBDOnCPU()
  {
    // gravity
    for (int i=0; i < totalSimulationVerts; i++)
    {
      if (i!= 0 && i != 10)
      {
        vel[i] += gravity * particleInvertMass * Time.deltaTime;
        pos[i] += vel[i] * Time.deltaTime;
      }
    }
    Vector3 BinaryDistanceConstraint(Vector3 pI, Vector3 pJ, float wI, float wJ, float restd)
    {
      // returns where pI should go
      return (wI/(wI + wJ)) * ((pI - pJ).magnitude - restd) * Vector3.Normalize(pJ-pI);
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

        // GameObject p;
        // p = Instantiate(particlePrefab, transform.TransformPoint(A), Quaternion.identity);
        // p.transform.localScale = particleScale;
        // p.name = "A";
        // p.transform.parent = parentObject.transform;
        // p = Instantiate(particlePrefab, transform.TransformPoint(B), Quaternion.identity);
        // p.transform.localScale = particleScale;
        // p.name = "B";
        // p.transform.parent = parentObject.transform;

        // print(AB);
        // print(Vector3.Distance(A, B));
        // break;

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
  }

  void DestroyBuffers()
  {
    positions.Release();
    projectedPositions.Release();
    velocities.Release();
    sortedTriangles.Release();
    boneWeight.Release();
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
