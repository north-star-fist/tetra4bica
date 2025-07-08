using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;
using VContainer;

namespace Tetra4bica.Init
{

    public class PoolsWarmer : MonoBehaviour
    {
        [Inject]
        private IGameObjectPoolManager _poolManager;
        [SerializeField, FormerlySerializedAs("tableCellWarmNumber")]
        private ushort _tableCellWarmNumber = 10 * 20;

        IObjectPool<GameObject> _tableCellPool;

        void Start()
        {
            _tableCellPool = _poolManager.GetPool(PoolId.GAME_CELLS);
            List<GameObject> cellList = new List<GameObject>();
            doInLoop(() => cellList.Add(_tableCellPool.Get()), _tableCellWarmNumber);
            doForEach(cellList, (go) => _tableCellPool.Release(go));
        }

        void doInLoop(Action action, ushort times)
        {
            for (int i = 0; i < times; i++)
            {
                action();
            }
        }

        void doForEach<T>(IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
            {
                action(item);
            }
        }
    }
}
