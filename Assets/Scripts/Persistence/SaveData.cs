using System;
using System.Collections.Generic;
using UnityEngine;
using Outpost404.Building;

namespace Outpost404.Persistence
{
    [Serializable]
    public struct Vector3Data
    {
        public float x;
        public float y;
        public float z;

        public Vector3Data(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    [Serializable]
    public struct SavedInventorySlot
    {
        public string itemId;
        public int quantity;

        public SavedInventorySlot(string id, int qty)
        {
            itemId = id;
            quantity = qty;
        }
    }

    [Serializable]
    public struct SavedStructure
    {
        public StructureType structureType;
        public Vector3Data position;
        public float rotationY;
        public float currentDurability;
        public float maxDurability;

        public SavedStructure(StructureType type, Vector3 pos, float rotY, float currentDur, float maxDur)
        {
            structureType = type;
            position = new Vector3Data(pos);
            rotationY = rotY;
            currentDurability = currentDur;
            maxDurability = maxDur;
        }
    }

    /// <summary>
    /// Pure serializable data model capturing entire game state for persistence roundtrip:
    /// player vitals, inventory contents, placed world structures, beacon objective progression,
    /// day-night diurnal time, and mission statistics.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public string version = "1.0";
        public long timestamp;

        // Player Vitals & Transform
        public Vector3Data playerPosition;
        public float playerRotationY;
        public float playerHealth = 100f;
        public float playerHunger = 100f;
        public float playerStamina = 100f;
        public float playerBodyTemperature = 37f;

        // Inventory
        public List<SavedInventorySlot> inventorySlots = new List<SavedInventorySlot>();

        // Placed World Structures
        public List<SavedStructure> placedStructures = new List<SavedStructure>();

        // Objective Progress
        public int beaconRepairStage = 0;
        public bool isBeaconCountdownActive = false;
        public float beaconCountdownRemaining = 60f;

        // Diurnal Time & Calendar
        public int dayCount = 1;
        public float normalizedDiurnalTime = 0.0f;

        // Mission Statistics
        public int enemiesSlain = 0;
        public int resourcesHarvested = 0;
        public int structuresBuilt = 0;
    }
}
