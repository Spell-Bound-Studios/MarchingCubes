using UnityEngine;

namespace Spellbound.MarchingCubes {
    [CreateAssetMenu(menuName = "Spellbound/MarchingCubes/TerrainSettings")]
    public class TerrainConfig : ScriptableObject {
        public int resolution = 1;
        public int viewDistance = 700;
        public int maxChunkSize = 128;
        public int maxLods = 3;
        public int cubesPerMarch = 16;
        public float marchingCubeDensityThreshold = 0.5f;
        
        [Space(10)]
        public bool useBuiltInMap = true;
        public BuiltInMap builtInMap;
        
        [System.Serializable]
        public class BuiltInMap {
            public Vector3Int mapSize = new Vector3Int(384, 128, 384);
            public MapStartState startState = MapStartState.HalfFull;
            public MapEdgeConstraints top = MapEdgeConstraints.Open;
            public MapEdgeConstraints bottom = MapEdgeConstraints.Closed;
            public MapEdgeConstraints sides = MapEdgeConstraints.Closed;
        }
        
        public enum MapEdgeConstraints {
            Open,
            Closed,
            Unconstrained
        }
        
        public enum MapStartState {
            Full,
            HalfFull,
            Empty
        }
    }
}

