using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class FishSpawn : MonoBehaviour
{
    [SerializeField]private GameObject fishGameObject;
    private bool touchedPlayer = false;
    private float biteSize;
    [Range(1,20)][SerializeField] int numberOfFishesAtTime = 3;
    private const float fishStartPositionY = -4.76f;
    [Range(1f, 20f)] [SerializeField] public float leftMinDistance = 11f;
    [Range(1f, 20f)] [SerializeField] public float rightMinDistance = 11f;
    [Range(1f, 20f)] [SerializeField] public float leftSpawnRange = 10f;
    [Range(1f, 20f)] [SerializeField] public float rightSpawnRange = 10f;
    [Range(1f, 20f)] [SerializeField] public float spawnCooldown = 1f;

    [Range(0.1f, 5f)] [SerializeField] private float minFishScale = 0.5f;
    [Range(0.1f, 5f)] [SerializeField] private float maxFishScale = 2f;

    public int numberOfActiveFishes = 0;
    private float[] spawnCooldownTracker;

    private void Awake()
    {
        biteSize = fishGameObject.GetComponent<FishMovement>().biteSize;
        
        // spawn cooldown array
        spawnCooldownTracker = new float[numberOfFishesAtTime];
        for (int i = 0; i < numberOfFishesAtTime; i++)
        {
            spawnCooldownTracker[i] = 0f;
        }
    }

    void Update()
    {
        // Check if we need to spawn
        // If we need to spawn
        if (numberOfActiveFishes < numberOfFishesAtTime && touchedPlayer)
        {
            int freeSlotIndex = GetFreeSpawnSlot(Time.time);
            // Only spawn if we no slots are on cooldown
            if (freeSlotIndex != -1)
            {
                ApplyCooldownOnSlot(freeSlotIndex, Time.time);
                
                // Check if we can spawn (either left or right are free or both)
                bool canSpawnLeft = CanSpawn(true);
                bool canSpawnRight = CanSpawn(false);

                // If not then we ignore
                if (canSpawnLeft || canSpawnRight)
                {
                    // If we can - if both available then randomise side, if only one pick one
                    bool spawnLeft = SpawnLeftSide(canSpawnLeft, canSpawnRight);
                    // Calculate spawn location  = minDistance + random value in range
                    Vector2 spawnLocation = GetSpawnLocation(spawnLeft);
                    // Spawn and increase the no of active fishes accordingly
                    fishGameObject.GetComponent<FishMovement>().fishOnRightOfBoat = !spawnLeft;
                    fishGameObject.transform.position = spawnLocation;
                    ApplySizeChanger();
                    Instantiate(fishGameObject);
                    numberOfActiveFishes++;
                }
            }
        }
    }

    private int GetFreeSpawnSlot(float time)
    {
        for (int i = 0; i < spawnCooldownTracker.Length; i++)
        {
            if (spawnCooldownTracker[i] < time) return i;
        }
        
        return -1;
    }

    private void ApplySizeChanger()
    {
        // Randomise a value in the min-max range
        float scale = Random.Range(minFishScale, maxFishScale);

        // Assign the fish scale
        fishGameObject.transform.localScale = new Vector3(scale, scale);

        // Update bitesize
        biteSize = scale;
    }

    private void ApplyCooldownOnSlot(int freeSlotIndex, float time)
    {
        spawnCooldownTracker[freeSlotIndex] = time + spawnCooldown;
    }

    private Vector2 GetBoatCenterPosition()
    {
        BoxCollider2D boatBoxCollider2D = gameObject.GetComponent<BoxCollider2D>();
        return boatBoxCollider2D.bounds.center;
    }

    private bool CanSpawn(bool left)
    {
        RaycastHit2D raycastHit2D;
        Vector2 boatCenterPosition = GetBoatCenterPosition();
        if (left)
        {
            raycastHit2D =
                Physics2D.Raycast(boatCenterPosition, Vector2.left, leftMinDistance + leftSpawnRange, 1 << LayerMask.NameToLayer("Default"));
        }
        else
        {
            raycastHit2D =
                Physics2D.Raycast(boatCenterPosition, Vector2.right, rightMinDistance + rightSpawnRange, 1 << LayerMask.NameToLayer("Default"));
        }
        return raycastHit2D.collider == null;
    }

    private bool SpawnLeftSide(bool canSpawnLeft, bool canSpawnRight)
    {
        // Just left
        if (canSpawnLeft && !canSpawnRight)
        {
            return true;
        }
        // Just right
        else if (!canSpawnLeft && canSpawnRight)
        {
            return false;
        }
        // If both
        else
        {
            // Randomise 
            return Random.Range(0,2) == 0;
        }
    }

    private Vector2 GetSpawnLocation(bool spawnLeft)
    {
        float bitingPointX = FindNearestBitingPoint(spawnLeft).x;

        float fishStartPositionX;
        if (spawnLeft)
        {
            fishStartPositionX = bitingPointX - leftMinDistance;
            float posXInRange = Random.Range(0, leftSpawnRange);
            fishStartPositionX -= posXInRange;
        }
        else
        {
            fishStartPositionX = bitingPointX + rightMinDistance;
            float posXInRange = Random.Range(0, rightSpawnRange);
            fishStartPositionX += posXInRange;
        }

        return new Vector2(fishStartPositionX, fishStartPositionY);
    }
    
    private Vector2 FindNearestBitingPoint(bool spawnLeft)
    {
        Vector2 boatCenterPosition = GetBoatCenterPosition();
        
        // Find size
        float boatScale = gameObject.transform.localScale.x;
        // Find collider size
        float colliderSize = gameObject.GetComponent<BoxCollider2D>().size.x;
        
        float boatSize = colliderSize * boatScale;

        // Half size as we need one side
        float halfSize = boatSize / 2;
        
        // Center + half - bitesize/2 to find center of biting point
        float bitingPointXCenter;
        if (!spawnLeft)
        {
            bitingPointXCenter = boatCenterPosition.x + halfSize - biteSize/2;
        }
        else
        {
            bitingPointXCenter = boatCenterPosition.x - halfSize + biteSize/2;
        }

        // return
        return new Vector2(bitingPointXCenter, boatCenterPosition.y);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        GameObject colliderObjGameObject = other.gameObject;
        if (colliderObjGameObject.CompareTag("Player"))
        {
            touchedPlayer = true;
        }
        else if (colliderObjGameObject.CompareTag("Fish"))
        {
            // Find busy slot
            int busySlotIndex = GetBusySpawnSlot();
            if (busySlotIndex != -1)
            {
                // Reduce cooldown of slot
                spawnCooldownTracker[busySlotIndex] = 0f;
            }
        }
        
    }

    private int GetBusySpawnSlot()
    {
        for (int i = 0; i < spawnCooldownTracker.Length; i++)
        {
            if (spawnCooldownTracker[i] >= Time.time) return i;
        }
        
        return -1;
    }
}
