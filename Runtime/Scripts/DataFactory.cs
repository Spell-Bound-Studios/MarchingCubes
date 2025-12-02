// Copyright 2025 Spellbound Studio Inc.

using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spellbound.MarchingCubes {
    public abstract class DataFactory : ScriptableObject {
        public abstract void FillDataArray(Vector3Int chunkCoord, 
            BlobAssetReference<VolumeConfigBlobAsset> configBlob, 
            NativeArray<VoxelData> data);
    }
}