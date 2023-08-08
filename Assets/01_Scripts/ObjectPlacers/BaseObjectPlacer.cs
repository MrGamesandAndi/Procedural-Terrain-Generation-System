using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProceduralGeneration.ObjectPlacers
{
    [System.Serializable]
    public class PlaceableObjectConfig
    {
        public bool hasHeightLimits = false;
        public float minHeightToSpawn = 0f;
        public float maxHeightToSpawn = 0f;
        public bool canGoInWater = false;
        public bool canGoAboveWater = true;

        [Range(0f, 1f)] public float weighting = 1f;
        public List<GameObject> prefabs;
        
        public float NormalisedWeighting { get; set; } = 0f;
    }

    public class BaseObjectPlacer : MonoBehaviour
    {
       
        [SerializeField] protected List<PlaceableObjectConfig> _objects;
        [SerializeField] protected float _targetDensity = 0.1f;
        [SerializeField] protected int _maxSpawnCount = 1000;
        [SerializeField] protected int _maxInvalidLocationSkips = 10;
        [SerializeField] protected float _maxPositionJitter = 0.15f;

        public virtual void Execute(ProcGenManager.GenerationData generationData, int biomeIndex = -1, BiomeConfigSO biome = null)
        {
            //Validate configs
            foreach (var config in _objects)
            {
                if (!config.canGoInWater && !config.canGoAboveWater)
                {
                    throw new System.InvalidOperationException($"Object placer forbids both in and out of water. Can't run!");
                }
            }

            //Normalize weightings
            float weightSum = 0f;

            foreach (var config in _objects)
            {
                weightSum += config.weighting;
            }

            foreach (var config in _objects)
            {
                config.NormalisedWeighting = config.weighting / weightSum;
            }

            //Debug.LogError($"No implementation of Execute function for {gameObject.name}.");
        }

        protected List<Vector3> GetAllLocationsForBiome(ProcGenManager.GenerationData generationData, int biomeIndex)
        {
            List<Vector3> locations = new List<Vector3>(generationData.mapResolution * generationData.mapResolution / 10);

            for (int y = 0; y < generationData.mapResolution; y++)
            {
                for (int x = 0; x < generationData.mapResolution; x++)
                {
                    if (generationData.biomeMap[x, y] != biomeIndex)
                    {
                        continue;
                    }

                    float height = generationData.heightMap[x, y] * generationData.heightmapScale.y;
                    locations.Add(new Vector3(y * generationData.heightmapScale.z, height, x * generationData.heightmapScale.x));
                }
            }

            return locations;
        }

        protected virtual void ExecuteSimpleSpawning(ProcGenManager.GenerationData generationData, List<Vector3> candidateLocations)
        {
            foreach (var spawnConfig in _objects)
            {
                var prefab = spawnConfig.prefabs[generationData.Random(0, spawnConfig.prefabs.Count)];

                //Determine spawn count
                float baseSpawnCount = Mathf.Min(_maxSpawnCount, candidateLocations.Count * _targetDensity);
                int numToSpawn = Mathf.FloorToInt(spawnConfig.NormalisedWeighting * baseSpawnCount);
                int skipCount = 0;
                int numPlaced = 0;

                for (int index = 0; index < numToSpawn; index++)
                {
                    //Pick a random location to spawn an object
                    int randomLocationIndex = generationData.Random(0, candidateLocations.Count);
                    Vector3 spawnLocation = candidateLocations[randomLocationIndex];

                    //Is height invalid?
                    bool isValid = true;

                    if (spawnLocation.y < generationData.config.waterHeight && !spawnConfig.canGoInWater)
                    {
                        isValid = false;
                    }

                    if (spawnLocation.y >= generationData.config.waterHeight && !spawnConfig.canGoAboveWater)
                    {
                        isValid = false;
                    }

                    //Not valid if outside of height limits
                    if (spawnConfig.hasHeightLimits && (spawnLocation.y < spawnConfig.minHeightToSpawn ||
                        spawnLocation.y >= spawnConfig.maxHeightToSpawn))
                    {
                        isValid = false;
                    }

                    //Skip if location is not valid
                    if (!isValid)
                    {
                        skipCount++;
                        index--;

                        if (skipCount >= _maxInvalidLocationSkips)
                        {
                            break;
                        }

                        continue;
                    }

                    skipCount = 0;
                    numPlaced++;

                    //Remove the location if chosen
                    candidateLocations.RemoveAt(randomLocationIndex);
                    SpawnObject(generationData, prefab, spawnLocation);
                }

                //Debug.Log($"Placed {numPlaced} objects out of {numToSpawn}");
            }
        }

        protected virtual void SpawnObject(ProcGenManager.GenerationData generationData, GameObject prefab, Vector3 spawnLocation)
        {
            Quaternion spawnRotation = Quaternion.Euler(0f, generationData.Random(0f, 360f), 0f);
            Vector3 positionOffset = new Vector3(generationData.Random(-_maxPositionJitter, _maxPositionJitter), 
                0f, generationData.Random(-_maxPositionJitter, _maxPositionJitter));

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Instantiate(prefab, spawnLocation + positionOffset, Quaternion.identity, generationData.objectParent);
            }
            else
            {
                var spawnedGO = PrefabUtility.InstantiatePrefab(prefab, generationData.objectParent) as GameObject;
                spawnedGO.transform.position = spawnLocation + positionOffset;
                spawnedGO.transform.rotation = spawnRotation;
                Undo.RegisterCreatedObjectUndo(spawnedGO, "Placed object");
            }
#else
            //Instantiate the prefab
            Instantiate(prefab, spawnLocation + positionOffset, spawnRotation, generationData.objectParent);
#endif
        }
    }
}