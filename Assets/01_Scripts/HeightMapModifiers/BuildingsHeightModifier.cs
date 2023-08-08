using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProceduralGeneration.HeightModifiers
{
    [System.Serializable]
    public class BuildingConfig
    {
        public Texture2D heightMap;
        public GameObject prefab;
        public int radius;
        public int numToSpawn = 1;
        public bool hasHeightLimits = false;
        public float minHeightToSpawn = 0f;
        public float maxHeightToSpawn = 0f;
        public bool canGoInWater = false;
        public bool canGoAboveWater = true;
    }

    public class BuildingsHeightModifier : BaseHeightMapModifier
    {
        [SerializeField] List<BuildingConfig> _buildings;

        protected void SpawnBuilding(ProcGenConfigSO globalConfig, BuildingConfig building, int spawnX, int spawnY, int mapResolution, float[,] heightMap, Vector3 heightMapScale, Transform buildingParent)
        {
            float _averageHeight = 0f;
            int numHeightSamples = 0;

            //Sum height values under feature
            for (int y = -building.radius; y <= building.radius; y++)
            {
                for (int x = -building.radius; x <= building.radius; x++)
                {
                    //Sum heightmap values
                    _averageHeight += heightMap[x + spawnX, y + spawnY];
                    numHeightSamples++;
                }
            }

            //Calculate average height
            _averageHeight /= numHeightSamples;
            float targetHeight = _averageHeight;

            if (!building.canGoInWater)
            {
                targetHeight = Mathf.Max(targetHeight, globalConfig.waterHeight / heightMapScale.y);
            }

            if (building.hasHeightLimits)
            {
                targetHeight = Mathf.Clamp(targetHeight, building.minHeightToSpawn / heightMapScale.y,
                    building.maxHeightToSpawn / heightMapScale.y);
            }

            //Apply building heightmap
            for (int y = -building.radius; y <= building.radius; y++)
            {
                int workingY = y + spawnY;
                float textureY = Mathf.Clamp01((float)(y + building.radius) / (building.radius * 2f));

                for (int x = -building.radius; x <= building.radius; x++)
                {
                    int workingX = x + spawnX;
                    float textureX = Mathf.Clamp01((float)(x + building.radius) / (building.radius * 2f));

                    //Sample heightmap
                    var pixelColor = building.heightMap.GetPixelBilinear(textureX, textureY);
                    float strength = pixelColor.r;

                    //Blend based on strength
                    heightMap[workingX, workingY] = Mathf.Lerp(heightMap[workingX, workingY], targetHeight, strength);
                }
            }

            //Spawn building
            Vector3 buildingLocation = new Vector3(spawnY * heightMapScale.z, heightMap[spawnX, spawnY] * 
                heightMapScale.y, spawnX * heightMapScale.x);

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Instantiate(building.prefab, buildingLocation, Quaternion.identity, buildingParent);
            }
            else
            {
                var spawnedGO = PrefabUtility.InstantiatePrefab(building.prefab, buildingParent) as GameObject;
                spawnedGO.transform.position = buildingLocation;
                Undo.RegisterCreatedObjectUndo(spawnedGO, "Add building");
            }
#else
                //Instantiate the prefab
                Instantiate(building.prefab, buildingLocation, Quaternion.identity, buildingParent);
#endif
        }

        public override void Execute(ProcGenManager.GenerationData generationData, int biomeIndex = -1, BiomeConfigSO biome = null)
        {
            var buildingParent = FindObjectOfType<ProcGenManager>().transform;

            //Traverse buldings
            foreach (var building in _buildings)
            {
                var spawnLocations = GetSpawnedLocationsForBuilding(generationData.config, generationData.mapResolution, generationData.heightMap,
                    generationData.heightmapScale, building);

                for (int buildingIndex = 0; buildingIndex < building.numToSpawn && spawnLocations.Count > 0; buildingIndex++)
                {
                    int spawnIndex = generationData.Random(0, spawnLocations.Count);
                    var spawnPos = spawnLocations[spawnIndex];
                    spawnLocations.RemoveAt(spawnIndex);
                    SpawnBuilding(generationData.config, building, spawnPos.x, spawnPos.y, generationData.mapResolution, generationData.heightMap, generationData.heightmapScale, buildingParent);
                }
            }
        }

        protected List<Vector2Int> GetSpawnedLocationsForBuilding(ProcGenConfigSO globalConfig, int mapResolution, float[,] heightMap, Vector3 heightMapScale, BuildingConfig buildingConfig)
        {
            List<Vector2Int> locations = new List<Vector2Int>(mapResolution * mapResolution / 10);

            for (int y = buildingConfig.radius; y < mapResolution - buildingConfig.radius; y += buildingConfig.radius * 2)
            {
                for (int x = buildingConfig.radius; x < mapResolution - buildingConfig.radius; x += buildingConfig.radius * 2)
                {
                    float height = heightMap[x, y] * heightMapScale.y;

                    //Skip is height is invalid
                    if (height < globalConfig.waterHeight && !buildingConfig.canGoInWater)
                    {
                        continue;
                    }

                    if (height >= globalConfig.waterHeight && !buildingConfig.canGoAboveWater)
                    {
                        continue;
                    }

                    //Skip if outside of height limits
                    if (buildingConfig.hasHeightLimits && (height < buildingConfig.minHeightToSpawn || 
                        height >= buildingConfig.maxHeightToSpawn))
                    {
                        continue;
                    }

                    locations.Add(new Vector2Int(x, y));
                }
            }

            return locations;
        }
    }
}