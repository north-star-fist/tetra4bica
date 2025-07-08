using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace Sergei.Safonov.Unity {

    public static class SceneExt {
        public static bool TryGetRootGameObject<T>(this Scene scene, out T result)
        {
            result = default;
            foreach (GameObject gameObject in scene.GetRootGameObjects()) {
                if (gameObject.TryGetComponent(out T component)) {
                    result = component;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets components of root objects of a scene that are of specific type.
        /// The functions take one component per game object, so if an object has more than
        /// one component of the specified type - one of them will be added to result list, other
        /// will be ignored.
        /// </summary>
        /// <seealso cref="GetRootComponents{T}(Scene, List{T})"/>
        public static void GetRootGameObjects<T>(this Scene scene, List<T> resultList) where T : UnityEngine.Object
        {
            foreach (GameObject gameObject in scene.GetRootGameObjects()) {
                if (gameObject.TryGetComponent(out T component)) {
                    resultList.Add(component);
                }
            }
        }


        /// <summary>
        /// Gets all components of root objects of a scene that are of specific type.
        /// The functions take all components of root game objects, so if a root object has more than
        /// one component of the specified type - all of them will be added to result list.
        /// </summary>
        /// <seealso cref="GetRootGameObjects{T}(Scene, List{T})"/>
        public static void GetRootComponents<T>(this Scene scene, List<T> resultList)
        {
            var tempList = ListPool<T>.Get();
            foreach (GameObject gameObject in scene.GetRootGameObjects())
            {
                gameObject.GetComponents<T>(tempList);
                resultList.AddRange(tempList);
            }
            ListPool<T>.Release(tempList);
        }
    }
}
