// Copyright 2025 Spellbound Studio Inc.

using System;
using Spellbound.Core;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Spellbound.MarchingCubes {
    /// <summary>
    /// Recursively Subdividing OctreeNode to subdivide a chunk at varying LODs.
    /// Either it has 8 children, or it has an Octree leaf (representing actual terrain).
    /// </summary>
    public class OctreeNode : IDisposable {
        private OctreeNode[] _children;
        private bool IsLeaf => _children == null;
        private GameObject _leafGo;
        private GameObject _transitionGo;
        private int _activeTransitionMask;
        private NativeList<MeshingVertexData> _transitionVertices;
        private NativeList<int> _transitionTriangles;
        private NativeArray<int2> _transitionRanges;
        private Vector3Int _localPosition;
        private readonly int _lod;
        private Bounds _bounds;
        private readonly IVoxelTerrainChunk _chunk;
        private readonly MarchingCubesManager _mcManager;
        private NativeArray<VoxelData> VoxelData => _chunk.GetVoxelArray();

        private Vector3Int WorldPosition => _chunk.GetChunkCoord() * SpellboundStaticHelper.ChunkSize;

        public OctreeNode(Vector3Int localPosition, int lod, IVoxelTerrainChunk chunk) {
            _localPosition = localPosition;
            _lod = lod;
            _chunk = chunk;

            var octreeSize = McStaticHelper.CubesMarchedPerOctreeLeaf * math.pow(2, _lod) + 2;

            _bounds = new Bounds(WorldPosition + _localPosition + Vector3.one * octreeSize / 2,
                Vector3.one * octreeSize);

            _mcManager = SingletonManager.GetSingletonInstance<MarchingCubesManager>();
        }

        public void ValidateOctreeEdits(Bounds bounds) {
            if (!bounds.Intersects(_bounds)) return;

            if (IsLeaf) {
                UpdateLeaf();

                return;
            }

            foreach (var child in _children)
                child.ValidateOctreeEdits(bounds);
        }

        private (int, int) GetLodRange(Vector3 octreePos, Vector3 playerPos) {
            var distance = Vector3.Distance(octreePos, playerPos);
            var coarsestLod = McStaticHelper.GetCoarsestLod(distance, _mcManager.lodRanges);
            var finestLod = McStaticHelper.GetFinestLod(distance, _mcManager.lodRanges);

            return (coarsestLod, finestLod);
        }

        public void ValidateOctreeLods(Vector3 playerPosition) {
            var octreePos = WorldPosition
                            + _localPosition
                            + Vector3.one * (McStaticHelper.CubesMarchedPerOctreeLeaf << (_lod - 1));
            var (coarsestLod, finestLod) = GetLodRange(octreePos, playerPosition);

            if (_chunk.IsChunkAllOneSideOfThreshold()) return;

            if (_lod <= finestLod) {
                MakeLeaf();

                return;
            }

            if (_lod == coarsestLod && IsLeaf && _leafGo == null) {
                MakeLeaf();

                return;
            }

            if (_lod > coarsestLod)
                Subdivide();

            if (IsLeaf)
                return;

            foreach (var child in _children)
                child.ValidateOctreeLods(playerPosition);
        }

        private void MarchAndUpdateLeaf() {
            var marchingCubeJob = new MarchingCubeJob {
                Tables = _mcManager.McTablesBlob,
                VoxelArray = VoxelData,

                // New Allocation - Ensure this is disposed of after the job.
                Vertices = new NativeList<MeshingVertexData>(Allocator.TempJob),
                // New Allocation - Ensure this is disposed of after the job.
                Triangles = new NativeList<int>(Allocator.TempJob),
                Lod = _lod,
                Start = new int3(_localPosition.x, _localPosition.y, _localPosition.z)
            };
            var jobHandle = marchingCubeJob.Schedule();
            jobHandle.Complete();
            UpdateLeaf(marchingCubeJob.Vertices, marchingCubeJob.Triangles);

            marchingCubeJob.Vertices.Dispose();
            marchingCubeJob.Triangles.Dispose();

            if (_lod != 0) {
                var transitionMarchingCubeJob = new TransitionMarchingCubeJob {
                    Tables = _mcManager.McTablesBlob,
                    VoxelArray = VoxelData,

                    // New Allocation - Ensure this is disposed of after the job.
                    TransitionMeshingVertexData = new NativeList<MeshingVertexData>(Allocator.TempJob),

                    TransitionTriangles = new NativeList<int>(Allocator.TempJob),
                    TransitionRanges = new NativeArray<int2>(6, Allocator.TempJob),

                    Lod = _lod,
                    Start = new int3(_localPosition.x, _localPosition.y, _localPosition.z)
                };

                var transitionJobHandle = transitionMarchingCubeJob.Schedule();
                transitionJobHandle.Complete();
                _activeTransitionMask = 0;
                _transitionVertices.CopyFrom(transitionMarchingCubeJob.TransitionMeshingVertexData);
                _transitionTriangles.CopyFrom(transitionMarchingCubeJob.TransitionTriangles);
                _transitionRanges.CopyFrom(transitionMarchingCubeJob.TransitionRanges);
                UpdateTransition(_activeTransitionMask);
                transitionMarchingCubeJob.TransitionMeshingVertexData.Dispose();
                transitionMarchingCubeJob.TransitionTriangles.Dispose();
                transitionMarchingCubeJob.TransitionRanges.Dispose();
            }
        }

        private void BroadcastNewLeaf() {
            var neighborPositions = GetFaceCenters();
            for (var i = 0; i < 6; i++) _chunk.BroadcastNewLeaf(this, neighborPositions[i], i);
        }

        public void ValidateTransition(
            OctreeNode neighbor, Vector3 facePos, McStaticHelper.TransitionFaceMask faceMask) {
            if (!_bounds.Contains(facePos))
                return;

            if (!IsLeaf) {
                foreach (var child in _children)
                    child.ValidateTransition(neighbor, facePos, faceMask);

                return;
            }

            if (_lod > neighbor._lod) {
                UpdateTransitionMask(GetOppositeTransition(faceMask), true);
                neighbor.UpdateTransitionMask(faceMask, false);

                return;
            }

            if (_lod == neighbor._lod) {
                UpdateTransitionMask(GetOppositeTransition(faceMask), false);
                neighbor.UpdateTransitionMask(faceMask, false);

                return;
            }

            UpdateTransitionMask(GetOppositeTransition(faceMask), false);
            neighbor.UpdateTransitionMask(faceMask, true);
        }

        private void UpdateTransitionMask(McStaticHelper.TransitionFaceMask mask, bool isSetter) {
            var newTransitionMask = _activeTransitionMask;

            if (isSetter)
                newTransitionMask |= (int)mask;
            else
                newTransitionMask &= ~(int)mask;

            if (newTransitionMask == _activeTransitionMask)
                return;

            _activeTransitionMask = newTransitionMask;
            UpdateTransition(_activeTransitionMask);
        }

        private Vector3[] GetFaceCenters() =>
                new[] {
                    new Vector3(_bounds.min.x - 1, _bounds.center.y, _bounds.center.z),
                    new Vector3(_bounds.center.x, _bounds.min.y - 1, _bounds.center.z),
                    new Vector3(_bounds.center.x, _bounds.center.y, _bounds.min.z - 1),
                    new Vector3(_bounds.max.x + 1, _bounds.center.y, _bounds.center.z),
                    new Vector3(_bounds.center.x, _bounds.max.y + 1, _bounds.center.z),
                    new Vector3(_bounds.center.x, _bounds.center.y, _bounds.max.z + 1)
                };

        private McStaticHelper.TransitionFaceMask GetOppositeTransition(
            McStaticHelper.TransitionFaceMask transitionMask) =>
                transitionMask switch {
                    McStaticHelper.TransitionFaceMask.XMin => McStaticHelper.TransitionFaceMask.XMax,
                    McStaticHelper.TransitionFaceMask.YMin => McStaticHelper.TransitionFaceMask.YMax,
                    McStaticHelper.TransitionFaceMask.ZMin => McStaticHelper.TransitionFaceMask.ZMax,
                    McStaticHelper.TransitionFaceMask.XMax => McStaticHelper.TransitionFaceMask.XMin,
                    McStaticHelper.TransitionFaceMask.YMax => McStaticHelper.TransitionFaceMask.YMin,
                    McStaticHelper.TransitionFaceMask.ZMax => McStaticHelper.TransitionFaceMask.ZMin,
                    _ => McStaticHelper.TransitionFaceMask.XMin
                };

        private void Subdivide() {
            if (!IsLeaf || _children != null)
                return;

            if (_leafGo != null) {
                Object.Destroy(_leafGo);
                _leafGo = null;
                Object.Destroy(_transitionGo);
                _transitionGo = null;
            }

            _children = new OctreeNode[8];
            var childLod = _lod - 1;
            var childSize = McStaticHelper.CubesMarchedPerOctreeLeaf << childLod;

            for (var i = 0; i < 8; i++) {
                var offset = new Vector3Int(
                    (i & 1) == 0 ? 0 : childSize,
                    (i & 2) == 0 ? 0 : childSize,
                    (i & 4) == 0 ? 0 : childSize
                );

                _children[i] = new OctreeNode(_localPosition + offset, childLod, _chunk);
            }
        }

        private void MakeLeaf() {
            if (_leafGo != null) return;

            if (!IsLeaf) {
                for (var i = 0; i < 8; i++) _children[i]?.Dispose();
                _children = null;
            }

            BuildLeaf();
            BuildTransitions();
            MarchAndUpdateLeaf();
            BroadcastNewLeaf();
        }

        private void UpdateLeaf() {
            if (_leafGo == null) return;

            MarchAndUpdateLeaf();
        }

        public void Dispose() {
            if (_children != null) {
                for (var i = 0; i < 8; i++) _children[i].Dispose();
                _children = null;
            }

            if (_leafGo != null) {
                Object.Destroy(_leafGo);
                _leafGo = null;
                Object.Destroy(_transitionGo);
                _transitionGo = null;
            }

            if (_transitionVertices.IsCreated)
                _transitionVertices.Dispose();

            if (_transitionTriangles.IsCreated)
                _transitionTriangles.Dispose();

            if (_transitionRanges.IsCreated)
                _transitionRanges.Dispose();
        }

        private void UpdateLeaf(NativeList<MeshingVertexData> vertices, NativeList<int> triangles) {
            if (triangles.Length < 3 || vertices.Length < 3)
                return;

            var meshFilter = _leafGo.GetComponent<MeshFilter>();
            var meshCollider = _leafGo.GetComponent<MeshCollider>();

            var mesh = new Mesh();

            mesh.SetVertexBufferParams(vertices.Length, MeshingVertexData.VertexBufferMemoryLayout);

            mesh.SetVertexBufferData(
                vertices.AsArray(),
                0,
                0,
                vertices.Length,
                0,
                MeshUpdateFlags.DontValidateIndices
            );

            mesh.SetIndexBufferParams(triangles.Length, IndexFormat.UInt32);

            mesh.SetIndexBufferData(
                triangles.AsArray(),
                0,
                0,
                triangles.Length,
                MeshUpdateFlags.DontValidateIndices
            );

            var subMesh = new SubMeshDescriptor(0, triangles.Length);
            mesh.subMeshCount = 1;

            mesh.SetSubMesh(0, subMesh);
            mesh.RecalculateBounds();

            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = mesh;
        }

        private void BuildLeaf() {
            _leafGo = Object.Instantiate(
                _mcManager.octreePrefab,
                WorldPosition,
                Quaternion.identity,
                _chunk.GetChunkTransform()
            );

            _leafGo.name = $"LeafSize {McStaticHelper.CubesMarchedPerOctreeLeaf << _lod} " +
                           $"at {_localPosition.x}, {_localPosition.y}, {_localPosition.z}";

            if (!_transitionVertices.IsCreated)
                _transitionVertices = new NativeList<MeshingVertexData>(Allocator.Persistent);

            if (!_transitionTriangles.IsCreated) _transitionTriangles = new NativeList<int>(Allocator.Persistent);

            if (!_transitionRanges.IsCreated) _transitionRanges = new NativeArray<int2>(6, Allocator.Persistent);
        }

        private void BuildTransitions() {
            _transitionGo = Object.Instantiate(
                _mcManager.octreePrefab,
                WorldPosition,
                Quaternion.identity,
                _chunk.GetChunkTransform()
            );

            _transitionGo.name = "transition";
            _transitionGo.transform.parent = _leafGo.transform;
        }

        private void UpdateTransition(int transitionMask) {
            if (!_transitionVertices.IsCreated)
                return;

            var meshFilter = _transitionGo.GetComponent<MeshFilter>();

            if (transitionMask == 0) {
                meshFilter.mesh = null;

                return;
            }

            var triangles =
                    GetFilteredTransitionTriangles(_transitionTriangles, _transitionRanges, transitionMask);

            if (triangles.Length < 3 || _transitionVertices.Length < 3)
                return;

            meshFilter = _transitionGo.GetComponent<MeshFilter>();

            var mesh = new Mesh();

            mesh.SetVertexBufferParams(_transitionVertices.Length, MeshingVertexData.VertexBufferMemoryLayout);

            mesh.SetVertexBufferData(
                _transitionVertices.AsArray(),
                0,
                0,
                _transitionVertices.Length,
                0,
                MeshUpdateFlags.DontValidateIndices
            );

            mesh.SetIndexBufferParams(triangles.Length, IndexFormat.UInt32);

            mesh.SetIndexBufferData(
                triangles.AsArray(),
                0,
                0,
                triangles.Length,
                MeshUpdateFlags.DontValidateIndices
            );

            var subMesh = new SubMeshDescriptor(0, triangles.Length);
            mesh.subMeshCount = 1;

            mesh.SetSubMesh(0, subMesh);
            mesh.RecalculateBounds();

            meshFilter.mesh = mesh;
            //meshCollider.sharedMesh = mesh;
        }

        private NativeList<int> GetFilteredTransitionTriangles(
            NativeList<int> allTriangles, NativeArray<int2> triangleRanges,
            int transitionMask) {
            var filteredTriangles = new NativeList<int>(Allocator.Temp);

            for (var i = 0; i < 6; i++) {
                if ((transitionMask & (1 << i)) == 0) continue;

                var range = triangleRanges[i];

                if (range.x < 0 || range.y > allTriangles.Length || range.x > range.y) continue;

                for (var j = range.x; j < range.y; j++) filteredTriangles.Add(allTriangles[j]);
            }

            return filteredTriangles;
        }
    }
}