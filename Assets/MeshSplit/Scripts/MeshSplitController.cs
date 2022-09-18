/* https://github.com/artnas/Unity-Plane-Mesh-Splitter */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MeshSplit.Scripts
{
    public class MeshSplitController : MonoBehaviour
    {
        public bool Verbose;
        
        public MeshSplitParameters Parameters;
        public bool DrawGridGizmosWhenSelected;

        private Mesh _baseMesh;
        private MeshRenderer _baseRenderer;

        // generated children are kept here, so the script knows what to delete on Split() or Clear()
        [HideInInspector] [SerializeField]
        private List<GameObject> Children = new();

        public void Split()
        {
            DestroyChildren();

            if (GetUsedAxisCount() < 1)
            {
                throw new Exception("You have to choose at least 1 axis.");
            }

            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter)
            {
                _baseMesh = meshFilter.sharedMesh;
            }
            else
            {
                throw new Exception("MeshFilter component is required.");
            }

            if (_baseRenderer || TryGetComponent(out _baseRenderer))
            {
                _baseRenderer.enabled = false;
            }

            CreateChildren();
        }

        private void CreateChildren()
        {
            var meshSplitter = new MeshSplitter(Parameters, Verbose);
            var subMeshData = meshSplitter.Split(_baseMesh);

            foreach (var (gridPoint, mesh) in subMeshData)
            {
                if (mesh.vertexCount > 0)
                    CreateChild(gridPoint, mesh);
            }
        }

        private void CreateChild(Vector3Int gridPoint, Mesh mesh)
        {
            var newGameObject = new GameObject
            {
                name = $"SubMesh {gridPoint}"
            };
        
            newGameObject.transform.SetParent(transform, false);
            if (Parameters.UseParentLayer)
            {
                newGameObject.layer = gameObject.layer;
            }
            if (Parameters.UseParentStaticFlag)
            {
                newGameObject.isStatic = gameObject.isStatic;
            }
            
            // assign the new mesh to this submeshes mesh filter
            var newMeshFilter = newGameObject.AddComponent<MeshFilter>();
            newMeshFilter.sharedMesh = mesh;

            var newMeshRenderer = newGameObject.AddComponent<MeshRenderer>();
            if (Parameters.UseParentMeshRendererSettings && _baseRenderer)
            {
                newMeshRenderer.sharedMaterial = _baseRenderer.sharedMaterial;
                newMeshRenderer.sortingOrder = _baseRenderer.sortingOrder;
                newMeshRenderer.sortingLayerID = _baseRenderer.sortingLayerID;
                newMeshRenderer.shadowCastingMode = _baseRenderer.shadowCastingMode;
            }

            if (Parameters.GenerateColliders)
            {
                var meshCollider = newGameObject.AddComponent<MeshCollider>();
                meshCollider.convex = Parameters.UseConvexColliders;
                meshCollider.sharedMesh = mesh;
            }
            
            Children.Add(newGameObject);
        }

        private int GetUsedAxisCount()
        {
            return (Parameters.SplitAxes.x ? 1 : 0) + (Parameters.SplitAxes.y ? 1 : 0) + (Parameters.SplitAxes.z ? 1 : 0);
        }

        public void Clear()
        {
            DestroyChildren();
            
            // reenable renderer
            if (_baseRenderer || TryGetComponent(out _baseRenderer))
            {
                _baseRenderer.enabled = true;
            }
        }

        private void DestroyChildren()
        {
            // find child submeshes which are not in child list
            var unassignedSubMeshes = GetComponentsInChildren<MeshRenderer>()
                .Where(child => child.name.Contains("SubMesh") && !Children.Contains(child.gameObject));

            foreach (var subMesh in unassignedSubMeshes)
            {
                Children.Add(subMesh.gameObject);
            }

            foreach (var t in Children)
            {
                // destroy mesh
                DestroyImmediate(t.GetComponent<MeshFilter>().sharedMesh);
                DestroyImmediate(t);
            }

            Children.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            if (!DrawGridGizmosWhenSelected || !TryGetComponent<MeshFilter>(out var meshFilter) || !meshFilter.sharedMesh || !TryGetComponent<Renderer>(out _))
                return;

            var t = transform;
            var bounds = meshFilter.sharedMesh.bounds;

            var xSize = Parameters.SplitAxes.x ? Mathf.Ceil(bounds.extents.x) : Parameters.GridSize / 2f;
            var ySize = Parameters.SplitAxes.y ? Mathf.Ceil(bounds.extents.y) : Parameters.GridSize / 2f;
            var zSize = Parameters.SplitAxes.z ? Mathf.Ceil(bounds.extents.z) : Parameters.GridSize / 2f;

            var center = bounds.center;
            
            // TODO improve grid alignment

            Gizmos.color = new Color(1, 1, 1, 0.3f);
            
            /* credit for this line drawing code goes to https://github.com/STARasGAMES */

            // X aligned lines
            for (var y = -ySize; y <= ySize; y += Parameters.GridSize)
            {
                for (var z = -zSize; z <= zSize; z += Parameters.GridSize)
                {
                    var start = t.TransformPoint(center + new Vector3(-xSize, y, z));
                    var end = t.TransformPoint(center + new Vector3(xSize, y, z));
                    Gizmos.DrawLine(start, end);
                }
            }

            // Y aligned lines
            for (var x = -xSize; x <= xSize; x += Parameters.GridSize)
            {
                for (var z = -zSize; z <= zSize; z += Parameters.GridSize)
                {
                    var start = t.TransformPoint(center + new Vector3(x, -ySize, z));
                    var end = t.TransformPoint(center + new Vector3(x, ySize, z));
                    Gizmos.DrawLine(start, end);
                }
            }
            
            // Z aligned lines
            for (var y = -ySize; y <= ySize + 1; y += Parameters.GridSize)
            {
                for (var x = -xSize; x <= xSize + 1; x += Parameters.GridSize)
                {
                    var start = t.TransformPoint(center + new Vector3(x, y, -zSize));
                    var end = t.TransformPoint(center + new Vector3(x, y, zSize));
                    Gizmos.DrawLine(start, end);
                }
            }
        }
    }
}
