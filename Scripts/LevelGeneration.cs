using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGeneration : MonoBehaviour {
    // How far down player has gone in the dungeon
    public int dungeonDepth = 1;

    // game objects needed by the level generator. Can add more as needed
    public GameObject player;
    public GameObject wall;
    public GameObject[] monsters;

    // Dimensions of the rectangular blocks of map in which generator places rooms
    public int cell_width = 16;
    public int cell_height = 16;
    // minimum dimensions for each room
    public int min_width = 5;
    public int min_height = 5;
    // length of random walk (could modify this to walk longer if not enough rooms have been generated)
    public int walk_length = 15;
    // max number of monsters per room
    public int maxNumMonsters = 10;
    // parameter to control probability distribution of number of monsters placed in each room
    public float monsterPlacementStopProbability = 0.4f;

    // could also make generate level public and have, say, the player object call it + set level generator depth at the same time
	void Start () {
        GenerateLevel();
	}

    /*
    This code is pretty messy and inefficient. I'll try to clean it up a bit as I improve it.
    */
	void GenerateLevel()
    {
        Dictionary<IntVec2D, Cell> level_graph = new Dictionary<IntVec2D, Cell>();
        // initialize level graph with a cell at (0, 0)
        level_graph.Add(new IntVec2D(), new Cell());
        // set initial position of random walk to (0, 0)
        IntVec2D currentPos = new IntVec2D();
        IntVec2D lastPos = new IntVec2D();

        // Do random walk
        for (int i = 0; i < walk_length; ++i)
        {
            int direction = (int)(4*Random.value);
            // walk left
            if (direction == 0)
            {
                currentPos += new IntVec2D(-1, 0);
            }
            // walk up
            else if (direction == 1)
            {
                currentPos += new IntVec2D(0, 1);
            }
            // walk right
            else if (direction == 2)
            {
                currentPos += new IntVec2D(1, 0);
            }
            // walk down
            else
            {
                currentPos += new IntVec2D(0, -1);
            }

            // add this position to level graph if it isn't already there
            if (!level_graph.ContainsKey(currentPos))
            {
                level_graph.Add(currentPos, new Cell(currentPos));
            }

            // connect last position to new position in move direction
            level_graph[lastPos].adjacent[direction] = currentPos;
            // connect new position to last position in opposite of move direction
            level_graph[currentPos].adjacent[(direction + 2) % 4] = lastPos;

            lastPos = currentPos;
        }

        /*
        now level graph has been built, create rooms that fit inside each cell. Cells represent blocks of size
        cell_width x cell_height. Their adjacent cells are the rooms that are connected to their room by corridors
        I assume that the center of each grid cell lies on the unity grid, with each tile being a square with each
        side 1 unity unit long
        I also keep at least three tiles in between each room to ensure I can put in S-shaped corridors
        */
        Dictionary<IntVec2D, Room> rooms = new Dictionary<IntVec2D, Room>();
        Dictionary<IntVec2D, PrefabRoom> prefabs = new Dictionary<IntVec2D, PrefabRoom>();

        foreach (KeyValuePair<IntVec2D, Cell> cell in level_graph)
        {
            int cellWorldX = cell.Key.x * cell_width;
            int cellWorldY = cell.Key.y * cell_height;
            int bottomLeftCornerX = (int)(cellWorldX - 0.5f * cell_width + 2 + (cell_width - min_width - 2)*Random.value);
            int bottomLeftCornerY = (int)(cellWorldY - 0.5f * cell_height + 2 + (cell_height - min_height - 2) * Random.value);
            int topRightCornerX = (int)(bottomLeftCornerX + min_width + (0.5f*cell_width + cellWorldX - bottomLeftCornerX - min_width - 2) * Random.value);
            int topRightCornerY = (int)(bottomLeftCornerY + min_height + (0.5f * cell_height + cellWorldY - bottomLeftCornerY - min_height - 2) * Random.value);
            rooms[cell.Key] = new Room(new IntVec2D(bottomLeftCornerX, bottomLeftCornerY), new IntVec2D(topRightCornerX, topRightCornerY), this);
        }

        // place corridors between rooms, setting room exits to match
        // This part of the code is especially hideous
        foreach (KeyValuePair<IntVec2D, Room> room in rooms)
        {
            Room thisRoom = room.Value;
            IntVec2D leftConnected = level_graph[room.Key].adjacent[0];
            IntVec2D upConnected = level_graph[room.Key].adjacent[1];
            // build left corridor if connected
            if (!leftConnected.Equals(room.Key))
            {
                Room otherRoom = rooms[leftConnected];
                // check if they have wall tiles directly across from each other; if so, connect them with a straight corridor
                // Don't permit corner tiles (uglier)
                if (thisRoom.topRightCorner.y > otherRoom.bottomLeftCorner.y + 1 && thisRoom.bottomLeftCorner.y < otherRoom.topRightCorner.y - 1)
                {
                    int intersectTop = Mathf.Min(thisRoom.topRightCorner.y - 1, otherRoom.topRightCorner.y - 1);
                    int intersectBottom = Mathf.Max(thisRoom.bottomLeftCorner.y + 1, otherRoom.bottomLeftCorner.y + 1);
                    int corridorY = (int)((intersectTop - intersectBottom) * Random.value + intersectBottom);
                    thisRoom.exits.Add(new IntVec2D(thisRoom.bottomLeftCorner.x, corridorY));
                    otherRoom.exits.Add(new IntVec2D(otherRoom.topRightCorner.x, corridorY));
                    BuildCorridor(thisRoom.bottomLeftCorner.x, otherRoom.topRightCorner.x, corridorY, corridorY, false);
                }
                // else, connect two random positions in each wall using an S-shaped corridor
                else
                {
                    int thisRoomCorridorY = (int)((thisRoom.topRightCorner.y - thisRoom.bottomLeftCorner.y - 2) * Random.value + thisRoom.bottomLeftCorner.y + 1);
                    int otherRoomCorridorY = (int)((otherRoom.topRightCorner.y - otherRoom.bottomLeftCorner.y - 2) * Random.value + otherRoom.bottomLeftCorner.y + 1);
                    thisRoom.exits.Add(new IntVec2D(thisRoom.bottomLeftCorner.x, thisRoomCorridorY));
                    otherRoom.exits.Add(new IntVec2D(otherRoom.topRightCorner.x, otherRoomCorridorY));
                    BuildCorridor(thisRoom.bottomLeftCorner.x, otherRoom.topRightCorner.x, thisRoomCorridorY, otherRoomCorridorY, false);
                }
            }
            // build up corridor if connected
            if (!upConnected.Equals(room.Key))
            {
                Room otherRoom = rooms[upConnected];
                if (thisRoom.topRightCorner.x > otherRoom.bottomLeftCorner.x + 1 && thisRoom.bottomLeftCorner.x < otherRoom.topRightCorner.x - 1)
                {
                    int intersectRight = Mathf.Min(thisRoom.topRightCorner.x - 1, otherRoom.topRightCorner.x - 1);
                    int intersectLeft = Mathf.Max(thisRoom.bottomLeftCorner.x + 1, otherRoom.bottomLeftCorner.x + 1);
                    int corridorX = (int)((intersectRight - intersectLeft) * Random.value + intersectLeft);
                    thisRoom.exits.Add(new IntVec2D(corridorX, thisRoom.topRightCorner.y));
                    otherRoom.exits.Add(new IntVec2D(corridorX, otherRoom.bottomLeftCorner.y));
                    BuildCorridor(corridorX, corridorX, thisRoom.topRightCorner.y, otherRoom.bottomLeftCorner.y, true);
                }
                else
                {
                    int thisRoomCorridorX = (int)((thisRoom.topRightCorner.x - thisRoom.bottomLeftCorner.x - 2) * Random.value + thisRoom.bottomLeftCorner.x + 1);
                    int otherRoomCorridorX = (int)((otherRoom.topRightCorner.x - otherRoom.bottomLeftCorner.x - 2) * Random.value + otherRoom.bottomLeftCorner.x + 1);
                    thisRoom.exits.Add(new IntVec2D(thisRoomCorridorX, thisRoom.topRightCorner.y));
                    otherRoom.exits.Add(new IntVec2D(otherRoomCorridorX, otherRoom.bottomLeftCorner.y));
                    BuildCorridor(thisRoomCorridorX, otherRoomCorridorX, thisRoom.topRightCorner.y, otherRoom.bottomLeftCorner.y, true);

                }
            }
        }

        // populate rooms
        bool havePlacedPlayer = false;
        float roomsRemaining = rooms.Count;
        foreach (KeyValuePair<IntVec2D, Room> room in rooms)
        {
            // place room walls
            Room thisRoom = room.Value;
            thisRoom.BuildRectangularRoom();

            // construct list of free spaces for placing objects
            // this is very inefficient, but easier to implement than alternatives. I should think of a better way
            int area = (thisRoom.topRightCorner.x - thisRoom.bottomLeftCorner.x - 2) * (thisRoom.topRightCorner.y - thisRoom.bottomLeftCorner.y - 2);
            List<Vector2> freeSpaces = new List<Vector2>(area);
            for (int x = thisRoom.bottomLeftCorner.x + 1; x < thisRoom.topRightCorner.x; ++x)
            {
                for (int y = thisRoom.bottomLeftCorner.y + 1; y < thisRoom.topRightCorner.y; ++y)
                {
                    freeSpaces.Add(new Vector2(x, y));
                }
            }

            // place player at a random position in a random room
            if (Random.value <= 1/roomsRemaining && !havePlacedPlayer)
            {
                int randPositionIndex = (int)(freeSpaces.Count * Random.value);
                Instantiate(player, freeSpaces[randPositionIndex], Quaternion.identity);
                freeSpaces.RemoveAt(randPositionIndex);
                havePlacedPlayer = true;
            }

            SpawnMonsters(ref freeSpaces);

            --roomsRemaining;
        }
    }

    /* Placeholder monster spawn code.
    Monsters selected randomly from monster list.
    Number of monsters spawned follows a geometric distribution, so there's a high probability of at least
    one monster but a low probability of many.
    Also stops spawning if runs out of free spaces or hits max monster count.
    */
    private void SpawnMonsters(ref List<Vector2> freeSpaces)
    {
        for (int i = 0; i < maxNumMonsters; ++i)
        {
            // stop spawning with a certain probability
            if ((int)(Random.value / monsterPlacementStopProbability) == 0)
            {
                break;
            }
            else if (freeSpaces.Count > 0)
            {
                // place a random monster
                int monsterIndex = (int)(monsters.Length * Random.value);
                int randPositionIndex = (int)(freeSpaces.Count * Random.value);
                Instantiate(monsters[monsterIndex], freeSpaces[randPositionIndex], Quaternion.identity);
                freeSpaces.RemoveAt(randPositionIndex);
            }
        }
    }

    // utility class used to store temporary information for level graph
    private class Cell
    {
        // list of neighboring rooms
        public IntVec2D[] adjacent;
        
        public Cell(IntVec2D pos)
        {
            adjacent = new IntVec2D[] { pos, pos, pos, pos };
        }

        public Cell()
        {
            IntVec2D pos = new IntVec2D();
            pos.x = 0;
            pos.y = 0;
            adjacent = new IntVec2D[] { pos, pos, pos, pos };
        }
    }

    // private class used to store temporary room information for random rooms. Could expand this later for more interesting rooms
    private class Room
    {
        public IntVec2D bottomLeftCorner;
        public IntVec2D topRightCorner;
        public List<IntVec2D> exits;
        public LevelGeneration parentGenerator;

        public Room(IntVec2D bottomLeftCorner, IntVec2D topRightCorner, LevelGeneration parentGenerator)
        {
            this.bottomLeftCorner = bottomLeftCorner;
            this.topRightCorner = topRightCorner;
            this.parentGenerator = parentGenerator;
            this.exits = new List<IntVec2D>();
        }

        public void BuildRectangularRoom()
        {
            // create top and bottom horizontal walls
            for (int x = bottomLeftCorner.x; x <= topRightCorner.x; ++x)
            {
                if (!exits.Contains(new IntVec2D(x, bottomLeftCorner.y)))
                {
                    parentGenerator.CreateWall(x, bottomLeftCorner.y);
                }
                if (!exits.Contains(new IntVec2D(x, topRightCorner.y)))
                {
                    parentGenerator.CreateWall(x, topRightCorner.y);
                }
            }
            // create left and right vertical walls (skipping top and bottom to avoid creating corners twice)
            for (int y = bottomLeftCorner.y+1; y < topRightCorner.y; ++y)
            {
                if (!exits.Contains(new IntVec2D(bottomLeftCorner.x, y)))
                {
                    parentGenerator.CreateWall(bottomLeftCorner.x, y);
                }
                if (!exits.Contains(new IntVec2D(topRightCorner.x, y)))
                {
                    parentGenerator.CreateWall(topRightCorner.x, y);
                }
            }
        }
    }

    // isOrientedUp = false: corridor going left. isOrientedUp = true: corridor going up.
    private void BuildStraightCorridor(int x1, int x2, int y1, int y2, bool isOrientedUp)
    {
        if (!isOrientedUp)
        {
            int d_x = (int) Mathf.Sign(x1 - x2);
            for (int x = x2+d_x; d_x*(x1 - x) > 0; x+=d_x)
            {
                CreateWall(x, y1 + 1);
                CreateWall(x, y1 - 1);
            }
        }
        else
        {
            int d_y = (int)Mathf.Sign(y2 - y1);
            for (int y = y1 + d_y; d_y * (y2 - y) > 0; y += d_y)
            {
                CreateWall(x1+1, y);
                CreateWall(x1-1, y);
            }
        }
    }

    // a bit messier than it needs to be
    private void BuildCorridor(int x1, int x2, int y1, int y2, bool isOrientedUp)
    {
        // if not straight, build an S-corridor
        if (x1 != x2 && y1 != y2)
        {
            // build left
            if (!isOrientedUp)
            {
                int turnX = (int)(x2 + 2 + (x1 - x2 - 3) * Random.value);
                int d_y = (int)Mathf.Sign(y2 - y1);
                BuildStraightCorridor(x1, turnX, y1, y1, false);

                // 1st corner
                CreateWall(turnX, y1 - d_y);
                CreateWall(turnX - 1, y1 - d_y);
                CreateWall(turnX - 1, y1);

                // 1st turn
                BuildStraightCorridor(turnX, turnX, y1, y2, true);

                // 2nd corner
                CreateWall(turnX+1, y2);
                CreateWall(turnX+1, y2 + d_y);
                CreateWall(turnX, y2 + d_y);

                // 2nd turn
                BuildStraightCorridor(turnX, x2, y2, y2, false);
            }
            // build up
            else
            {
                int turnY = (int)(y1 + 2 + (y2 - y1 - 3) * Random.value);
                int d_x = (int)Mathf.Sign(x2 - x1);

                BuildStraightCorridor(x1, x1, y1, turnY, true);

                // 1st corner
                CreateWall(x1 - d_x, turnY);
                CreateWall(x1 - d_x, turnY + 1);
                CreateWall(x1, turnY + 1);

                // 1st turn
                BuildStraightCorridor(x1, x2, turnY, turnY, false);

                // 2nd corner
                CreateWall(x2, turnY - 1);
                CreateWall(x2 + d_x, turnY - 1);
                CreateWall(x2 + d_x, turnY);

                // 2nd turn
                BuildStraightCorridor(x2, x2, turnY, y2, true);
            }
        }
        else
        {
            BuildStraightCorridor(x1, x2, y1, y2, isOrientedUp);
        }
    }

    public void CreateWall(int x, int y)
    {
        Instantiate(wall, new Vector2(x, y), Quaternion.identity);
    }
}

