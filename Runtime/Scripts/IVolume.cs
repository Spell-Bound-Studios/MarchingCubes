// Copyright 2025 Spellbound Studio Inc.

using Unity.Entities;
using UnityEngine;

namespace Spellbound.MarchingCubes {
    public interface IVolume {
        Vector2[] ViewDistanceLodRanges { get; }
        
        Transform VolumeTransform { get; }

        Transform LodTarget { get; }
        
        bool IsMoving { get; set; }
        
        BlobAssetReference<VolumeConfigBlobAsset> ConfigBlob { get; }

        Awaitable ValidateChunkLods();

        bool IntersectsVolume(Bounds voxelBounds);
        
        public Vector3Int WorldToVoxelSpace(Vector3 worldPosition);

        public IChunk GetChunkByCoord(Vector3Int coord);

        public IChunk GetChunkByWorldPosition(Vector3 worldPos);

        public IChunk GetChunkByVoxelPosition(Vector3Int voxelPos);

        public Vector3Int GetCoordByVoxelPosition(Vector3Int voxelPos);
    }
}