// Copyright 2025 Spellbound Studio Inc.

using UnityEngine;

namespace Spellbound.MarchingCubes {
    public interface IVoxelVolume {
        public IChunk GetChunkByVoxelPosition(Vector3Int position);
        
        public IChunk GetChunkByWorldPosition(Vector3 position);

        public IChunk GetChunkByCoord(Vector3Int coord);
        
        public Vector3Int WorldToVoxelSpace(Vector3 worldPosition);

        public Transform GetTransform();
    }
}