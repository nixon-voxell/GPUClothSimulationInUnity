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
  Mesh mesh;
  SkinnedMeshRenderer skin;
  Mesh tempMesh;
  [ConditionalHideAttribute("hide")]
  public int vertexCount;
  [HideInInspector]
  public List<Vector3> bakedVertices = new List<Vector3>();
  [HideInInspector]
  public List<Vector3> bakedNormals = new List<Vector3>();
  public System.Action<SkinnedMesh> OnResultsReady;

  public float bakedScale = 1f;
  public bool transformBaked = false;
  // public bool independentScript = false;

  #region Editor Stuffs
  [HideInInspector]
  public bool hide = false;
  #endregion

  // #region UnityCallbacks
  // void Start()
  // {
  //   if (independentScript)
  //   Init();
  // }

  // void LateUpdate()
  // {
  //   if (independentScript)
  //   BakeMeshData();
  // }
  // #endregion

  public void Init()
  {
    skin = GetComponent<SkinnedMeshRenderer>();
    mesh = skin.sharedMesh;
    vertexCount = mesh.vertexCount;
    tempMesh = new Mesh();
    tempMesh.MarkDynamic();
    //Debug.LogError("Don't use this class, use skin.BakeMesh()");
  }

  public void BakeMeshData()
  {
    // 1,300 fps with bake mesh
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
  }

}