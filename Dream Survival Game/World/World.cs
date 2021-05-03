using Godot;
using System;
using System.Collections.Generic;

public class World : Spatial
{
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";

	public static World _instance = null;

	Dictionary<Vector3, Chunk> allChunks = new System.Collections.Generic.Dictionary<Vector3, Chunk>();

	public Node ChunkHolderNode;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (_instance == null)
			_instance = this;

		ChunkHolderNode = this;

		//System.Threading.Tasks.Task worldGenTask = new System.Threading.Tasks.Task(GenerateChunks);
		//worldGenTask.Start();

		//System.Threading.Thread myThread = new System.Threading.Thread(GenerateChunks);
		//myThread.Start();

		GenerateChunks();
	}

	public void GenerateChunks()
	{
		// TODO: Add threading for generating the world asynchrously
		for (int x = 0; x < GameData.WorldSizeInChunks; x++)
		{
			for (int y = 0; y < GameData.WorldSizeInChunks; y++)
			{
				for (int z = 0; z < GameData.WorldSizeInChunks; z++)
				{
					Vector3 chunkPos = new Vector3(x * GameData.ChunkWidth, y * GameData.ChunkHeight, z * GameData.ChunkWidth);
					allChunks.Add(chunkPos, new Chunk(chunkPos));
					AddChild(allChunks[chunkPos].ChunkNode);
				}
			}
		}
	}

	public Chunk GetChunkFromVector3(Vector3 i_position)
	{
		int x = Mathf.FloorToInt(i_position.x);
		int y = Mathf.FloorToInt(i_position.y);
		int z = Mathf.FloorToInt(i_position.z);
		return allChunks[new Vector3(x, y, z)];
	}



//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
