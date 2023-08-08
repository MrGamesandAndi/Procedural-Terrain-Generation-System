using System.Collections.Generic;
using UnityEngine;
using ProceduralGeneration.TexturePainters;
using ProceduralGeneration.HeightModifiers;
using System.IO;
using ProceduralGeneration.ObjectPlacers;
using System.Linq;
using System.Collections;
using ProceduralGeneration.DetailPainters;
using ProceduralGeneration.BiomeGenerators;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
#endif

namespace ProceduralGeneration
{
    public enum GenerationStage
    {
        Beginning = 1,
        BuildTextureMap,
        BuildDetailMap,
        BuildBiomeMap,
        HeightMapGeneration,
        TerrainPainting,
        ObjectPlacement,
        DetailPainting,
        Complete,
        NumStages = Complete
    }

    public class ProcGenManager : MonoBehaviour
    {
        public class GenerationData
        {
            System.Random _RNGenerator;

            public ProcGenManager manager;
            public ProcGenConfigSO config;
            public Transform objectParent;

            public int mapResolution;
            public float[,] heightMap;
            public Vector3 heightmapScale;
            public float[,,] alphaMaps;
            public int alphaMapResolution;

            public byte[,] biomeMap;
            public float[,] biomeStrengths;
            public float[,] slopeMap;

            public List<int[,]> detailLayerMaps;
            public int detailMapResolution;
            public int maxDetailsPerPatch;

            public Dictionary<TextureConfig, int> biomeTextureToTerrainLayerIndex = new Dictionary<TextureConfig, int>();
            public Dictionary<TerrainDetailConfig, int> biomeTerrainDetailToDetailLayerIndex = new Dictionary<TerrainDetailConfig, int>();

            public GenerationData(int inSeed)
            {
                _RNGenerator = new System.Random(inSeed);
            }

            public int Random(int minInclusive,int maxExclusive)
            {
                return _RNGenerator.Next(minInclusive, maxExclusive);
            }

            public float Random(float minInclusive,float maxInclusive)
            {
                return Mathf.Lerp(minInclusive, maxInclusive, (float)_RNGenerator.NextDouble());
            }
        }

        [SerializeField] ProcGenConfigSO _config;
        [SerializeField] Terrain _targetTerrain;
        [SerializeField] int _seed;
        [SerializeField] bool _randomizeSeedEvertyTime = true;

        [Header("DEBUG ONLY")]
        [SerializeField] bool DEBUG_TurnOffObjectPlacers = false;

        GenerationData _data;

        public IEnumerator AsyncRegenerateWorld(System.Action<GenerationStage, string> reportStatus = null)
        {
            int workingSeed = _seed;

            if (_randomizeSeedEvertyTime)
            {
                workingSeed = Random.Range(int.MinValue, int.MaxValue);

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Undo.RecordObject(this, "Randomize seed");
                    _seed = workingSeed;
                }
#endif
            }

            _data = new GenerationData(workingSeed);

            //Cache core information
            _data.manager = this;
            _data.config = _config;
            _data.objectParent = _targetTerrain.transform;

            //Cache map resolution
            _data.mapResolution = _targetTerrain.terrainData.heightmapResolution;
            _data.alphaMapResolution = _targetTerrain.terrainData.alphamapResolution;
            _data.detailMapResolution = _targetTerrain.terrainData.detailResolution;
            _data.maxDetailsPerPatch = _targetTerrain.terrainData.detailResolutionPerPatch;
            _data.heightmapScale = _targetTerrain.terrainData.heightmapScale;

            if (reportStatus != null)
            {
                reportStatus.Invoke(GenerationStage.Beginning, "Beginning Generation...");
            }

            yield return new WaitForSeconds(1f);

            //Clear every spawned object
            for (int childIndex = _data.objectParent.childCount - 1; childIndex >= 0; childIndex--)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    Destroy(_data.objectParent.GetChild(childIndex).gameObject);
                }
                else
                {
                    Undo.DestroyObjectImmediate(_data.objectParent.GetChild(childIndex).gameObject);
                }
#else
                Destroy(transform.GetChild(childIndex).gameObject);
#endif
            }

            if (reportStatus != null)
            {
                reportStatus.Invoke(GenerationStage.BuildTextureMap, "Building texture map...");
            }

            yield return new WaitForSeconds(1f);

            //Generate texture mapping
            PerformTextureMappingGeneration();

            if (reportStatus != null)
            {
                reportStatus.Invoke(GenerationStage.BuildDetailMap, "Building detail map...");
            }

            yield return new WaitForSeconds(1f);

            //Generate detail mapping
            PerformTerrainDetailMapping();

            if (reportStatus != null)
            {
                reportStatus.Invoke(GenerationStage.BuildBiomeMap, "Build biome map...");
            }

            yield return new WaitForSeconds(1f);

            //Generate biome map
            PerformBiomeGeneration();

            if (reportStatus != null)
            {
                reportStatus.Invoke(GenerationStage.HeightMapGeneration, "Modifying heights...");
            }

            yield return new WaitForSeconds(1f);

            //Update terrain heights
            PerformHeightMapModification();

            if (reportStatus != null)
            {
                reportStatus.Invoke(GenerationStage.TerrainPainting, "Painting terrain...");
            }

            yield return new WaitForSeconds(1f);

            //Paint terrain
            PerformTerrainPainting();

            if (reportStatus != null)
            {
                reportStatus.Invoke(GenerationStage.ObjectPlacement, "Placing objects...");
            }

            yield return new WaitForSeconds(1f);

            //Object placement
            PerformObjectPlacement();

            if (reportStatus != null)
            {
                reportStatus.Invoke(GenerationStage.DetailPainting, "Painting details...");
            }

            yield return new WaitForSeconds(1f);

            //Paint details
            PerformDetailPainting();

            if (reportStatus != null)
            {
                reportStatus.Invoke(GenerationStage.Complete, "Generation completed.");
            }
        }

        private void PerformTextureMappingGeneration()
        {
            _data.biomeTextureToTerrainLayerIndex.Clear();

            //Fill list of all textures
            List<TextureConfig> allTextures = new List<TextureConfig>();

            foreach (var biomeMetadata in _config.biomes)
            {
                List<TextureConfig> biomeTextures = biomeMetadata.biome.RetrieveTextures();

                if (biomeTextures == null || biomeTextures.Count == 0)
                {
                    continue;
                }

                allTextures.AddRange(biomeTextures);
            }

            if (_config.paintingPostProcessingModifier != null)
            {
                //Extract textures from painters
                BaseTexturePainter[] allPainters = _config.paintingPostProcessingModifier.GetComponents<BaseTexturePainter>();

                foreach (var painter in allPainters)
                {
                    var painterTextures = painter.RetrieveTextures();

                    if (painterTextures == null || painterTextures.Count == 0)
                    {
                        continue;
                    }

                    allTextures.AddRange(painterTextures);
                }
            }

            //Filter out duplicates
            allTextures = allTextures.Distinct().ToList();
            int layerIndex = 0;

            //Iterate over the texture configs
            foreach (var textureConfig in allTextures)
            {
                _data.biomeTextureToTerrainLayerIndex[textureConfig] = layerIndex;
                layerIndex++;
            }
        }

        private void PerformTerrainDetailMapping()
        {
            _data.biomeTerrainDetailToDetailLayerIndex.Clear();

            //Fill list of all textures
            List<TerrainDetailConfig> allTerrainDetails = new List<TerrainDetailConfig>();

            foreach (var biomeMetadata in _config.biomes)
            {
                List<TerrainDetailConfig> biomeTerrainDetails = biomeMetadata.biome.RetrieveTerrainDetails();

                if (biomeTerrainDetails == null || biomeTerrainDetails.Count == 0)
                {
                    continue;
                }

                allTerrainDetails.AddRange(biomeTerrainDetails);
            }

            if (_config.detailPaintingPostProcessingModifier != null)
            {
                //Extract all terrain details from painters
                BaseDetailPainter[] allPainters = _config.paintingPostProcessingModifier.GetComponents<BaseDetailPainter>();

                foreach (var painter in allPainters)
                {
                    var terrainDetails = painter.RetrieveTerrainDetails();

                    if (terrainDetails == null || terrainDetails.Count == 0)
                    {
                        continue;
                    }

                    allTerrainDetails.AddRange(terrainDetails);
                }
            }

            //Filter out duplicates
            allTerrainDetails = allTerrainDetails.Distinct().ToList();
            int layerIndex = 0;

            //Iterate over the terrain detail configs
            foreach (var terrainDetail in allTerrainDetails)
            {
                _data.biomeTerrainDetailToDetailLayerIndex[terrainDetail] = layerIndex;
                layerIndex++;
            }
        }

        private void PerformHeightMapModification()
        {
            _data.heightMap = _targetTerrain.terrainData.GetHeights(0, 0, _data.mapResolution, _data.mapResolution);

            //Initialize initial height modifiers
            if (_config.initialHeightModifier != null)
            {
                BaseHeightMapModifier[] modifiers = _config.initialHeightModifier.GetComponents<BaseHeightMapModifier>();

                foreach (var modifier in modifiers)
                {
                    modifier.Execute(_data);
                }
            }

            //Run heightmap generation for each biome
            for (int biomeIndex = 0; biomeIndex < _config.NumBiomes; biomeIndex++)
            {
                var biome = _config.biomes[biomeIndex].biome;

                if (biome.heightModifier == null)
                {
                    continue;
                }

                BaseHeightMapModifier[] modifiers = biome.heightModifier.GetComponents<BaseHeightMapModifier>();

                foreach (var modifier in modifiers)
                {
                    modifier.Execute(_data, biomeIndex, biome);
                }
            }

            //Initialize post processing height modifiers
            if (_config.heightPostProcessingModifier != null)
            {
                BaseHeightMapModifier[] modifiers = _config.heightPostProcessingModifier.GetComponents<BaseHeightMapModifier>();

                foreach (var modifier in modifiers)
                {
                    modifier.Execute(_data);
                }
            }

            _targetTerrain.terrainData.SetHeights(0, 0, _data.heightMap);
            _data.slopeMap = new float[_data.alphaMapResolution, _data.alphaMapResolution];

            //Generate slope map
            for (int y = 0; y < _data.alphaMapResolution; y++)
            {
                for (int x = 0; x < _data.alphaMapResolution; x++)
                {
                    for (int layerIndex = 0; layerIndex < _targetTerrain.terrainData.alphamapLayers; layerIndex++)
                    {
                        _data.slopeMap[x, y] = _targetTerrain.terrainData.GetInterpolatedNormal((float)x / _data.alphaMapResolution, (float)y / _data.alphaMapResolution).y;
                    }
                }
            }
        }

        public int GetLayerForTexture(TextureConfig textureConfig)
        {
            return _data.biomeTextureToTerrainLayerIndex[textureConfig];
        }

        public int GetDetailLayerForTerrainDetail(TerrainDetailConfig detailConfig)
        {
            return _data.biomeTerrainDetailToDetailLayerIndex[detailConfig];
        }

        private void PerformTerrainPainting()
        {
            _data.alphaMaps = _targetTerrain.terrainData.GetAlphamaps(0, 0, _data.alphaMapResolution, _data.alphaMapResolution);

            //Reset all layers
            for (int y = 0; y < _data.alphaMapResolution; y++)
            {
                for (int x = 0; x < _data.alphaMapResolution; x++)
                {
                    for (int layerIndex = 0; layerIndex < _targetTerrain.terrainData.alphamapLayers; layerIndex++)
                    {
                        _data.alphaMaps[x, y, layerIndex] = 0;
                    }
                }
            }

            //Run terrain painting for each biome
            for (int biomeIndex = 0; biomeIndex < _config.NumBiomes; biomeIndex++)
            {
                var biome = _config.biomes[biomeIndex].biome;

                if (biome.heightModifier == null)
                {
                    continue;
                }

                BaseTexturePainter[] modifiers = biome.terrainPainter.GetComponents<BaseTexturePainter>();

                foreach (var modifier in modifiers)
                {
                    modifier.Execute(_data, biomeIndex, biome);
                }
            }

            //Run texture post processing
            if (_config.paintingPostProcessingModifier != null)
            {
                BaseTexturePainter[] modifiers = _config.paintingPostProcessingModifier.GetComponents<BaseTexturePainter>();

                foreach (var modifier in modifiers)
                {
                    modifier.Execute(_data);
                }
            }

            _targetTerrain.terrainData.SetAlphamaps(0, 0, _data.alphaMaps);
        }

        private void PerformObjectPlacement()
        {
            if (DEBUG_TurnOffObjectPlacers)
            {
                return;
            }

            //Run object placement for each biome
            for (int biomeIndex = 0; biomeIndex < _config.NumBiomes; biomeIndex++)
            {
                var biome = _config.biomes[biomeIndex].biome;

                if (biome.objectPlacer == null)
                {
                    continue;
                }

                BaseObjectPlacer[] modifiers = biome.objectPlacer.GetComponents<BaseObjectPlacer>();

                foreach (var modifier in modifiers)
                {
                    modifier.Execute(_data, biomeIndex, biome);
                }
            }
        }

        private void PerformDetailPainting()
        {
            //Creating a new empty set of layers
            int numDetailLayers = _targetTerrain.terrainData.detailPrototypes.Length;
            _data.detailLayerMaps = new List<int[,]>(numDetailLayers);

            for (int layerIndex = 0; layerIndex < numDetailLayers; layerIndex++)
            {
                _data.detailLayerMaps.Add(new int[_data.detailMapResolution, _data.detailMapResolution]);
            }

            //Run terrain detail painting for each biome
            for (int biomeIndex = 0; biomeIndex < _config.NumBiomes; biomeIndex++)
            {
                var biome = _config.biomes[biomeIndex].biome;

                if (biome.detailPainter == null)
                {
                    continue;
                }

                BaseDetailPainter[] modifiers = biome.detailPainter.GetComponents<BaseDetailPainter>();

                foreach (var modifier in modifiers)
                {
                    modifier.Execute(_data, biomeIndex, biome);
                }
            }

            //Run detail painting post processing
            if (_config.detailPaintingPostProcessingModifier != null)
            {
                BaseDetailPainter[] modifiers = _config.detailPaintingPostProcessingModifier.GetComponents<BaseDetailPainter>();

                foreach (var modifier in modifiers)
                {
                    modifier.Execute(_data);
                }
            }

            //Apply detail layers
            for (int layerIndex = 0; layerIndex < numDetailLayers; layerIndex++)
            {
                _targetTerrain.terrainData.SetDetailLayer(0, 0, layerIndex, _data.detailLayerMaps[layerIndex]);
            }
        }

        private void PerformBiomeGeneration()
        {
            //Allocate the biome map and strength map
            _data.biomeMap = new byte[_data.mapResolution, _data.mapResolution];
            _data.biomeStrengths = new float[_data.mapResolution, _data.mapResolution];

            //Initialize initial height modifiers
            if (_config.biomeGenerators != null)
            {
                BaseBiomeMapGenerator[] generators = _config.biomeGenerators.GetComponents<BaseBiomeMapGenerator>();

                foreach (var generator in generators)
                {
                    generator.Execute(_data);
                }
            }
        }

#if UNITY_EDITOR
        public void RegenerateTextures()
        {
            PerformLayerSetup();
        }

        public void RegenerateDetailPrototypes()
        {
            PerformDetailPrototypeSetup();
        }

        private void PerformLayerSetup()
        {
            //Delete all existing layers
            if (_targetTerrain.terrainData.terrainLayers != null || _targetTerrain.terrainData.terrainLayers.Length > 0)
            {
                Undo.RecordObject(_targetTerrain, "Clearing old layers");

                //Build list of assets paths for every layer
                List<string> layersToDelete = new List<string>();

                foreach (var layer in _targetTerrain.terrainData.terrainLayers)
                {
                    if (layer == null)
                    {
                        continue;
                    }

                    layersToDelete.Add(AssetDatabase.GetAssetPath(layer.GetInstanceID()));
                }

                //Remove all links to layers
                _targetTerrain.terrainData.terrainLayers = null;

                //Delete each layer
                foreach (var layerFile in layersToDelete)
                {
                    if (string.IsNullOrEmpty(layerFile))
                    {
                        continue;
                    }

                    AssetDatabase.DeleteAsset(layerFile);
                }

                Undo.FlushUndoRecordObjects();
            }

            string scenePath = Path.GetDirectoryName(SceneManager.GetActiveScene().path);
            PerformTextureMappingGeneration();

            //Generate layers
            int numLayers = _data.biomeTextureToTerrainLayerIndex.Count;
            List<TerrainLayer> newLayers = new List<TerrainLayer>(numLayers);

            //Preallocate layers
            for (int layerIndex = 0; layerIndex < numLayers; layerIndex++)
            {
                newLayers.Add(new TerrainLayer());
            }

            //Iterate over texture map
            foreach (var textureMappingEntry in _data.biomeTextureToTerrainLayerIndex)
            {
                var textureConfig = textureMappingEntry.Key;
                var textureLayerIndex = textureMappingEntry.Value;
                var textureLayer = newLayers[textureLayerIndex];

                //Configure terrain layer textures
                textureLayer.diffuseTexture = textureConfig.diffuse;
                textureLayer.normalMapTexture = textureConfig.normalMap;

                //Save layer as Asset
                string layerPath = Path.Combine(scenePath, "Layer_" + textureLayerIndex);
                AssetDatabase.CreateAsset(textureLayer, $"{layerPath}.asset");
            }

            Undo.RecordObject(_targetTerrain.terrainData, "Updating Terrain layers");
            _targetTerrain.terrainData.terrainLayers = newLayers.ToArray();
        }

        private void PerformDetailPrototypeSetup()
        {
            PerformTerrainDetailMapping();

            //Build list of detail prototypes
            var detailPrototypes = new DetailPrototype[_data.biomeTerrainDetailToDetailLayerIndex.Count];

            foreach (var kvp in _data.biomeTerrainDetailToDetailLayerIndex)
            {
                TerrainDetailConfig detailData = kvp.Key;
                int layerIndex = kvp.Value;
                DetailPrototype newDetail = new DetailPrototype();

                //Check if is a mesh
                if (detailData.detailPrefab)
                {
                    newDetail.prototype = detailData.detailPrefab;
                    newDetail.renderMode = DetailRenderMode.VertexLit;
                    newDetail.usePrototypeMesh = true;
                    newDetail.useInstancing = true;
                }
                else
                {
                    newDetail.prototypeTexture = detailData.billboardTexture;
                    newDetail.renderMode = DetailRenderMode.GrassBillboard;
                    newDetail.usePrototypeMesh = false;
                    newDetail.useInstancing = false;
                    newDetail.healthyColor = detailData.healthyColor;
                    newDetail.dryColor = detailData.dryColor;
                }

                //Transfer general data
                newDetail.minWidth = detailData.minWidth;
                newDetail.maxWidth = detailData.maxWidth;
                newDetail.minHeight = detailData.minHeight;
                newDetail.maxHeight = detailData.maxHeight;
                newDetail.noiseSeed = detailData.noiseSeed;
                newDetail.noiseSpread = detailData.noiseSpread;
                newDetail.holeEdgePadding = detailData.holeEdgePadding;


                //Check prototype
                string errorMessage;

                if (!newDetail.Validate(out errorMessage))
                {
                    throw new System.InvalidOperationException(errorMessage);
                }

                detailPrototypes[layerIndex] = newDetail;
            }

            //Update detail prototypes
            Undo.RecordObject(_targetTerrain.terrainData, "Updating Detail Prototypes");
            _targetTerrain.terrainData.detailPrototypes = detailPrototypes;
            _targetTerrain.terrainData.RefreshPrototypes();
        }
#endif
    }
}