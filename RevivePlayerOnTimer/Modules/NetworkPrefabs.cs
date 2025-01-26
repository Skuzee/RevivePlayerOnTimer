// LethalLib/LethalLib/Modules/NetworkPrefabs.cs
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
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Unity.Netcode;
using UnityEngine;

#endregion

namespace RevivePlayerOnTimer.Modules
{
    public class NetworkPrefabs
    {

        private static List<GameObject> _networkPrefabs = new List<GameObject>();

        /// <summary>
        /// Registers a prefab to be added to the network manager.
        /// </summary>
        public static void RegisterNetworkPrefab(GameObject prefab)
        {
            if (prefab is null)
                throw new ArgumentNullException(nameof(prefab), $"The given argument for {nameof(RegisterNetworkPrefab)} is null!");
            if (!_networkPrefabs.Contains(prefab))
                _networkPrefabs.Add(prefab);
        }

        /// <summary>
        /// Creates a network prefab programmatically and registers it with the network manager.
        /// Credit to Day and Xilo.
        /// </summary>
        public static GameObject CreateNetworkPrefab(string name)
        {
            var prefab = PrefabUtils.CreatePrefab(name);
            prefab.AddComponent<NetworkObject>();

            var hash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(Assembly.GetCallingAssembly().GetName().Name + name));

            prefab.GetComponent<NetworkObject>().GlobalObjectIdHash = BitConverter.ToUInt32(hash, 0);

            RegisterNetworkPrefab(prefab);
            return prefab;
        }

        /// <summary>
        /// Clones a network prefab programmatically and registers it with the network manager.
        /// Credit to Day and Xilo.
        /// </summary>
        public static GameObject CloneNetworkPrefab(GameObject prefabToClone, string newName = null)
        {
            var prefab = PrefabUtils.ClonePrefab(prefabToClone, newName);

            var hash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(Assembly.GetCallingAssembly().GetName().Name + prefab.name));

            prefab.GetComponent<NetworkObject>().GlobalObjectIdHash = BitConverter.ToUInt32(hash, 0);

            RegisterNetworkPrefab(prefab);
            return prefab;
        }


        //private static void GameNetworkManager_Start(On.GameNetworkManager.orig_Start orig, GameNetworkManager self)
        //{
        //    orig(self);

        //    foreach (GameObject obj in _networkPrefabs)
        //    {
        //        if (!NetworkManager.Singleton.NetworkConfig.Prefabs.Contains(obj))
        //            NetworkManager.Singleton.AddNetworkPrefab(obj);
        //    }

        //}
    }
}