using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Zenject;

namespace Tetra4bica.Init
{

    public class PoolsWarmer : MonoBehaviour
    {

        [Inject(Id = PoolId.GAME_CELLS)]
        IObjectPool<GameObject> tableCellPool;
        [SerializeField]
        private ushort tableCellWarmNumber = 10 * 20;

        void Start()
        {
            List<GameObject> cellList = new List<GameObject>();
            doInLoop(() => cellList.Add(tableCellPool.Get()), tableCellWarmNumber);
            doForEach(cellList, (go) => tableCellPool.Release(go));
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
