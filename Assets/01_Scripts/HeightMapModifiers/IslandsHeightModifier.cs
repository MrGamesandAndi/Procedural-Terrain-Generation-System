using UnityEngine;

namespace ProceduralGeneration.HeightModifiers
{
    public class IslandsHeightModifier : BaseHeightMapModifier
    {
        [SerializeField] [Range(1, 100)] int _numIslands = 100;
        [SerializeField] float _minIslandSize = 20f;
        [SerializeField] float _maxIslandSize = 80f;
        [SerializeField] float _minIslandHeight = 10f;
        [SerializeField] float _maxIslandHeight = 40f;
        [SerializeField] float _angleNoiseScale = 1f;
        [SerializeField] float _distanceNoiseScale = 1f;
        [SerializeField] float _noiseHeightDelta = 5f;
        [SerializeField] AnimationCurve _islandShapeCurve;

        public override void Execute(ProcGenManager.GenerationData generationData, int biomeIndex = -1, BiomeConfigSO biome = null)
        {
            for (int island = 0; island < _numIslands; island++)
            {
                PlaceIsland(generationData, generationData.mapResolution, generationData.heightMap, generationData.heightmapScale, generationData.biomeMap, biomeIndex, biome);
            }
        }

        private void PlaceIsland(ProcGenManager.GenerationData generationData, int mapResolution, float[,] heightMap, Vector3 heightMapScale, byte[,] biomeMap, int biomeIndex, BiomeConfigSO biome)
        {
            int workingIslandSize = Mathf.RoundToInt(generationData.Random(_minIslandSize, _maxIslandSize) / heightMapScale.x);
            float workingIslandHeight = (generationData.Random(_minIslandHeight, _maxIslandHeight) + generationData.config.waterHeight) / heightMapScale.y;
            int centerX = generationData.Random(workingIslandSize, mapResolution - workingIslandSize);
            int centerY = generationData.Random(workingIslandSize, mapResolution - workingIslandSize);

            for (int islandY = -workingIslandSize; islandY <= workingIslandSize; islandY++)
            {
                int y = centerY + islandY;

                if (y < 0 || y >= mapResolution)
                {
                    continue;
                }

                for (int islandX = -workingIslandSize; islandX <= workingIslandSize; islandX++)
                {
                    int x = centerX + islandX;

                    if (x < 0 || x >= mapResolution)
                    {
                        continue;
                    }

                    float normalisedDistance = Mathf.Sqrt(islandX * islandX + islandY * islandY) / workingIslandSize;

                    if (normalisedDistance > 1)
                    {
                        continue;
                    }

                    float normalisedAngle = Mathf.Clamp01((Mathf.Atan2(islandY, islandX) + Mathf.PI) / (2 * Mathf.PI));
                    float noise = Mathf.PerlinNoise(normalisedAngle * _angleNoiseScale, normalisedDistance * _distanceNoiseScale);
                    float noiseHeightDelta = ((noise - 0.5f) * 2f) * _noiseHeightDelta / heightMapScale.y;

                    float height = workingIslandHeight * _islandShapeCurve.Evaluate(normalisedDistance) + noiseHeightDelta;
                    heightMap[x, y] = Mathf.Max(heightMap[x, y], height);
                }
            }
        }
    }
}