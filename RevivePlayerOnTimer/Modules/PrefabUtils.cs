// LethalLib/LethalLib/Modules/PrefabUtils.cs
// https://github.com/EvaisaDev/LethalLib
//MIT License

//Copyright (c) 2024 Evaisa

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

#region

using System;
using UnityEngine;
using Object = UnityEngine.Object;

#endregion

namespace RevivePlayerOnTimer.Modules
{

    public class PrefabUtils
    {
        internal static Lazy<GameObject> _prefabParent;
        internal static GameObject prefabParent { get { return _prefabParent.Value; } }

        static PrefabUtils()
        {
            _prefabParent = new Lazy<GameObject>(() =>
            {
                var parent = new GameObject("LethalLibGeneratedPrefabs");
                parent.hideFlags = HideFlags.HideAndDontSave;
                parent.SetActive(false);

                return parent;
            });
        }

        /// <summary>
        /// Clones a prefab and returns the clone.
        /// </summary>
        public static GameObject ClonePrefab(GameObject prefabToClone, string newName = null)
        {
            var prefab = Object.Instantiate(prefabToClone, prefabParent.transform);
            prefab.hideFlags = HideFlags.HideAndDontSave;

            if (newName != null)
            {
                prefab.name = newName;
            }
            else
            {
                prefab.name = prefabToClone.name;
            }

            return prefab;
        }

        /// <summary>
        /// Creates a prefab and returns it.
        /// </summary>
        public static GameObject CreatePrefab(string name)
        {
            var prefab = new GameObject(name);
            prefab.hideFlags = HideFlags.HideAndDontSave;

            prefab.transform.SetParent(prefabParent.transform);

            return prefab;
        }
    }
}