using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

public class Chunk
{
    public Spatial ChunkNode;
    Vector3 _position;

    SurfaceTool st = new SurfaceTool();

    StaticBody staticBody;
    MeshInstance meshInstance;
    CollisionShape collisionMesh;

    TerrainData[,,] terrainMap;

    bool smoothTerrain = false;
    bool flatShaded = false;
    bool threadLocked = false;

    List<Vector3> vertices = new List<Vector3>();
    List<Color> colours = new List<Color>();
    List<int> triangles = new List<int>();

    // Called when the node enters the scene tree for the first time.
    public Chunk(Vector3 position)
    {   
        _position = position;
        ChunkNode = new Spatial();
        ChunkNode.Translate(_position);
        ChunkNode.Name = $"Chunk ( X:{_position.x} Z:{_position.z} )";

        meshInstance = new MeshInstance();
        staticBody = new StaticBody();
        collisionMesh = new CollisionShape();

        ChunkNode.AddChild(meshInstance);

        ChunkNode.AddChild(staticBody);
        staticBody.AddChild(collisionMesh);
        
        terrainMap = new TerrainData[GameData.ChunkWidth + 1, GameData.ChunkHeight + 1, GameData.ChunkWidth + 1];
        
        //System.Threading.Thread myThread = new System.Threading.Thread(new ThreadStart(PopulateTerrainMap));
        //myThread.Start();

        System.Threading.Tasks.Task populateMap = new Task(PopulateTerrainMap);
        populateMap.Start();

        //PopulateTerrainMap();

    }

    void PopulateTerrainMap ()
    {
        float colourR = 20f;
        float colourG = 150f;
        float colourB = 20f;

        // The data points for terrain are stored at the corners of our "cubes", so the terrainMap needs to be 1 larger
        // than the width/height of our mesh.
        for (int x = 0; x < GameData.ChunkWidth + 1; x++) {
            for (int z = 0; z < GameData.ChunkWidth + 1; z++) {
                for (int y = 0; y < GameData.ChunkHeight + 1; y++) {

                    // Get a terrain height using regular old Perlin noise.
                    float thisHeight = GameData.GetNoise3D(x + (int)_position.x, y + (int)_position.y, z + (int)_position.z); // = 8 * noise.GetNoise3d((float)x, (float)y, (float)z);

                    // Set the value of this point in the terrainMap.

                    // Terracing
                    //terrainMap[x, y, z] = -(float)y - thisHeight + (float)y % GameData.TerracedHeight;

                    if (x == 0 && x > GameData.ChunkWidth && y == 0 && y > GameData.ChunkHeight && z == 0 && z > GameData.ChunkWidth)
                        thisHeight = 0f;
                    
                    terrainMap[x, y, z] = new TerrainData(thisHeight, new Color(colourR, colourG, colourB));

                }
            }
        }

        _createMeshData();
    }

    public void CreateMeshData()
    {
        //_createMeshData();

        //System.Threading.Tasks.Task createMeshData = new Task(_createMeshData);
        //createMeshData.Start();

        //System.Threading.Thread myThread = new System.Threading.Thread(new ThreadStart(_createMeshData));
        //myThread.Start();
    }

    public void _createMeshData()
    {
        threadLocked = true;

        ClearMeshData();

        // Loop through each "cube" in our terrain.
        for (int x = 0; x < GameData.ChunkWidth; x++) {
            for (int y = 0; y < GameData.ChunkHeight; y++) {
                for (int z = 0; z < GameData.ChunkWidth; z++) {

                    // Pass the value into our MarchCube function.
                    MarchCube(new Vector3(x, y, z), terrainMap[x, y, z]._colour);
                }
            }
        }

        UpdateMeshData();

        threadLocked = false;
    }
    
    void MarchCube (Vector3 position, Color colour) 
    {
        // Sample terrain values at each corner of the cube.
        float[] cube = new float[8];
        for (int i = 0; i < 8; i++) {

            cube[i] = SampleTerrain(position + GameData.CornerTable[i]);

        }

        // Get the configuration index of this cube.
        int configIndex = GetCubeConfiguration(cube);

        // If the configuration of this cube is 0 or 255 (completely inside the terrain or completely outside of it) we don't need to do anything.
        if (configIndex == 0 || configIndex == 255)
            return;

        // Loop through the triangles. There are never more than 5 triangles to a cube and only three vertices to a triangle.
        int edgeIndex = 0;
        for(int i = 0; i < 5; i++) {
            for(int p = 0; p < 3; p++) {

                // Get the current indice. We increment triangleIndex through each loop.
                int indice = GameData.TriangleTable[configIndex, edgeIndex];

                // If the current edgeIndex is -1, there are no more indices and we can exit the function.
                if (indice == -1)
                    return;

                // Get the vertices for the start and end of this edge.
                Vector3 vert1 = position + GameData.CornerTable[GameData.EdgeIndexes[indice, 0]];
                Vector3 vert2 = position + GameData.CornerTable[GameData.EdgeIndexes[indice, 1]];

                Vector3 vertPosition;
                if (smoothTerrain) {

                    // Get the terrain values at either end of our current edge from the cube array created above.
                    float vert1Sample = cube[GameData.EdgeIndexes[indice, 0]];
                    float vert2Sample = cube[GameData.EdgeIndexes[indice, 1]];

                    // Calculate the difference between the terrain values.
                    float difference = vert2Sample - vert1Sample;

                    // If the difference is 0, then the terrain passes through the middle.
                    if (difference == 0)
                        difference = GameData.TerrainSurface;
                    else
                        difference = (GameData.TerrainSurface - vert1Sample) / difference;

                    // Calculate the point along the edge that passes through.
                    vertPosition = vert1 + ((vert2 - vert1) * difference);


                } else {

                    // Get the midpoint of this edge.
                    vertPosition = (vert1 + vert2) / 2f;

                }

                // Add to our vertices and triangles list and incremement the edgeIndex.
                if (flatShaded) {

                    vertices.Add(vertPosition);
                    triangles.Add(vertices.Count - 1);

                } else
                    triangles.Add(VertForIndice(vertPosition));

                edgeIndex++;

            }
        }
    }

    float SampleTerrain (Vector3 point) {

        return terrainMap[(int)point.x, (int)point.y, (int)point.z]._value;

    }

    public void PlaceTerrain (Vector3 pos) {

        Vector3 v3Int = new Vector3(Mathf.CeilToInt(pos.x), Mathf.CeilToInt(pos.y), Mathf.CeilToInt(pos.z));
        v3Int -= _position;
        terrainMap[(int)v3Int.x, (int)v3Int.y, (int)v3Int.z]._value = 1f;
        CreateMeshData();

    }

    public void RemoveTerrain (Vector3 pos) {

        Vector3 v3Int = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
        v3Int -= _position;
        terrainMap[(int)v3Int.x, (int)v3Int.y, (int)v3Int.z]._value = 0f;
        CreateMeshData();

    }


    int VertForIndice (Vector3 vert) {

        // Loop through all the vertices currently in the vertices list.
        for (int i = 0; i < vertices.Count; i++) {

            // If we find a vert that matches ours, then simply return this index.
            if (vertices[i] == vert)
                return i;

        }

        // If we didn't find a match, add this vert to the list and return last index.
        colours.Add((new System.Random().Next(0, 2) == 0 ? Colors.Black : Colors.Red));
        vertices.Add(vert);
        return vertices.Count - 1;

    }

    int GetCubeConfiguration (float[] cube) {

        // Starting with a configuration of zero, loop through each point in the cube and check if it is below the terrain surface.
        int configurationIndex = 0;
        for (int i = 0; i < 8; i++) {

            // If it is, use bit-magic to the set the corresponding bit to 1. So if only the 3rd point in the cube was below
            // the surface, the bit would look like 00100000, which represents the integer value 32.
            if (cube[i] > GameData.TerrainSurface)
                configurationIndex |= 1 << i;

        }

        return configurationIndex;

    }

    public void ClearMeshData()
    {
        vertices.Clear();
        triangles.Clear();
        colours.Clear();
    }

    public void UpdateMeshData()
    {
        st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        //GD.Print(vertices.Count);
        //GD.Print("Updating Mesh Data...");
        for (int i = 0; i < vertices.Count; i++)
        {
            //GD.Print("Adding Vertex Data...");
            st.AddColor(colours[i]);
            st.AddVertex(vertices[i]);
        }

        for (int i = 0; i < triangles.Count; i++)
        {
            //GD.Print("Adding Triangle Data...");
            st.AddIndex(triangles[i]);
        }

        st.GenerateNormals();

        ArrayMesh mesh = st.Commit();
        meshInstance.Mesh = mesh;
        collisionMesh.Shape = mesh.CreateTrimeshShape();
        //GD.Print("Mesh Data Updated.");
    }
}
