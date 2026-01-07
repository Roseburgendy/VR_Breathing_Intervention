using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.EffectModules
{
    public class TerrainTreeMaterialDriver : MonoBehaviour
    {
        [SerializeField] private Terrain terrain;
        [SerializeField] private bool cloneTerrainDataAtRuntime = true;

        private TerrainData _originalData;
        private TerrainData _runtimeData;

        private readonly List<GameObject> _runtimePrototypeInstances = new();

        public TerrainData RuntimeData => _runtimeData != null ? _runtimeData : terrain != null ? terrain.terrainData : null;

        void Awake()
        {
            if (terrain == null) terrain = FindObjectOfType<Terrain>();
            if (terrain == null) return;

            if (!Application.isPlaying) return;

            if (cloneTerrainDataAtRuntime)
            {
                _originalData = terrain.terrainData;
                _runtimeData = Instantiate(_originalData);
                _runtimeData.name = _originalData.name + "_RUNTIME_CLONE";
                terrain.terrainData = _runtimeData;
            }
            else
            {
                _runtimeData = terrain.terrainData;
            }

            CloneTreePrototypesToRuntimeInstances();
        }

        private void CloneTreePrototypesToRuntimeInstances()
        {
            if (_runtimeData == null) return;

            var prototypes = _runtimeData.treePrototypes;
            if (prototypes == null || prototypes.Length == 0) return;

            for (int i = 0; i < prototypes.Length; i++)
            {
                var srcPrefab = prototypes[i].prefab;
                if (srcPrefab == null) continue;

                // Clone prefab instance for runtime (do NOT touch asset)
                var inst = Instantiate(srcPrefab);
                inst.name = srcPrefab.name + "_TP_RUNTIME";
                inst.hideFlags = HideFlags.DontSave; // prevents scene saving
                inst.SetActive(false); // not required to be active for material edits

                prototypes[i].prefab = inst;
                _runtimePrototypeInstances.Add(inst);
            }

            _runtimeData.treePrototypes = prototypes;

            // Force terrain to rebuild tree instances
            terrain.Flush();
        }

        public IReadOnlyList<GameObject> GetRuntimePrototypeInstances() => _runtimePrototypeInstances;

        void OnDestroy()
        {
            if (!Application.isPlaying) return;

            // Restore original data (extra safe)
            if (terrain != null && _originalData != null)
                terrain.terrainData = _originalData;

            for (int i = 0; i < _runtimePrototypeInstances.Count; i++)
            {
                if (_runtimePrototypeInstances[i] != null)
                    Destroy(_runtimePrototypeInstances[i]);
            }

            if (_runtimeData != null && _runtimeData != _originalData)
                Destroy(_runtimeData);
        }
    }
}
