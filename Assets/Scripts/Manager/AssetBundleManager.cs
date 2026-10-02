using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Manager
{
    public class AssetBundleContainer
	{
	    private AssetBundle _thisAssetBundle;
		private string _bundleName; // used for more readable debug messages
		private Object _object;
		/// <summary>
		/// Gets or sets the this asset bundle.
		/// </summary>
	    public AssetBundle ThisAssetBundle
	    {
	        get
	        {
	            return _thisAssetBundle;
	        }
	        set
	        {
	            _thisAssetBundle = value;
	        }
	    }

		public Object Object
		{
			get => _object;
			set => _object = value;
		}

		/// <summary>
		/// Gets or sets the name of the bundle.
		/// </summary>
		/// <value>
		/// The name of the bundle.
		/// </value>
		public string BundleName
		{
			get
			{
				return _bundleName;	
			}
			set
			{
				_bundleName = value;	
			}
		}

		/// <summary>
	    /// Unloads the assetBundle
	    /// </summary>
	    public void Unload()
	    {
	        Debug.Log("Unloading AssetBundle(true):" + _bundleName);
	        _thisAssetBundle.Unload(true);
	    }
	}
    public class AssetBundleManager : MonoBehaviour
    {
        public static AssetBundleManager Instance { get; private set; }
        
        private readonly Dictionary<string, AssetBundleContainer> _assetBundles = new Dictionary<string, AssetBundleContainer>();


        private void Awake()
        {
	        Instance = this;
        }
        
        /// <summary>
        /// Adds the bundle for removal management, if no gameobjects are using the assetbundle it will be
        /// removed automatically(if you use this method for all objects created from asset bundles)
        /// </summary>
        public AssetBundleContainer AddBundle(string bundleName, AssetBundle assetBundle,Object obj)
        {
	        var bundle = GetAssetBundle(bundleName);
	        //Check if the assetbundle already has a container in the dictionary
	        if (!_assetBundles.ContainsKey(bundleName))
	        {
		        //Create a new container and store the referenced game object
		        AssetBundleContainer bundleContainer = new AssetBundleContainer();
		        bundleContainer.ThisAssetBundle = assetBundle;
		        bundleContainer.BundleName = bundleName;
		        bundleContainer.Object = obj;
		        _assetBundles.Add(bundleName, bundleContainer);

		        bundle = bundleContainer;
	        }

	        return bundle;
        }
        
        /// <summary>
        /// Gets the asset bundle for the specified key.
        /// </summary>
        /// <returns>
        /// The asset bundle.
        /// </returns>
        /// <param name='bundleName'>
        /// Bundle name key.
        /// </param>
        public AssetBundleContainer GetAssetBundle(string bundleName)
        {
	        AssetBundleContainer thisBundle = null;
	        _assetBundles.TryGetValue(bundleName, out thisBundle);

	        return thisBundle;
        }

        public Object GetAssetObject(string bundleName)
        {
	        var asset = GetAssetBundle(bundleName);

	        if (asset == null)
	        {
		        Debug.LogError("failed to load object");
		        return null;
	        }

	        return asset.Object;
        }
        
        /// <summary>
        /// Destroys and unloads an asset bundle and all its referenced objects with
        /// the specified key.
        /// </summary>
        /// <param name='bundleName'>
        /// Bundle name.
        /// </param>
        public void UnloadAssetBundle(string bundleName)
        {
	        AssetBundleContainer thisBundle = null;
	        _assetBundles.TryGetValue(bundleName, out thisBundle);
	        if (thisBundle != null)
	        {
		        thisBundle.Unload();
		        _assetBundles.Remove(bundleName);
	        }
        }
        
        /// <summary>
        /// Destroy and unload all asset bundles at once and all of their referenced objects.
        /// </summary>
        public void UnloadAllBundles()
        {
	        foreach (KeyValuePair<string, AssetBundleContainer> bundle in _assetBundles)
	        {
		        bundle.Value.Unload();
	        }
	        _assetBundles.Clear();
        }

    }
}
