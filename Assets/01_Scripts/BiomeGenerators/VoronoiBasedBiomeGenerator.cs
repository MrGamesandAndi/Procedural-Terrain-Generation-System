using System.IO;
using UnityEngine;

namespace ProceduralGeneration.BiomeGenerators
{
    public class VoronoiBasedBiomeGenerator : BaseBiomeMapGenerator
    {
        [SerializeField] int _numCells = 20;
        [SerializeField] int _resampleDistance = 20;
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

        public override void Execute(ProcGenManager.GenerationData generationData)
        {
            int cellSize = Mathf.CeilToInt((float)generationData.mapResolution / _numCells);

            //Generate seed points
            Vector3Int[] biomeSeeds = new Vector3Int[_numCells * _numCells];

            for (int cellY = 0; cellY < _numCells; cellY++)
            {
                int centerY = Mathf.RoundToInt((cellY + 0.5f) * cellSize);

                for (int cellX = 0; cellX < _numCells; cellX++)
                {
                    int cellIndex = cellX + cellY * _numCells;
                    int centerX = Mathf.RoundToInt((cellX + 0.5f) * cellSize);
                    biomeSeeds[cellIndex].x = centerX + generationData.Random(-cellSize / 2, cellSize / 2);
                    biomeSeeds[cellIndex].y = centerY + generationData.Random(-cellSize / 2, cellSize / 2);
                    biomeSeeds[cellIndex].z = generationData.Random(0, generationData.config.NumBiomes);
                }
            }

            //Generate base biome map
            byte[,] baseBiomeMap = new byte[generationData.mapResolution, generationData.mapResolution];

            for (int y = 0; y < generationData.mapResolution; y++)
            {
                for (int x = 0; x < generationData.mapResolution; x++)
                {
                    baseBiomeMap[x, y] = FindClosestBiome(x, y, _numCells, cellSize, biomeSeeds);
                }
            }
            
            for (int y = 0; y < generationData.mapResolution; y++)
            {
                for (int x = 0; x < generationData.mapResolution; x++)
                {
                    generationData.biomeMap[x, y] = ResampleBiomeMap(x, y, baseBiomeMap, generationData.mapResolution);
                }
            }

#if UNITY_EDITOR
            //Save out biome map
            Texture2D biomeMapTexture = new Texture2D(generationData.mapResolution, generationData.mapResolution, TextureFormat.RGB24, false);

            for (int y = 0; y < generationData.mapResolution; y++)
            {
                for (int x = 0; x < generationData.mapResolution; x++)
                {
                    float hue = ((float)baseBiomeMap[x, y] / (float)generationData.config.NumBiomes);
                    biomeMapTexture.SetPixel(x, y, Color.HSVToRGB(hue, 0.75f, 0.75f));
                }
            }

            biomeMapTexture.Apply();
            File.WriteAllBytes("BiomeMap_VoronoiBase.png", biomeMapTexture.EncodeToPNG());


            biomeMapTexture = new Texture2D(generationData.mapResolution, generationData.mapResolution, TextureFormat.RGB24, false);

            for (int y = 0; y < generationData.mapResolution; y++)
            {
                for (int x = 0; x < generationData.mapResolution; x++)
                {
                    float hue = (float)generationData.biomeMap[x, y] / (float)generationData.config.NumBiomes;
                    biomeMapTexture.SetPixel(x, y, Color.HSVToRGB(hue, 0.75f, 0.75f));
                }
            }

            biomeMapTexture.Apply();
            File.WriteAllBytes("BiomeMap_VoronoiFinal.png", biomeMapTexture.EncodeToPNG());
#endif
        }

        private byte FindClosestBiome(int x, int y, int numCells, int cellSize, Vector3Int[] biomeSeeds)
        {
            int cellX = x / cellSize;
            int cellY = y / cellSize;
            int cellIndex = cellX + cellY * numCells;
            float closestSeedDistanceSq = (biomeSeeds[cellIndex].x - x) * (biomeSeeds[cellIndex].x - x) +
                (biomeSeeds[cellIndex].y - y) * (biomeSeeds[cellIndex].y - y);
            byte bestBiome = (byte)biomeSeeds[cellIndex].z;

            foreach (var neighbourOffset in _neighbourOffsets)
            {
                int workingCellX = cellX + neighbourOffset.x;
                int workingCellY = cellY + neighbourOffset.y;
                cellIndex = workingCellX + workingCellY * numCells;

                if (workingCellX < 0 || workingCellY < 0 || workingCellX >= numCells || workingCellY >= numCells)
                {
                    continue;
                }

                float distanceSq = (biomeSeeds[cellIndex].x - x) * (biomeSeeds[cellIndex].x - x) +
                (biomeSeeds[cellIndex].y - y) * (biomeSeeds[cellIndex].y - y);

                if (distanceSq < closestSeedDistanceSq)
                {
                    closestSeedDistanceSq = distanceSq;
                    bestBiome = (byte)biomeSeeds[cellIndex].z;
                }
            }

            return bestBiome;
        }

        private byte ResampleBiomeMap(int x, int y, byte[,] biomeMap, int mapResolution)
        {
            float noise = 2f * (Mathf.PerlinNoise((float)x / mapResolution, (float)y / mapResolution) - 0.5f);
            int newX = Mathf.Clamp(Mathf.RoundToInt(x + noise * _resampleDistance), 0, mapResolution - 1);
            int newY = Mathf.Clamp(Mathf.RoundToInt(y + noise * _resampleDistance), 0, mapResolution - 1);
            return biomeMap[newX, newY];
        }
    }
}