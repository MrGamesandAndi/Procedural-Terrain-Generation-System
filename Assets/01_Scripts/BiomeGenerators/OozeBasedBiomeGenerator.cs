using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ProceduralGeneration.BiomeGenerators
{
    public class OozeBasedBiomeGenerator : BaseBiomeMapGenerator
    {
        public enum BiomeMapBaseResolution
        {
            Size_64x64 = 64,
            Size_128x128 = 128,
            Size_256x256 = 256,
            Size_512x512 = 512
        }

        byte[,] _biomeMapLowResolution;
        float[,] _biomeStrengthsLowResolution;
        Vector2Int[] _neighbourOffsets = new Vector2Int[]
        {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 1),
            new Vector2Int(-1, -1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1)
        };

        [Range(0f, 1f)] public float biomeSeedPointDensity = 0.1f;
        public BiomeMapBaseResolution biomeMapResolution = BiomeMapBaseResolution.Size_64x64;

        public override void Execute(ProcGenManager.GenerationData generationData)
        {
            PerformBiomeGenerationLowResolution(generationData, (int)biomeMapResolution);
            PerformBiomeGenerationHighResolution(generationData, (int)biomeMapResolution, generationData.mapResolution, generationData.biomeMap, generationData.biomeStrengths);
        }

        private void PerformBiomeGenerationLowResolution(ProcGenManager.GenerationData generationData, int mapResolution)
        {
            //Allocate the biome map and strength map
            _biomeMapLowResolution = new byte[mapResolution, mapResolution];
            _biomeStrengthsLowResolution = new float[mapResolution, mapResolution];
            int numSpeedPoints = Mathf.FloorToInt(mapResolution * mapResolution * biomeSeedPointDensity);
            List<byte> biomesToSpawn = new List<byte>(numSpeedPoints);

            //Populate the biomes to spawn based on weightings
            float totalBiomeWeighting = generationData.config.TotalWeighting;

            for (int biomeIndex = 0; biomeIndex < generationData.config.NumBiomes; biomeIndex++)
            {
                int numEntries = Mathf.RoundToInt(numSpeedPoints * generationData.config.biomes[biomeIndex].weighting / totalBiomeWeighting);
                //Debug.Log($"Will spawn: {numEntries}. Seed points for: {_config.biomes[biomeIndex].biome.biomeName}");

                for (int entryIndex = 0; entryIndex < numEntries; entryIndex++)
                {
                    biomesToSpawn.Add((byte)biomeIndex);
                }
            }

            //Spawn the individual biomes
            while (biomesToSpawn.Count > 0)
            {
                //Pick a random seed point
                int seedPointIndex = generationData.Random(0, biomesToSpawn.Count);

                //Extract the biome index
                byte biomeIndex = biomesToSpawn[seedPointIndex];

                //Remove seed point
                biomesToSpawn.RemoveAt(seedPointIndex);
                SpawnIndividualBiome(generationData, biomeIndex, mapResolution);
            }

#if UNITY_EDITOR
            //Save out biome map
            Texture2D biomeMapTexture = new Texture2D(mapResolution, mapResolution, TextureFormat.RGB24, false);

            for (int y = 0; y < mapResolution; y++)
            {
                for (int x = 0; x < mapResolution; x++)
                {
                    float hue = ((float)_biomeMapLowResolution[x, y] / (float)generationData.config.NumBiomes);
                    biomeMapTexture.SetPixel(x, y, Color.HSVToRGB(hue, 0.75f, 0.75f));
                }
            }

            biomeMapTexture.Apply();
            File.WriteAllBytes("BiomeMap_LowRes.png", biomeMapTexture.EncodeToPNG());
#endif
        }

        private void SpawnIndividualBiome(ProcGenManager.GenerationData generationData, byte biomeIndex, int mapResolution)
        {
            //Cache biome config
            BiomeConfigSO biomeConfig = generationData.config.biomes[biomeIndex].biome;

            //Pick spawn location
            Vector2Int spawnLocation = new Vector2Int(generationData.Random(0, mapResolution), generationData.Random(0, mapResolution));

            //Pick the starting intensity
            float startIntesity = generationData.Random(biomeConfig.minIntensity, biomeConfig.maxIntensity);

            //Setup working list
            Queue<Vector2Int> pointsToProcess = new Queue<Vector2Int>();
            pointsToProcess.Enqueue(spawnLocation);

            //Setup visited map and target intensity map
            bool[,] wasVisited = new bool[mapResolution, mapResolution];
            float[,] targetIntensity = new float[mapResolution, mapResolution];

            //Set the starting intensity
            targetIntensity[spawnLocation.x, spawnLocation.y] = startIntesity;

            while (pointsToProcess.Count > 0)
            {
                Vector2Int workingLocation = pointsToProcess.Dequeue();

                //Set biome
                _biomeMapLowResolution[workingLocation.x, workingLocation.y] = biomeIndex;
                wasVisited[workingLocation.x, workingLocation.y] = true;
                _biomeStrengthsLowResolution[workingLocation.x, workingLocation.y] = targetIntensity[workingLocation.x, workingLocation.y];

                //Traverse the neighbours
                for (int neighbourIndex = 0; neighbourIndex < _neighbourOffsets.Length; neighbourIndex++)
                {
                    Vector2Int neighbourLocation = workingLocation + _neighbourOffsets[neighbourIndex];

                    //Skip if invalid
                    if (neighbourLocation.x < 0 || neighbourLocation.y < 0 || neighbourLocation.x >= mapResolution ||
                        neighbourLocation.y >= mapResolution)
                    {
                        continue;
                    }

                    //Skip is already visited
                    if (wasVisited[neighbourLocation.x, neighbourLocation.y])
                    {
                        continue;
                    }

                    //Flag as visited
                    wasVisited[workingLocation.x, workingLocation.y] = true;

                    //Work out neighbour strength
                    float decayAmount = generationData.Random(biomeConfig.minDecayRate, biomeConfig.maxDecayRate) *
                        _neighbourOffsets[neighbourIndex].magnitude;
                    float neighbourStrength = targetIntensity[workingLocation.x, workingLocation.y] - decayAmount;
                    targetIntensity[neighbourLocation.x, neighbourLocation.y] = neighbourStrength;

                    //If the strength is too low stop
                    if (neighbourStrength <= 0)
                    {
                        continue;
                    }

                    pointsToProcess.Enqueue(neighbourLocation);
                }
            }
        }

        private byte CalculateHighResBiomeIndex(int lowResMapSize, int lowResX, int lowResY, float fractionX, float fractionY)
        {
            float A = _biomeMapLowResolution[lowResX, lowResY];
            float B = (lowResX + 1) < lowResMapSize ? _biomeMapLowResolution[lowResX + 1, lowResY] : A;
            float C = (lowResY + 1) < lowResMapSize ? _biomeMapLowResolution[lowResX, lowResY + 1] : A;
            float D = 0f;

            if ((lowResX + 1) >= lowResMapSize)
            {
                D = C;
            }
            else if ((lowResY + 1) >= lowResMapSize)
            {
                D = B;
            }
            else
            {
                D = _biomeMapLowResolution[lowResX + 1, lowResY + 1];
            }

            //Bilinear filtering
            float filteredIndex = A * (1 - fractionX) * (1 - fractionY) + B * fractionX * (1 - fractionY) *
                C * fractionY * (1 - fractionX) + D * fractionX * fractionY;

            //Building an Array of the possible biomes based on the values used to interpolate
            float[] candidateBiomes = new float[] { A, B, C, D };

            //Finding the neighbouring biome closest to the interpolated biome
            float bestBiome = -1f;
            float bestDelta = float.MaxValue;

            for (int biomeIndex = 0; biomeIndex < candidateBiomes.Length; biomeIndex++)
            {
                float delta = Mathf.Abs(filteredIndex - candidateBiomes[biomeIndex]);

                if (delta < bestDelta)
                {
                    bestDelta = delta;
                    bestBiome = candidateBiomes[biomeIndex];
                }
            }

            return (byte)Mathf.RoundToInt(bestBiome);
        }

        private void PerformBiomeGenerationHighResolution(ProcGenManager.GenerationData generationData, int lowResMapSize, int highResMapSize, byte[,] biomeMap, float[,] biomeStrengths)
        {
            //Calculate map scale
            float mapScale = (float)lowResMapSize / (float)highResMapSize;

            //Calculate the high resolution map
            for (int y = 0; y < highResMapSize; y++)
            {
                int lowResY = Mathf.FloorToInt(y * mapScale);
                float yFraction = y * mapScale - lowResY;

                for (int x = 0; x < highResMapSize; x++)
                {
                    int lowResX = Mathf.FloorToInt(x * mapScale);
                    float xFraction = x * mapScale - lowResX;
                    biomeMap[x, y] = CalculateHighResBiomeIndex(lowResMapSize, lowResX, lowResY, xFraction, yFraction);

                    //No interpolation (point based)
                    //_biomeMap[x, y] = _biomeMapLowResolution[lowResX, lowResY];
                }
            }

#if UNITY_EDITOR
            //Save out biome map
            Texture2D biomeMapTexture = new Texture2D(highResMapSize, highResMapSize, TextureFormat.RGB24, false);

            for (int y = 0; y < highResMapSize; y++)
            {
                for (int x = 0; x < highResMapSize; x++)
                {
                    float hue = ((float)biomeMap[x, y] / (float)generationData.config.NumBiomes);
                    biomeMapTexture.SetPixel(x, y, Color.HSVToRGB(hue, 0.75f, 0.75f));
                }
            }

            biomeMapTexture.Apply();
            File.WriteAllBytes("BiomeMap_HighRes.png", biomeMapTexture.EncodeToPNG());
#endif
        }
    }
}