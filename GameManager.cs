using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

//The layer class
[System.Serializable]
public class Layers{
	//Layer name
	public string name;
	//Array of layers rows
	public GameObject[][] rows;
	//Object that contains all layers blocks
	[System.NonSerialized]
	public GameObject container;
}

//The block class
[System.Serializable]
public class Blocks{
	//Block name
	public string name;
	//The layer it should be placed on
	public int layer;
	//The texture coordinate within the texture atlas
	public Vector2 textureCoord;
	//Whether or not the block has collisions
	public bool collidable;
	//The mesh to use to rended the graphics
	[System.NonSerialized]
	public Mesh bMesh;
}

public class GameManager : MonoBehaviour {

	//Width and height of the map
	public int width;
	public int height;
	//Size of the texture atlas in pixels
	public Vector2 atlasSize;
	//How many pixels each texture takes up
	public int pixelPerTexture;
	//List of all layers, editable in inspector to add new ones
	public List<Layers> layers;
	//List of all blocks, editable in inspector to add new ones
	public List<Blocks> blocks;
	//Background image for map
	public Transform background;
	//The grid for use in level editing
	public Transform grid;
	//The array of bytes for saving, loading and collison detection
	public byte[] chunkSave;
	//The atlas material
	public Material AtlasMat;
	//The name of the file when saved
	private const string FILE_NAME = "ChunkTest.chunks";
	//The layer currently in use for when blocks are spawned
	public int currentLayer;
	//The robot spawn positions
	public Vector3[] botsPos;

	void Start () {
		//Set up the robot spawn array
		botsPos = new Vector3[4];
		//Set up the chunk save array size
		chunkSave =  new byte[width*height*layers.Count];
		//Scale and position the camera to the desired size of the map
		transform.camera.orthographicSize = (float)height/2;
		transform.position = new Vector3((float)width/2, (float)height/2, -1f);
		//Create background image and scale
		background.position = new Vector3((float)width/2, (float)height/2, 10f);
		background.localScale = new Vector3(width, height, 1);
		//Create Grid, scale object and scale material
		grid.position = new Vector3((float)width/2, (float)height/2, -1f);
		grid.localScale = new Vector3(width, height, 1);
		grid.renderer.material.mainTextureScale = new Vector2(width,height);
		//Create mesh for each block type
		for(int l = 0; l < blocks.Count; l++){
			Mesh m = new Mesh();
			m.name = "ScriptedMesh" + l.ToString();
			//Set up the vertices
			m.vertices = new Vector3[] {
				new Vector3(-0.5f, -0.5f, 0.0f),
				new Vector3(0.5f, -0.5f, 0.0f),
				new Vector3(0.5f, 0.5f, 0.0f),
				new Vector3(-0.5f, 0.5f, 0.0f)
			};
			//Bottom left and top right need half pixel correction to avoid texture blurring
			Vector2 bottomLeft = UVCreator(blocks[l].textureCoord.x, blocks[l].textureCoord.y, +1);
			Vector2 topRight = UVCreator(blocks[l].textureCoord.x +1, blocks[l].textureCoord.y +1, -1);
			m.uv = new Vector2[] {
				bottomLeft,
				new Vector2(topRight.x, bottomLeft.y),
				topRight,
				new Vector2(bottomLeft.x, topRight.y),
			};
			m.triangles = new int[] {0, 1, 2, 0, 2, 3};
			m.RecalculateNormals();
			//Store the mesh for future blocks of this type
			blocks[l].bMesh = m;
		}
		//Create layer and slots in container
		for(int k = 0; k < layers.Count; k++){
			layers[k].container = new GameObject(layers[k].name);
			layers[k].rows = new GameObject[height][];
			//Create each row and parent it to layer
			for(int i = 0; i < height; i++){
				GameObject newRow = new GameObject("Row"+i.ToString());
				newRow.transform.parent = layers[k].container.transform;
				//Create each collumn and parent it to row
				GameObject[] collums = new GameObject[width];
				for(int j = 0; j < width; j++){
					collums[j] = new GameObject("Collumn"+j.ToString());
					collums[j].transform.parent = newRow.transform;
				}
				//Store the collumn
				layers[k].rows[i] = collums;
			}
		}
	}

	//Creates the Uvs for the tile mesh's
	private Vector2 UVCreator(float x, float y, int dir){
		float nx = x*pixelPerTexture;
		float ny = y*pixelPerTexture;
		//Uses half a pixel correction to fix texture bleeding
		Vector2 uvs = new Vector2((nx+(0.5f*dir))/atlasSize.x, (ny+(0.5f*dir))/atlasSize.y);
		return uvs;
	}

	//Creates the gameobject for the blocks graphics
	public GameObject CreateTile(int type, Vector3 pos){
		//Create the object and fix position scale and rotation
		GameObject newTile = new GameObject();
		newTile.transform.position = pos;
		newTile.transform.rotation = Quaternion.Euler(180,0,180);
		newTile.transform.localScale = new Vector3(-1, 1, 1);
		//Add a mesh filter and renderer
		newTile.AddComponent<MeshFilter>();
		MeshRenderer rend = newTile.AddComponent<MeshRenderer>();
		//Set off some unneeded extras
		rend.castShadows = false; 
		rend.receiveShadows = false;
		//Add the atlas mat to the renderer
		newTile.renderer.material = AtlasMat;
		//Add the blocks specific mesh to object
		newTile.GetComponent<MeshFilter>().mesh = blocks[type].bMesh;
		//Return the created tile
		return newTile;
	}

	//Places blocks in the right places and stores them
	public void PlaceAndSort(Vector3 pos, byte type){
		if(pos.y+.5f <= height && pos.y+.5f >= 1f && pos.x+.5f <= width && pos.x+.5f >= 1f){
			//Sets the layer to the correct one for the block if not air
			if(type!=0){
				currentLayer = blocks[type].layer;
			}
			//Set the new value in the array
			chunkSave[(int)(pos.y)*width+(int)pos.x + (width*height*currentLayer)] = type;
			//Works out the positon to place the block
			pos = new Vector3(pos.x, pos.y, layers.Count-currentLayer);
			//The new block gameobject
			GameObject tempCube;
			//If not air, create the tile
			if(type!=0)
				tempCube = CreateTile(type, pos);
			//If it is air, create an empty game object
			else{
				tempCube = new GameObject();
				tempCube.transform.position = pos;
			}
			//Find the old object in the targeted position
			GameObject oldPlace = layers[currentLayer].rows[((int)(pos.y+.5f)-1)][((int)(pos.x+.5f)-1)];
			//Set the new blocks parent to that of the old block
			tempCube.transform.parent = oldPlace.transform.parent;
			//Set the new blocks name to the old name
			tempCube.name = oldPlace.name;
			//Store the object in the arrat
			layers[currentLayer].rows[((int)(pos.y+.5f)-1)][((int)(pos.x+.5f)-1)] = tempCube;
			//Destroy the old block
			Destroy(oldPlace);
			//Sets the spawn points for the bots
			if(type == 1)
				botsPos[0] = pos;
			if(type == 7)
				botsPos[1] = pos;
			if(type == 10)
				botsPos[2] = pos;
			if(type == 5)
				botsPos[3] = pos;
		}
	}

	//Saves the level to new file
	public void Save(){
		//Deletes file if it already exists
		if (File.Exists(FILE_NAME)) {
			File.Delete(FILE_NAME);
		}
		//Creates a new file
		FileStream fs = new FileStream(FILE_NAME, FileMode.CreateNew);
		BinaryWriter w = new BinaryWriter(fs);
		//Write each byte in the chunkSave array to the new file
		for (int i = 0; i < chunkSave.Length; i++) {
			w.Write(chunkSave[i]);
		}
		//Close the file
		w.Close();
		fs.Close();
	}

	//Loads the level from the file
	public void Load(){	
		//Finds the file
		FileStream fs = new FileStream(FILE_NAME, FileMode.Open, FileAccess.Read);
		BinaryReader r = new BinaryReader(fs);
		//Checks for every place in the chunkSave array
		for (int i = 0; i < chunkSave.Length; i++) {
			//Checks which level to place the blocks on
			currentLayer = i/(width*height);
			//Works out the Vector3 position from the array position
			Vector3 cPos = new Vector3((int)(i%width)+.5f,(int)((i/width)%height)+.5f,0);
			//Places the read block
			PlaceAndSort(cPos, r.ReadByte());
		}
		//Close the file
		r.Close();
		fs.Close();
	}
}