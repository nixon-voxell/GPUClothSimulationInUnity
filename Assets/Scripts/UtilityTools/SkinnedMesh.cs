using UnityEngine;
using System.Collections.Generic;

// https://forum.unity.com/threads/get-skinned-vertices-in-real-time.15685/

/// <summary>
/// Compute a skinned mesh's deformation.
/// 
/// The script must be attached aside a SkinnedMeshRenderer,
/// which is only used to get the bone list and the mesh
/// (it doesn't even need to be enabled).
/// 
/// Make sure the scripts accessing the results run after this one
/// (otherwise you'll have a 1-frame delay),
/// or use the OnResultsReady delegate.
/// </summary>
[RequireComponent(typeof(SkinnedMeshRenderer))]
public class SkinnedMesh : MonoBehaviour
{
  private Mesh mesh;
  private SkinnedMeshRenderer skin;

  private Mesh tempMesh;

  public bool useBakeMesh = true;
  public int vertexCount;
  [HideInInspector]
  public Vector3[] vertices;
  [HideInInspector]
  public Vector3[] normals;
  public System.Action<SkinnedMesh> OnResultsReady;

  private Matrix4x4[] boneMatrices;
  private BoneWeight[] meshBoneWeights; // getBoneWeights() ~1.8GB for 8k verts
  private Matrix4x4[] meshBindposes; // probably not required
  private Vector3[] meshVerts; // getVerst() ~0.7GB for 8k verts
  private Vector3[] meshNormals; // getNormals() ~0.7GB for 8k verts
  private Transform[] skinnedBones;

  [HideInInspector]
  public List<Vector3> bakedVertices = new List<Vector3>();
  [HideInInspector]
  public List<Vector3> bakedNormals = new List<Vector3>();

  public float bakedScale = 0.01f;
  public bool transformBaked = true;

  void Start()
  {
    skin = GetComponent<SkinnedMeshRenderer>();
    mesh = skin.sharedMesh;

    vertexCount = mesh.vertexCount;
    vertices = new Vector3[vertexCount];
    normals = new Vector3[vertexCount];

    boneMatrices = new Matrix4x4[skin.bones.Length];
    meshBoneWeights = mesh.boneWeights;
    meshBindposes = mesh.bindposes;
    meshVerts = mesh.vertices;
    meshNormals = mesh.normals;
    skinnedBones = skin.bones;
    
    tempMesh = new Mesh();
    //Debug.LogError("Don't use this class, use skin.BakeMesh()");
  }

  void LateUpdate()
  {
   
    // 1,300 fps with bake mesh
    if (useBakeMesh)
    {
      skin.BakeMesh(tempMesh);
      // don't use tempMesh.vertices; or tempMesh.normals; (creates memory)
      // lists are ok
      tempMesh.GetVertices(bakedVertices);
      tempMesh.GetNormals(bakedNormals);

      if (transformBaked)
      {
        for (int i = 0; i < mesh.vertexCount; i++)
        {
          bakedVertices[i] = transform.TransformPoint(bakedVertices[i] * bakedScale);
          bakedNormals[i] = transform.TransformDirection(bakedNormals[i]).normalized;
        }
      }
       
      if (OnResultsReady != null)
      {
        OnResultsReady(this);
      }
        
      return;
    }

    // ~125 fps
    for (int i = 0; i < boneMatrices.Length; i++)
      boneMatrices[i] = skinnedBones[i].localToWorldMatrix * meshBindposes[i];

    BoneWeight weight;
    Matrix4x4 bm0;
    Matrix4x4 bm1;
    Matrix4x4 bm2;
    Matrix4x4 bm3;
    Matrix4x4 vm = new Matrix4x4();

    for (int i = 0; i < meshVerts.Length; i++)
    {
       weight = meshBoneWeights[i];
       bm0 = boneMatrices[weight.boneIndex0];
       bm1 = boneMatrices[weight.boneIndex1];
       bm2 = boneMatrices[weight.boneIndex2];
       bm3 = boneMatrices[weight.boneIndex3];

      // YoungXi 56fps => 125fps
      vm.m00 = bm0.m00 * weight.weight0 + bm1.m00 * weight.weight1 + bm2.m00 * weight.weight2 + bm3.m00 * weight.weight3;
      vm.m01 = bm0.m01 * weight.weight0 + bm1.m01 * weight.weight1 + bm2.m01 * weight.weight2 + bm3.m01 * weight.weight3;
      vm.m02 = bm0.m02 * weight.weight0 + bm1.m02 * weight.weight1 + bm2.m02 * weight.weight2 + bm3.m02 * weight.weight3;
      vm.m03 = bm0.m03 * weight.weight0 + bm1.m03 * weight.weight1 + bm2.m03 * weight.weight2 + bm3.m03 * weight.weight3;

      vm.m10 = bm0.m10 * weight.weight0 + bm1.m10 * weight.weight1 + bm2.m10 * weight.weight2 + bm3.m10 * weight.weight3;
      vm.m11 = bm0.m11 * weight.weight0 + bm1.m11 * weight.weight1 + bm2.m11 * weight.weight2 + bm3.m11 * weight.weight3;
      vm.m12 = bm0.m12 * weight.weight0 + bm1.m12 * weight.weight1 + bm2.m12 * weight.weight2 + bm3.m12 * weight.weight3;
      vm.m13 = bm0.m13 * weight.weight0 + bm1.m13 * weight.weight1 + bm2.m13 * weight.weight2 + bm3.m13 * weight.weight3;

      vm.m20 = bm0.m20 * weight.weight0 + bm1.m20 * weight.weight1 + bm2.m20 * weight.weight2 + bm3.m20 * weight.weight3;
      vm.m21 = bm0.m21 * weight.weight0 + bm1.m21 * weight.weight1 + bm2.m21 * weight.weight2 + bm3.m21 * weight.weight3;
      vm.m22 = bm0.m22 * weight.weight0 + bm1.m22 * weight.weight1 + bm2.m22 * weight.weight2 + bm3.m22 * weight.weight3;
      vm.m23 = bm0.m23 * weight.weight0 + bm1.m23 * weight.weight1 + bm2.m23 * weight.weight2 + bm3.m23 * weight.weight3;
      
      vertices[i] = vm.MultiplyPoint3x4(meshVerts[i]);
      normals[i] = vm.MultiplyVector(meshNormals[i]).normalized; 
    }

    if (OnResultsReady != null)
    {
      OnResultsReady(this);
    }
  }
}