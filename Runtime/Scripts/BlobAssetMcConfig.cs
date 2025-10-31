using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Spellbound.MarchingCubes {
    /// <summary>
    /// Blob Asset to hold Marching Cubes Settings.
    /// </summary>
    public struct McConfigBlobAsset {
        public int ChunkSize;
        public int LevelsOfDetail;
        public int CubesMarchedPerOctreeLeaf;
        public int ChunkDataWidthSize;
        public int ChunkDataAreaSize;
        public int ChunkDataVolumeSize;
        public Vector3Int ChunkCenter;
        public Vector3Int ChunkExtents;
        public byte DensityThreshold;
    }

    public static class McConfigBlobCreator {
        public static BlobAssetReference<McConfigBlobAsset>
                CreateMcSettingsBlobAsset(TerrainConfig terrainConfig) {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var config = ref builder.ConstructRoot<McConfigBlobAsset>();

            // TODO: max may not be used if they clash with eachother or with this max view distance
            config.ChunkSize = terrainConfig.maxChunkSize;
            config.LevelsOfDetail = terrainConfig.maxLods;
            
            config.CubesMarchedPerOctreeLeaf = terrainConfig.cubesPerMarch;
            config.ChunkDataWidthSize = config.ChunkSize + 3;
            config.ChunkDataAreaSize = config.ChunkDataWidthSize * config.ChunkDataWidthSize;
            config.ChunkDataVolumeSize = config.ChunkDataAreaSize * config.ChunkDataWidthSize;
            config.ChunkCenter = Vector3Int.one * (1 + config.ChunkSize / 2);
            config.ChunkExtents = Vector3Int.one * config.ChunkSize;
            config.DensityThreshold = 128;
            
            var result = builder.CreateBlobAssetReference<McConfigBlobAsset>(Allocator.Persistent);
            builder.Dispose();

            return result;
        }
    }
}