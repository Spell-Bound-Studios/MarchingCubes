// Copyright 2025 Spellbound Studio Inc.

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Spellbound.MarchingCubes {
    /// <summary>
    /// Defines the contract that a chunk must fulfill to integrate with the Marching Cubes Voxel System.
    /// </summary>
    public interface IChunk {
        VoxChunk VoxelChunk { get; }

        public void InitializeChunk(NativeList<SparseVoxelData> voxels); //polymorphic
        
        public void PassVoxelEdits(List<VoxelEdit> newVoxelEdits); //polymorphic
        
    }
}