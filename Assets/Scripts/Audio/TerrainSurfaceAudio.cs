using UnityEngine;
using Outpost404.Building;

namespace Outpost404.Audio
{
    /// <summary>
    /// Samples native Unity Terrain splatmap alphamaps at world positions to detect
    /// dominant TerrainLayers underfoot and dispatches surface-responsive footstep audio.
    /// Safely falls back to default gravel footsteps when off-terrain or atop structures.
    /// </summary>
    [DisallowMultipleComponent]
    public class TerrainSurfaceAudio : MonoBehaviour
    {
        [Header("Terrain Reference")]
        [Tooltip("Cached terrain reference. Defaults to Terrain.activeTerrain if unassigned.")]
        [SerializeField] private Terrain targetTerrain;

        [Header("Surface Raycast Detection")]
        [Tooltip("Raycast distance downwards from character root to detect ground/structures.")]
        [SerializeField] private float raycastDistance = 2.5f;

        [Tooltip("Layer mask checked when probing ground under boots.")]
        [SerializeField] private LayerMask groundLayerMask = ~0;

        [Header("Runtime State")]
        [SerializeField] private FootstepSurfaceType currentSurface = FootstepSurfaceType.Default;

        public Terrain TargetTerrain { get => targetTerrain; set => targetTerrain = value; }
        public FootstepSurfaceType CurrentSurface => currentSurface;
        public float RaycastDistance { get => raycastDistance; set => raycastDistance = value; }

        private void Awake()
        {
            if (targetTerrain == null)
            {
                targetTerrain = Terrain.activeTerrain;
            }
        }

        /// <summary>
        /// Detects the dominant ground surface beneath the given world coordinates.
        /// Checks for physical structures first, then samples terrain alphamaps.
        /// </summary>
        public static FootstepSurfaceType DetectSurface(Vector3 worldPosition, Terrain fallbackTerrain = null, float probeDistance = 2.5f)
        {
            // 1. Raycast downwards to identify structures, platforms, or natural terrain
            Vector3 rayOrigin = worldPosition + Vector3.up * 0.5f;
            Terrain hitTerrain = null;
            bool hitStructure = false;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, probeDistance))
            {
                // Check if hit object is a player-built structure or artificial obstacle
                if (hit.collider.GetComponentInParent<BuildableStructure>() != null)
                {
                    hitStructure = true;
                }
                else
                {
                    string colName = hit.collider.gameObject.name.ToLowerInvariant();
                    if (colName.Contains("wall") || colName.Contains("floor") || colName.Contains("foundation") || colName.Contains("beacon"))
                    {
                        hitStructure = true;
                    }
                }

                if (hitStructure)
                {
                    return FootstepSurfaceType.Default;
                }

                var tCol = hit.collider.GetComponent<TerrainCollider>();
                if (tCol != null)
                {
                    hitTerrain = hit.collider.GetComponent<Terrain>();
                }
            }

            Terrain terrain = hitTerrain ?? fallbackTerrain ?? Terrain.activeTerrain;
            if (terrain == null || terrain.terrainData == null)
            {
                return FootstepSurfaceType.Default;
            }

            TerrainData tData = terrain.terrainData;
            Vector3 terrainPos = terrain.transform.position;
            Vector3 terrainSize = tData.size;

            // 2. Bounds check - fallback to Default if world position is off-terrain
            float relX = worldPosition.x - terrainPos.x;
            float relZ = worldPosition.z - terrainPos.z;

            if (relX < 0f || relX > terrainSize.x || relZ < 0f || relZ > terrainSize.z)
            {
                return FootstepSurfaceType.Default;
            }

            // 3. Convert world position to splatmap alphamap coordinates
            float normX = Mathf.Clamp01(relX / terrainSize.x);
            float normZ = Mathf.Clamp01(relZ / terrainSize.z);

            int alphaWidth = tData.alphamapWidth;
            int alphaHeight = tData.alphamapHeight;

            int mapX = Mathf.Clamp(Mathf.FloorToInt(normX * (alphaWidth - 1)), 0, alphaWidth - 1);
            int mapZ = Mathf.Clamp(Mathf.FloorToInt(normZ * (alphaHeight - 1)), 0, alphaHeight - 1);

            float[,,] splat = tData.GetAlphamaps(mapX, mapZ, 1, 1);
            if (splat == null || splat.GetLength(2) == 0)
            {
                return FootstepSurfaceType.Default;
            }

            TerrainLayer[] layers = tData.terrainLayers;
            int numLayers = Mathf.Min(layers != null ? layers.Length : 0, splat.GetLength(2));
            if (numLayers == 0)
            {
                return FootstepSurfaceType.Default;
            }

            // 4. Find dominant layer index with highest splat weight
            int dominantIndex = 0;
            float maxWeight = -1f;

            for (int i = 0; i < numLayers; i++)
            {
                float weight = splat[0, 0, i];
                if (weight > maxWeight)
                {
                    maxWeight = weight;
                    dominantIndex = i;
                }
            }

            if (maxWeight <= 0.001f)
            {
                return FootstepSurfaceType.Default;
            }

            // 5. Map dominant layer to FootstepSurfaceType
            TerrainLayer dominantLayer = layers[dominantIndex];
            string layerName = dominantLayer != null ? dominantLayer.name.ToLowerInvariant() : "";

            if (layerName.Contains("moss") || dominantIndex == 1)
            {
                return FootstepSurfaceType.BiolumMoss;
            }
            if (layerName.Contains("rock") || layerName.Contains("crystal") || dominantIndex == 2)
            {
                return FootstepSurfaceType.CrystallineRock;
            }
            if (layerName.Contains("basalt") || layerName.Contains("soil") || layerName.Contains("ash") || dominantIndex == 0)
            {
                return FootstepSurfaceType.BasaltSoil;
            }

            return FootstepSurfaceType.Default;
        }

        /// <summary>
        /// Updates and returns the current surface beneath this GameObject's transform position.
        /// </summary>
        public FootstepSurfaceType SampleCurrentSurface()
        {
            currentSurface = DetectSurface(transform.position, targetTerrain);
            return currentSurface;
        }
    }
}
