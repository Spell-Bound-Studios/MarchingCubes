// Copyright 2025 Spellbound Studio Inc.

using UnityEngine;

namespace Spellbound.MarchingCubes {
    public interface IVoxelVolume {
        public IVoxelTerrainChunk GetChunkByVoxelPosition(Vector3Int position);
        
        public IVoxelTerrainChunk GetChunkByWorldPosition(Vector3 position);

        public IVoxelTerrainChunk GetChunkByCoord(Vector3Int coord);
        
        public Vector3Int WorldToVoxelSpace(Vector3 worldPosition);

        public Transform GetTransform();
    }
}