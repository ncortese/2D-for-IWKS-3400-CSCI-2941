using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerMovement : MonoBehaviour {

    private enum Direction { north = 0, south = 1, east = 2, west = 3 }

    // Public values can be adjusted in unity
    public int speed = 1;
    public int movementCooldown = 3;
    public bool allowHoldToMove = true;
	public int maxHealth = 10;
	public float xpGrowth = 0.15f;
	public float xpToLevel = 10;

    // Private values for behind the scenes work
    private int movementTimer = 0;
	private int health;
	private float xp = 0;
	private int level = 1;
    private Direction direction;

	// Use this for initialization
	void Start () {
		health = maxHealth;
	}
	
	// Update is called once per frame
	void Update () {
        if (allowHoldToMove) {
            ControlPlayerHold();
        }
        else {
            ControlPlayerTap();
        }
		XPController();
        UpdateTimer();
	}

    void ControlPlayerHold() {
        // Used for moving the player
        Vector3 movePlayer = Vector3.zero;

        if (Input.GetKey(KeyCode.LeftArrow) && movementTimer == 0) {
            direction = Direction.west;
            movePlayer.x = -speed;
            movementTimer += movementCooldown;
        }
        if (Input.GetKey(KeyCode.RightArrow) && movementTimer == 0) {
            direction = Direction.east;
            movePlayer.x = speed;
            movementTimer += movementCooldown;
        }
        if (Input.GetKey(KeyCode.UpArrow) && movementTimer == 0) {
            direction = Direction.north;
            movePlayer.y = speed;
            movementTimer += movementCooldown;
        }
        if (Input.GetKey(KeyCode.DownArrow) && movementTimer == 0) {
            direction = Direction.south;
            movePlayer.y = -speed;
            movementTimer += movementCooldown;
        }

        transform.position += movePlayer;
    }

    void ControlPlayerTap() {
        // Used for moving the player
        Vector3 movePlayer = Vector3.zero;

        if (Input.GetKeyDown(KeyCode.LeftArrow) && movementTimer == 0) {
            direction = Direction.west;
            movePlayer.x = -speed;
            movementTimer += movementCooldown;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow) && movementTimer == 0) {
            direction = Direction.east;
            movePlayer.x = speed;
            movementTimer += movementCooldown;
        }
        if (Input.GetKeyDown(KeyCode.UpArrow) && movementTimer == 0) {
            direction = Direction.north;
            movePlayer.y = speed;
            movementTimer += movementCooldown;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow) && movementTimer == 0) {
            direction = Direction.south;
            movePlayer.y = -speed;
            movementTimer += movementCooldown;
        }

        transform.position += movePlayer;
    }

    void UpdateTimer() {
        if (movementTimer > 0) {
            movementTimer--;
        }
    }

	void XPController() {
		if (xp == xpToLevel) {
			xp = 0;
			xpToLevel *= xpGrowth;
			level++;
			maxHealth += 5;
			health = maxHealth;
		}
		else if (xp > xpToLevel) {
			xp -= xpToLevel;
			level++;
			maxHealth += 5;
			health = maxHealth;
		}
	}
}
