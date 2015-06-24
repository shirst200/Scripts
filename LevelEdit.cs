using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class LevelEdit : MonoBehaviour {

	//The different modes the level editor can use
	public enum paintMode{
		DrawMode = 0,
		LineMode = 1,
		BoxMode = 2,
		fillMode = 3
	}

	//Acts as a cursor at mouse coordinates
	public Transform block;
	//The current block to be places
	private byte blockNum = 1;
	//The current paint mode
	public paintMode mode;
	//The previous block position for certain functions, like a line
	private Vector3 bufferPos;
	//The name of the file to be saved
	private const string FILE_NAME = "ChunkTest.chunks";
	//The GameManager script
	public GameManager gameManager;

	void Start () {
		//Get the GameManager script attached to the same gameobject
		gameManager = transform.gameObject.GetComponent<GameManager>();
		//Set the block to number 1
		blockNum = 1;
	}
	
	void Update () {
		//Example Inputs to set the block to use
		if(Input.GetKeyUp(KeyCode.Alpha1))
			blockNum = 1;
		if(Input.GetKeyUp(KeyCode.Alpha2))
			blockNum = 2;
		if(Input.GetKeyUp(KeyCode.Alpha3))
			blockNum = 3;
		if(Input.GetKeyUp(KeyCode.Alpha4))
			blockNum = 4;
		if(Input.GetKeyUp(KeyCode.Alpha5))
			blockNum = 5;
		if(Input.GetKeyUp(KeyCode.Alpha6))
			blockNum = 6;
		if(Input.GetKeyUp(KeyCode.Alpha7))
			blockNum = 7;
		if(Input.GetKeyUp(KeyCode.Alpha8))
			blockNum = 8;
		if(Input.GetKeyUp(KeyCode.Alpha9))
			blockNum = 9;
		if(Input.GetKeyUp(KeyCode.Keypad1))
			blockNum = 10;
		if(Input.GetKeyUp(KeyCode.Keypad2))
			blockNum = 11;
		if(Input.GetKeyUp(KeyCode.Keypad3))
			blockNum = 12;
		if(Input.GetKeyUp(KeyCode.Alpha0))
			blockNum = 0;

		//Inputs to change the draw mode
		if(Input.GetKeyUp(KeyCode.D))
			mode = paintMode.DrawMode;
		if(Input.GetKeyUp(KeyCode.L))
			mode = paintMode.LineMode;
		if(Input.GetKeyUp(KeyCode.B))
			mode = paintMode.BoxMode;
		if(Input.GetKeyUp(KeyCode.F))
			mode = paintMode.fillMode;

		//Loads the level
		if(Input.GetKeyUp(KeyCode.O))
			gameManager.Load();
		//Saves the level
		if(Input.GetKeyUp(KeyCode.P))
			gameManager.Save();
		
		//Possition of the cursor
		Vector2 bothPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		//Convert to block positions
		Vector3 boxPos = new Vector3(Mathf.FloorToInt(bothPos.x) + 0.5f,
		                             Mathf.FloorToInt(bothPos.y) + 0.5f, 0);
		//Move the block to the cursor
		block.transform.position = boxPos;
		
		/* Basic drawing mode
		 * Places a block each frame left click is held at the current position.
		 * In order to fix the problem of a gittery cursor at larger screen sizes
		 * producing random blocks spaces far apart even when draw is held;
		 * Lines are drawn between the current cursor position and the last
		 * cursor position where possible */

		if(mode == paintMode.DrawMode){
			if(Input.GetButton("Fire1")){
				if(bufferPos!=Vector3.zero){
					if(bufferPos!=boxPos){
						DrawLine(Mathf.RoundToInt(bufferPos.x - .5f), 
						             Mathf.RoundToInt(bufferPos.y - .5f), 
						             Mathf.RoundToInt(boxPos.x - .5f), 
						             Mathf.RoundToInt(boxPos.y - .5f));
					}
					gameManager.PlaceAndSort(boxPos, blockNum);
					bufferPos = boxPos;
				}	
				else
					bufferPos = boxPos;
			}
			if(Input.GetButtonUp("Fire1")){
				bufferPos = Vector3.zero;
			}
		}

		/* Line mode, draws line from where left click 
		 * was pressed to where left click was let go of */
	
		if(mode == paintMode.LineMode){
			if(Input.GetButtonDown("Fire1")){
				bufferPos = boxPos;
			}
			if(Input.GetButtonUp("Fire1")){
				DrawLine(Mathf.RoundToInt(bufferPos.x - .5f), 
				             Mathf.RoundToInt(bufferPos.y - .5f), 
				             Mathf.RoundToInt(boxPos.x - .5f), 
				             Mathf.RoundToInt(boxPos.y - .5f));
				gameManager.PlaceAndSort(bufferPos, blockNum);
				gameManager.PlaceAndSort(boxPos, blockNum);
				bufferPos = Vector3.zero;
			}
		}

		/* Box mode, draws box from where left click
		 * was pressed as one corner to where left
		 * click was let go of as the opposite corner */

		if(mode == paintMode.BoxMode){
			if(Input.GetButtonDown("Fire1")){
				bufferPos = boxPos;
			}
			if(Input.GetButtonUp("Fire1")){
				DrawBox(Mathf.RoundToInt(bufferPos.x - .5f), 
				           Mathf.RoundToInt(bufferPos.y - .5f), 
				           Mathf.RoundToInt(boxPos.x - .5f), 
				           Mathf.RoundToInt(boxPos.y - .5f));
				bufferPos = Vector3.zero;
			}
		}

		/* Fill Mode, fills the current block type and all of 
		 * the adjacent blocks of that type with the current block */

		if(mode == paintMode.fillMode){
			if(Input.GetButtonDown("Fire1") ){
				Fill(boxPos);
			}
		}
	}

	/* Lines are drawn in blocks from the start to
	 * end position. The following code is based on
	 * the bresenham line algorithm, converted to c#
	 * and made to work with blocks. It works by
	 * accounting for errors working in pixels, or
	 * in this case, blocks, and placing them in the
	 * most suitable location. */

	void DrawLine(int startX, int startY, int endX, int endY){
		bool looping = true;
		int dx = Mathf.Abs(endX-startX);
		int dy = Mathf.Abs(endY-startY);
		int testx = startX;
		int testy = startY;
		int sx = -1;
		int sy = -1;
		if (startX < endX)
			sx = 1 ;
		if (startY < endY)
			sy = 1 ;
		int err = dx-dy;
		// Where to place the block
		Vector3 instantPoint = new Vector3(0,0,0);
		while(looping){
			instantPoint = new Vector3(startX+.5f, startY+.5f, 0);
			// Places the block as long as it is not the start or end position
			if((startX!= testx || startY!=testy) && (startX != endX || startY != endY)){
				gameManager.PlaceAndSort(instantPoint, blockNum);
			}
			// Loops until a block is placed at the end point
			if((startX == endX) && (startY == endY)){
				looping = false;	
			}
			// Finds the next position to place the block in the line
			int e2 = 2*err;
			if (e2 > -dy){
				err = err - dy;
				startX = startX + sx;
			}
			if (e2 <  dx){ 
				err = err + dx;
				startY = startY + sy;
			}
		}
	}

	/* Boxes are drawn by placing blocks vertically
	 * and horizontally from the start point to the end
	 * point. As parallel lines are very similar only 2 
	 * of the sides needs to be tracked and then slightly
	 * edited to work for the parallel lines. dx is
	 * horizontal, dy is vertical. */

	void DrawBox(int startX, int startY, int endX, int endY){
		int dx = Mathf.Abs(endX-startX);
		int dy = Mathf.Abs(endY-startY);
		int sx = -1;
		int sy = -1;
		if (startX < endX)
			sx = 1 ;
		if (startY < endY)
			sy = 1 ;
		// 2 instantiation points for parallel lines
		Vector3 upperInstantPoint = new Vector3(0,0,0);
		Vector3 lowerInstantPoint = new Vector3(0,0,0);
		// Places blocks in the x axis
		for(int i = 0; i<= dx; i++){
			upperInstantPoint = new Vector3(startX + (sx*i) +.5f, startY+.5f, 0);
			lowerInstantPoint = new Vector3(startX + (sx*i)+.5f, endY+.5f, 0);
			gameManager.PlaceAndSort(upperInstantPoint, blockNum);
			gameManager.PlaceAndSort(lowerInstantPoint, blockNum);
		}
		// Places blocks in the y axis
		for(int i = 1; i< dy; i++){
			upperInstantPoint = new Vector3(startX+.5f, startY + (sy*i)+.5f, 0);
			lowerInstantPoint = new Vector3(endX+.5f, startY + (sy*i)+.5f, 0);
			gameManager.PlaceAndSort(upperInstantPoint, blockNum);
			gameManager.PlaceAndSort(lowerInstantPoint, blockNum);
		}
	}

	/* Fill works by finding the block at the target
	 * position. With that block as the target type to
	 * fill, the function then looks at the blocks adjacent
	 * to it, and if they are of the fill type adds them to
	 * the loop to be checked, as long as they have not
	 * already been checked before. When blocks of the correct
	 * type are found, they are replaced by the current block
	 * type and set as already processed in an array. */

	void Fill(Vector3 pos){
		int i = 0;
		// The following variable is used to make sure the correct layer is checked
		int layerCorrection = gameManager.blocks[blockNum].layer * (gameManager.width * gameManager.height);
		// When a block is processed it is added to the array at it's chunk save position
		bool[] blockProcess = new bool[gameManager.width*gameManager.height*gameManager.layers.Count];
		// The position of the block to be filled in the array
		int fillNum = (int)(pos.y)*gameManager.width+(int)pos.x + layerCorrection;
		// The target block type to be filled
		int targetType = gameManager.chunkSave[fillNum];
		// The list of the block positions to be checked
		List<int> checks = new List<int>();
		checks.Add(fillNum);
		// Loops while there are blocks in the array
		while(checks.Count>i){
			// Converts the position in the array to a Vector 3
			Vector3 newPos = new Vector3((checks[i]-layerCorrection)%gameManager.width + .5f, (int)((checks[i]-layerCorrection)/gameManager.width) + .5f, 0);
			gameManager.PlaceAndSort(newPos, blockNum);
			// Checks the block above
			if(newPos.y < gameManager.height -0.5f && gameManager.chunkSave[checks[i] + gameManager.width] == targetType && blockProcess[checks[i] + gameManager.width] != true){
				checks.Add(checks[i] + gameManager.width);
				blockProcess[checks[i] + gameManager.width] = true;
			}
			// Checks the block below
			if(newPos.y > 0.5f && gameManager.chunkSave[checks[i] - gameManager.width] == targetType && blockProcess[checks[i] - gameManager.width] != true){
				checks.Add(checks[i] - gameManager.width);
				blockProcess[checks[i] - gameManager.width] = true;
			}
			// Checks the block to the left
			if(newPos.x > 0.5f && gameManager.chunkSave[checks[i] - 1] == targetType && blockProcess[checks[i] - 1] != true){
				checks.Add(checks[i] - 1);
				blockProcess[checks[i] - 1] = true;
			}
			// Checks the block to the right
			if(newPos.x < gameManager.width -0.5f && gameManager.chunkSave[checks[i] + 1] == targetType && blockProcess[checks[i] + 1] != true){
				checks.Add(checks[i] + 1);
				blockProcess[checks[i] + 1] = true;
			}
			// Increments the count
			i ++;
		}
	}
}