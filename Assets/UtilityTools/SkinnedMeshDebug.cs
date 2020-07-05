using UnityEngine;

[RequireComponent(typeof(SkinnedMeshRenderer))]
public class SkinnedMeshDebug : MonoBehaviour
{
  public float normalLength = 5.0f;
  
  void Start()
  {
    SkinnedMesh mesh = GetComponent<SkinnedMesh>();
    mesh.OnResultsReady += DrawVertices;
  }

  void DrawVertices(SkinnedMesh mesh)
  {
    Color color = Color.green;
    var m = transform.localToWorldMatrix;
    for (int i = 0; i < mesh.vertexCount; i++)
    {
      Vector3 position =  mesh.bakedVertices[i];
      Vector3 normal = mesh.bakedNormals[i];
      Debug.DrawLine(position, position + (normal * normalLength), color);
    }
  }
}