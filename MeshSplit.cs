/*  

    Made by Artur Nasiadko
    https://github.com/artnas

    1. Add this thing to the plane you want to split
    2. Press the Split button
    3. ???
    4. Profit

*/

using UnityEngine;
using System.Collections.Generic;

public class MeshSplit : MonoBehaviour
{

    public struct GridCoordinates
    {

        private static readonly float precision = 100;

        public int x, y, z;

        public GridCoordinates(float x, float y, float z)
        {
            this.x = Mathf.RoundToInt(x * precision);
            this.y = Mathf.RoundToInt(y * precision);
            this.z = Mathf.RoundToInt(z * precision);
        }

        public static implicit operator GridCoordinates(Vector3 v)
        {
            return new GridCoordinates((int)v.x, (int)v.y, (int)v.z);
        }
        public static implicit operator Vector3(GridCoordinates i)
        {
            return new Vector3(i.x, i.y, i.z);
        }

        public override string ToString()
        {

            return string.Format("({0},{1},{2})", (float)x / precision, (float)y / precision, (float)z / precision);

        }

    }

    private readonly bool drawGrid = true;

    private Mesh baseMesh;
    private MeshRenderer baseRenderer;

    // Size can be much higher than 64, but that would completly defeat the point of this script

    [Range(0.1f, 64)]
    public float gridSize = 16;

    public bool axisX = true;
    public bool axisY = true;
    public bool axisZ = true;

    public int renderLayerIndex = 0;
    public string renderLayerName = "Default";

    public bool useSortingLayerFromThisMesh = true;
    public bool useStaticSettingsFromThisMesh = true;

    private Vector3[] baseVerticles;
    private int[] baseTriangles;
    private Vector2[] baseUvs;

    private Dictionary<GridCoordinates, List<int>> triDictionary;

    // generated children are kept here, so the script knows what to delete on Split() or Clear()

    [HideInInspector]
    public List<GameObject> childen = new List<GameObject>();

    private void MapTrianglesToGridNodes()
    {

        triDictionary = new Dictionary<GridCoordinates, List<int>>();

        for (int i = 0; i < baseTriangles.Length; i += 3)
        {

            Vector3 currentPoint =
                (baseVerticles[baseTriangles[i]] +
                 baseVerticles[baseTriangles[i + 1]] +
                 baseVerticles[baseTriangles[i + 2]]) / 3;

            currentPoint.x = Mathf.Round(currentPoint.x / gridSize) * gridSize;
            currentPoint.y = Mathf.Round(currentPoint.y / gridSize) * gridSize;
            currentPoint.z = Mathf.Round(currentPoint.z / gridSize) * gridSize;

            GridCoordinates gridPos = new GridCoordinates(
                axisX ? currentPoint.x : 0,
                axisY ? currentPoint.y : 0,
                axisZ ? currentPoint.z : 0
                );

            if (!triDictionary.ContainsKey(gridPos))
            {
                triDictionary.Add(gridPos, new List<int>());
            }

            triDictionary[gridPos].Add(baseTriangles[i]);
            triDictionary[gridPos].Add(baseTriangles[i + 1]);
            triDictionary[gridPos].Add(baseTriangles[i + 2]);

        }

    }

    public void Split()
    {

        DestroyChildren();

        if (GetComponent<MeshFilter>() == null)
        {
            Debug.LogError("Mesh Filter Component is missing.");
            return;
        }
        if (GetUsedAxisCount() < 1)
        {
            Debug.LogError("You have to choose at least 1 axis.");
            return;
        }

        baseMesh = GetComponent<MeshFilter>().sharedMesh;

        baseRenderer = GetComponent<MeshRenderer>();
        if (baseRenderer)
            baseRenderer.enabled = false;

        baseVerticles = baseMesh.vertices;
        baseTriangles = baseMesh.triangles;
        baseUvs = baseMesh.uv;

        MapTrianglesToGridNodes();

        foreach (var item in triDictionary.Keys)
        {
            CreateMesh(item, triDictionary[item]);
        }

        //int boundsHeightMin;
        //if (secondaryAxis == Axis.y)
        //    boundsHeightMin = Mathf.CeilToInt(baseMesh.bounds.min.y);
        //else
        //    boundsHeightMin = Mathf.CeilToInt(baseMesh.bounds.min.z);

        //int boundsHeightMax;
        //if (secondaryAxis == Axis.y)
        //    boundsHeightMax = Mathf.CeilToInt(baseMesh.bounds.max.y);
        //else
        //    boundsHeightMax = Mathf.CeilToInt(baseMesh.bounds.max.z);

        //for (float y = boundsHeightMin - gridSize; y <= boundsHeightMax + gridSize; y += gridSize)
        //{

        //    for (float x = (int)baseMesh.bounds.min.x - gridSize; x <= (int)baseMesh.bounds.max.x + gridSize; x += gridSize)
        //    {

        //        if (secondaryAxis == Axis.y)
        //        {
        //            CreateMesh(new Vector3(x + gridSize / 2, y + gridSize / 2));
        //        }
        //        else
        //        {
        //            CreateMesh(new Vector3(x + gridSize / 2, 0, y + gridSize / 2));
        //        }

        //    }

        //}

    }

    private void DestroyChildren()
    {

        for (int i = 0; i < childen.Count; i++)
        {

            DestroyImmediate(childen[i]);

        }

        childen.Clear();

    }

    public void Clear()
    {

        DestroyChildren();

        GetComponent<MeshRenderer>().enabled = true;

    }

    ///// <summary>
    ///// Creates a new mesh from verts/tris/uvs which are close (manhattan distance) to the given pivot 
    ///// </summary>
    ///// <param name="pivot"></param>
    //public void CreateMesh(Vector3 pivot)
    //{

    //    // create a new game object

    //    GameObject newObject = new GameObject();
    //    newObject.name = "SubMesh " + pivot;
    //    newObject.transform.SetParent(transform);
    //    newObject.transform.localPosition = Vector3.zero;
    //    newObject.transform.localScale = Vector3.one;
    //    newObject.AddComponent<MeshFilter>();
    //    newObject.AddComponent<MeshRenderer>();

    //    MeshRenderer newRenderer = newObject.GetComponent<MeshRenderer>();
    //    newRenderer.sharedMaterial = GetComponent<MeshRenderer>().sharedMaterial;

    //    // sorting order and layer name of the generated mesh renderer

    //    if (!useSortingLayerFromThisMesh)
    //    {
    //        newRenderer.sortingLayerName = renderLayerName;
    //        newRenderer.sortingOrder = renderLayerIndex;
    //    }
    //    else if (baseRenderer)
    //    {
    //        newRenderer.sortingLayerName = baseRenderer.sortingLayerName;
    //        newRenderer.sortingOrder = baseRenderer.sortingOrder;
    //    }

    //    List<Vector3> verts = new List<Vector3>();
    //    List<int> tris = new List<int>();
    //    List<Vector2> uvs = new List<Vector2>();

    //    int actualIndex = 0;

    //    bool isEmpty = true;

    //    for (int i = 0; i < baseTriangles.Length; i += 3)
    //    {

    //        // get the middle position of current triangle (average of its 3 verts)

    //        Vector3 currentPoint =
    //            (baseVerticles[baseTriangles[i]] +
    //             baseVerticles[baseTriangles[i + 1]] +
    //             baseVerticles[baseTriangles[i + 2]]) / 3;

    //        // calculate distance from pivot

    //        float dist = ManhattanDistance(currentPoint, pivot);
    //        if (dist > gridSize / 2) continue;

    //        // Do the things

    //        verts.Add(baseVerticles[baseTriangles[i]]);
    //        verts.Add(baseVerticles[baseTriangles[i + 1]]);
    //        verts.Add(baseVerticles[baseTriangles[i + 2]]);

    //        tris.Add(actualIndex++);
    //        tris.Add(actualIndex++);
    //        tris.Add(actualIndex++);

    //        uvs.Add(baseUvs[baseTriangles[i]]);
    //        uvs.Add(baseUvs[baseTriangles[i + 1]]);
    //        uvs.Add(baseUvs[baseTriangles[i + 2]]);

    //        isEmpty = false;

    //    }

    //    // Return if the mesh is empty

    //    if (isEmpty)
    //    {
    //        DestroyImmediate(newObject);
    //        return;
    //    }

    //    // add the new object to children

    //    childen.Add(newObject);

    //    // Create a new mesh

    //    Mesh m = new Mesh();

    //    m.name = pivot.ToString();

    //    m.vertices = verts.ToArray();
    //    m.triangles = tris.ToArray();
    //    m.uv = uvs.ToArray();

    //    UnityEditor.MeshUtility.Optimize(m);
    //    m.RecalculateNormals();

    //    // assign the new mesh to submeshes mesh filter

    //    MeshFilter newMeshFilter = newObject.GetComponent<MeshFilter>();
    //    newMeshFilter.mesh = m;

    //    if (useStaticSettingsFromThisMesh)
    //        newObject.isStatic = gameObject.isStatic;

    //}

    public void CreateMesh(GridCoordinates gridCoordinates, List<int> dictionaryTriangles)
    {

        // create a new game object

        GameObject newObject = new GameObject();
        newObject.name = "SubMesh " + gridCoordinates;
        newObject.transform.SetParent(transform);
        newObject.transform.localPosition = Vector3.zero;
        newObject.transform.localScale = Vector3.one;
        newObject.transform.localRotation = transform.localRotation;
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

        for (int i = 0; i < dictionaryTriangles.Count; i += 3)
        {

            verts.Add(baseVerticles[dictionaryTriangles[i]]);
            verts.Add(baseVerticles[dictionaryTriangles[i + 1]]);
            verts.Add(baseVerticles[dictionaryTriangles[i + 2]]);

            tris.Add(i);
            tris.Add(i + 1);
            tris.Add(i + 2);

            uvs.Add(baseUvs[dictionaryTriangles[i]]);
            uvs.Add(baseUvs[dictionaryTriangles[i + 1]]);
            uvs.Add(baseUvs[dictionaryTriangles[i + 2]]);

        }

        //int actualIndex = 0;

        //bool isEmpty = true;

        //for (int i = 0; i < baseTriangles.Length; i += 3)
        //{

        //    // get the middle position of current triangle (average of its 3 verts)

        //    Vector3 currentPoint =
        //        (baseVerticles[baseTriangles[i]] +
        //         baseVerticles[baseTriangles[i + 1]] +
        //         baseVerticles[baseTriangles[i + 2]]) / 3;

        //    // calculate distance from pivot

        //    float dist = ManhattanDistance(currentPoint, gridCoordinates);
        //    if (dist > gridSize / 2) continue;

        //    // Do the things

        //    verts.Add(baseVerticles[baseTriangles[i]]);
        //    verts.Add(baseVerticles[baseTriangles[i + 1]]);
        //    verts.Add(baseVerticles[baseTriangles[i + 2]]);

        //    tris.Add(actualIndex++);
        //    tris.Add(actualIndex++);
        //    tris.Add(actualIndex++);

        //    uvs.Add(baseUvs[baseTriangles[i]]);
        //    uvs.Add(baseUvs[baseTriangles[i + 1]]);
        //    uvs.Add(baseUvs[baseTriangles[i + 2]]);

        //    isEmpty = false;

        //}

        // Return if the mesh is empty

        //if (isEmpty)
        //{
        //    DestroyImmediate(newObject);
        //    return;
        //}

        // add the new object to children

        childen.Add(newObject);

        // Create a new mesh

        Mesh m = new Mesh();

        m.name = gridCoordinates.ToString();

        m.vertices = verts.ToArray();
        m.triangles = tris.ToArray();
        m.uv = uvs.ToArray();

        UnityEditor.MeshUtility.Optimize(m);
        m.RecalculateNormals();

        // assign the new mesh to submeshes mesh filter

        MeshFilter newMeshFilter = newObject.GetComponent<MeshFilter>();
        newMeshFilter.mesh = m;

        if (useStaticSettingsFromThisMesh)
            newObject.isStatic = gameObject.isStatic;

    }

    private int GetUsedAxisCount()
    {

        return (axisX ? 1 : 0) + (axisY ? 1 : 0) + (axisZ ? 1 : 0);

    }

    public float ManhattanDistance(Vector3 a, Vector3 b)
    {

        float xd = 0;
        if (axisX) xd = a.x - b.x;

        float yd = 0;
        if (axisY) yd = a.y - b.y;

        float zd = 0;
        if (axisZ) zd = a.z - b.z;

        return Mathf.Max(Mathf.Abs(xd), Mathf.Abs(yd), Mathf.Abs(zd));

    }

    void OnDrawGizmosSelected()
    {

        MeshFilter meshFilter = GetComponent<MeshFilter>();

        if (drawGrid && meshFilter && meshFilter.sharedMesh)
        {

            Bounds b = meshFilter.sharedMesh.bounds;

            float xSize = Mathf.Ceil(b.extents.x) + gridSize;
            float ySize = Mathf.Ceil(b.extents.y) + gridSize;
            float zSize = Mathf.Ceil(b.extents.z) + gridSize;

            for (float z = -zSize; z <= zSize; z += gridSize)
            {

                for (float y = -ySize; y <= ySize; y += gridSize)
                {

                    for (float x = -xSize; x <= xSize; x += gridSize)
                    {

                        Vector3 position = transform.position + new Vector3(x, y, z);

                        Gizmos.DrawWireCube(position, gridSize * transform.localScale);

                    }

                }

            }

        }

    }

}
