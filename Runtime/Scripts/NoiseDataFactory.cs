// Copyright 2025 Spellbound Studio Inc.

using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spellbound.MarchingCubes {
    [CreateAssetMenu(menuName = "Spellbound/MarchingCubes/DataFactory/NoiseDataFactory")]
    public class NoiseDataFactory : DataFactory {
        public override void FillDataArray(
            Vector3Int chunkCoord,
            BlobAssetReference<VolumeConfigBlobAsset> configBlob,
            NativeArray<VoxelData> data) {
            
        }
    }
}