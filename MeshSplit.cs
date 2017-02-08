using UnityEngine;
using System.Collections.Generic;

/*  

    Made by Artur Nasiadko
    https://github.com/artnas

    1. Add this thing to the plane you want to split
    2. Press the Split button
    3. ???
    4. Profit

*/

public class MeshSplit : MonoBehaviour
{

    public enum Axis
    {

        y,
        z

    };

    private Mesh baseMesh;
    private MeshRenderer baseRenderer;

    // Size can be much higher than 64, but that would completly defeat the point of this script

    [Range(2, 64)]
    public int size = 16;

    public Axis secondaryAxis = Axis.y;

    public int renderLayerIndex = 0;
    public string renderLayerName = "Default";

    public bool useSortingLayerFromThisMesh = true;

    private Vector3[] baseVerticles;
    private int[] baseTriangles;
    private Vector2[] baseUvs;

    // generated children are kept here, so the script knows what to delete on Split() or Clear()

    [HideInInspector]
    public List<GameObject> childen = new List<GameObject>();

    public void Split()
    {

        for (int i = 0; i < childen.Count; i++)
        {

            DestroyImmediate(childen[i]);

        }
        childen.Clear();

        if (GetComponent<MeshFilter>() == null)
        {
            print("Mesh Filter is missing");
            return;
        }

        baseMesh = GetComponent<MeshFilter>().sharedMesh;

        baseRenderer = GetComponent<MeshRenderer>();
        if (baseRenderer)
        baseRenderer.enabled = false;

        baseVerticles = baseMesh.vertices;
        baseTriangles = baseMesh.triangles;
        baseUvs = baseMesh.uv;

        int boundsHeightMin; 
        if (secondaryAxis == Axis.y)
            boundsHeightMin = (int)baseMesh.bounds.min.y;
        else
            boundsHeightMin = (int)baseMesh.bounds.min.z;

        int boundsHeightMax;
        if (secondaryAxis == Axis.y)
            boundsHeightMax = (int)baseMesh.bounds.max.y;
        else
            boundsHeightMax = (int)baseMesh.bounds.max.z;

        for (int y = boundsHeightMin; y <= boundsHeightMax + size; y += size)
        {

            for (int x = (int)baseMesh.bounds.min.x; x <= (int)baseMesh.bounds.max.x + size; x += size)
            {

                if (secondaryAxis == Axis.y)
                {
                    CreateMesh(new Vector3(x + size/2, y + size/2));
                }
                else
                {
                    CreateMesh(new Vector3(x + size/2, 0, y + size/2));
                }

            }

        }

    }

    public void Clear()
    {

        for (int i = 0; i < childen.Count; i++)
        {

            DestroyImmediate(childen[i]);

        }

        childen.Clear();

        GetComponent<MeshRenderer>().enabled = true;

    }

    public double Distance(Vector3 a, Vector3 b)
    {

        float xd = a.x - b.x;
        float yd;
        
        if (secondaryAxis == Axis.y)
            yd = a.y - b.y;
        else
            yd = a.z - b.z;

        return Mathf.Max(Mathf.Abs(xd), Mathf.Abs(yd));

    }

    public void CreateMesh(Vector3 pivot)
    {

        // create a new game object

        GameObject newObject = new GameObject();
        newObject.name = "SubMesh " + pivot;
        newObject.transform.SetParent(transform);
        newObject.transform.localPosition = Vector3.zero;
        newObject.AddComponent<MeshFilter>();
        newObject.AddComponent<MeshRenderer>();

        MeshRenderer newRenderer = newObject.GetComponent<MeshRenderer>();
        newRenderer.sharedMaterial = GetComponent<MeshRenderer>().sharedMaterial;

        // sorting order and layer name of the generated mesh renderer

        if (!useSortingLayerFromThisMesh)
        {
            newRenderer.sortingLayerName = renderLayerName;
            newRenderer.sortingOrder = renderLayerIndex;
        }
        else if (baseRenderer)
        {
            newRenderer.sortingLayerName = baseRenderer.sortingLayerName;
            newRenderer.sortingOrder = baseRenderer.sortingOrder;
        }

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        int actualIndex = 0;

        bool isEmpty = true;

        for (int i = 0; i < baseTriangles.Length; i += 3)
        {

            // get the middle position of current triangle (average of its 3 verts)

            Vector3 currentPoint =
                (baseVerticles[baseTriangles[i]] +
                 baseVerticles[baseTriangles[i + 1]] +
                 baseVerticles[baseTriangles[i + 2]])/3;
                    
            double dist = Distance(currentPoint, pivot);
            if (dist > (float)size / 2) continue;

            // Do the things

            verts.Add(baseVerticles[baseTriangles[i]]);
            verts.Add(baseVerticles[baseTriangles[i + 1]]);
            verts.Add(baseVerticles[baseTriangles[i + 2]]);

            tris.Add(actualIndex);
            tris.Add(actualIndex + 1);
            tris.Add(actualIndex + 2);

            actualIndex += 3;

            uvs.Add(baseUvs[baseTriangles[i]]);
            uvs.Add(baseUvs[baseTriangles[i + 1]]);
            uvs.Add(baseUvs[baseTriangles[i + 2]]);

            isEmpty = false;

        }

        // Return if the mesh is empty

        if (isEmpty)
        {
            DestroyImmediate(newObject);
            return;
        }

        // add the new object to children

        childen.Add(newObject);

        // Create a new mesh

        Mesh m = new Mesh();

        m.name = pivot.ToString();

        m.vertices = verts.ToArray();
        m.triangles = tris.ToArray();
        m.uv = uvs.ToArray();

        UnityEditor.MeshUtility.Optimize(m);
        m.RecalculateNormals();

        // assign the new mesh to submeshes mesh filter

        MeshFilter newMeshFilter = newObject.GetComponent<MeshFilter>();
        newMeshFilter.mesh = m;

        newObject.isStatic = true;

    }

}
