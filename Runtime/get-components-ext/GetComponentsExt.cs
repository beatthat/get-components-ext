using System;
using System.Collections.Generic;
using BeatThat.Pools;
using UnityEngine;

namespace BeatThat.GetComponentsExt
{
    /// <summary>
    /// ext methods for GetComponent[s] with options beyond what Unity's GetComponent methods support
    /// </summary>
    public static class Ext
	{
		public static Component AddIfMissing(this Component targetObject, System.Type concreteType)
		{
			return targetObject.gameObject.AddIfMissing(concreteType, concreteType);
		}

		public static Component AddIfMissing(this GameObject targetObject, System.Type concreteType)
		{
			return targetObject.AddIfMissing(concreteType, concreteType);
		}

		public static bool DestroyIfPresent<T>(this GameObject targetObject)
		{
			return DestroyIfPresent (targetObject, typeof(T));
		}

		public static bool DestroyIfPresent(this GameObject targetObject, System.Type compType)
		{
			using (var toDestroy = ListPool<Component>.Get ()) {
				targetObject.GetComponents (compType, toDestroy);

				if (toDestroy.Count == 0) {
					return false;
				}

				foreach (var c in toDestroy) {
					if (Application.isPlaying) {
						GameObject.Destroy (c);
					} else {
						GameObject.DestroyImmediate (c);
					}
				}

				return true;
			}
		}

		public static Component AddIfMissing(this Component targetObject, System.Type interfaceType, System.Type concreteType)
		{
			return targetObject.gameObject.AddIfMissing(interfaceType, concreteType);
		}

		public static Component AddIfMissing(this GameObject targetObject, System.Type interfaceType, System.Type concreteType)
		{
			foreach(Component c in targetObject.GetComponents<Component>()) {
				if(interfaceType.IsInstanceOfType(c)) {
					return c;
				}
			}

			return targetObject.AddComponent(concreteType) as Component;
		}

		public static T AddIfMissing<T>(this Component c)
			where T : Component
		{
			return c.gameObject.AddIfMissing<T, T>();
		}

		public static I AddIfMissing<I, T>(this Component c)
			where I : class
			where T : Component, I
		{
			return c.gameObject.AddIfMissing<I, T>();
		}

		/// <summary>
		/// If type matching interface (or base type) 'I' is missing adds a component of concrete type 'T' (which must implement 'I')
		/// </summary>
		/// <returns>The component that was either found or added.</returns>
		/// <param name="go">extension method 'this'</param>
		/// <typeparam name="I">The interface type to look for.</typeparam>
		/// <typeparam name="T">The concrete implementation type of I to add if no instance of I is found.</typeparam>
		public static T AddIfMissing<T>(this GameObject go)
			where T : Component
		{
			return go.AddIfMissing<T, T>();
		}

		/// <summary>
		/// If type matching interface (or base type) 'I' is missing adds a component of concrete type 'T' (which must implement 'I')
		/// </summary>
		/// <returns>The component that was either found or added.</returns>
		/// <param name="go">extension method 'this'</param>
		/// <typeparam name="I">The interface type to look for.</typeparam>
		/// <typeparam name="T">The concrete implementation type of I to add if no instance of I is found.</typeparam>
		public static I AddIfMissing<I, T>(this GameObject go)
			where I : class
			where T : Component, I
		{
			I inst = go.GetComponent<I>();
			if(inst == null) {
				inst = go.AddComponent<T>();
			}
			return inst;
		}

		public static T GetComponentInParent<T>(this Component c, bool includeInactive = false, bool excludeSelf = false) where T : class
		{
			if(!excludeSelf && (includeInactive || c.gameObject.activeSelf)) {
				var comp = c.GetComponent<T>();
				if(comp != null) {
					return comp;
				}
			}

			Transform p = (c is Transform)? (c as Transform).parent: c.transform.parent;

			if(p == null) {
				return null;
			}

			return p.GetComponentInParent<T>(includeInactive, false);
		}

		/// <summary>
		/// Same as GetComponent but excludes the caller
		/// </summary>
		public static T GetSiblingComponent<T>(this Component primary, bool includeInactive = false) where T : class
		{
			using(var comps = ListPool<T>.Get()) {
				primary.GetComponents<T>(comps, includeInactive);
				foreach(var c in comps) {
					if(object.ReferenceEquals(c, primary)) { continue; }
					return c;
				}
			}
			return null;
		}

		/// <summary>
		/// Same as GetComponent but excludes the caller
		/// </summary>
		public static void GetSiblingComponents<T>(this Component primary, List<T> results, bool includeInactive = false) where T : class
		{
			primary.GetComponents<T>(results, includeInactive);
			for(int i = results.Count - 1; i >= 0; i--) {
				if(object.ReferenceEquals(primary, results[i])) {
					results.RemoveAt(i);
				}
			}
		}

		public static void GetComponents(this Component t, Type cType, List<Component> results, bool includeInactive)
		{
			using (var allComps = ListPool<Component>.Get ()) {
				t.GetComponents<Component> (allComps, includeInactive);
				foreach (var c in allComps) {
					if (cType.IsAssignableFrom (c.GetType())) {
						results.Add (c);
					}
				}
			}
		}

		public static void GetComponents<T>(this Component t, List<T> results, bool includeInactive) where T : class
		{
			GetComponents<T> (t.gameObject, results, includeInactive);
		}

		public static void GetComponents<T>(this GameObject t, List<T> results, bool includeInactive) where T : class
		{
			if(includeInactive) {
				using(var tmp = ListPool<T>.Get()) {
					t.GetComponents<T>(tmp);
					results.AddRange(tmp);
				}
			}
			else {
				using(var tmp = ListPool<T>.Get()) {
					t.GetComponents<T>(tmp);
					foreach(var c in tmp) {
						if((c as Component).gameObject.activeInHierarchy) {
							results.Add(c);
						}
					}
				}
			}
		}

		public static T GetComponentInDirectChildren<T>(this Transform t, bool includeInactive = false) where T : class
		{
			T c = t.GetComponent<T>();
			if(c != null) {
				return c;
			}

			foreach(Transform childT in t) {
				if((c = childT.GetComponent<T>()) != null
					&& (includeInactive || (c as Component).gameObject.activeInHierarchy)) {
					return c;
				}
			}
			return null;
		}

		public static void GetComponentsInDirectChildren<T>(this Transform t, List<T> results, bool includeInactive = false) where T : class
		{
			var n = t.childCount;

			for(int i = 0; i < n; i++) {
				var child = t.GetChild(i);
				child.GetComponents<T>(results, includeInactive);
			}
		}

        public static void GetComponentsInDirectChildren<T>(this Component c, List<T> results, bool includeInactive = false) where T : class
        {
            c.transform.GetComponentsInDirectChildren<T>(results, includeInactive);
        }

        public static void GetComponentsInDirectChildren<T>(this GameObject go, List<T> results, bool includeInactive = false) where T : class
        {
            go.transform.GetComponentsInDirectChildren<T>(results, includeInactive);
        }

        public static int CountComponentsInDirectChildren<T>(this Transform t, bool includeInactive = false) where T : class
        {
            using (var results = ListPool<T>.Get())
            {
                t.GetComponentsInDirectChildren<T>(results, includeInactive);
                return results.Count;
            }
        }

        public static int CountComponentsInDirectChildren<T>(this Component c, bool includeInactive = false) where T : class
        {
            return c.transform.CountComponentsInDirectChildren<T>(includeInactive);
        }

        public static int CountComponentsInDirectChildren<T>(this GameObject go, bool includeInactive = false) where T : class
        {
            return go.transform.CountComponentsInDirectChildren<T>(includeInactive);
        }

        public static bool HasExactlyOneChildWithComponent<T>(this Transform t, bool includeInactive = false) where T : class
        {
            return t.CountComponentsInDirectChildren<T>(includeInactive) == 1;
        }

        public static bool HasExactlyOneChildWithComponent<T>(this Component c, bool includeInactive = false) where T : class
        {
            return c.transform.HasExactlyOneChildWithComponent<T>(includeInactive);
        }

        public static bool HasExactlyOneChildWithComponent<T>(this GameObject go, bool includeInactive = false) where T : class
        {
            return go.transform.HasExactlyOneChildWithComponent<T>(includeInactive);
        }

		public static void GetComponentsInTrueChildren<T>(this GameObject go, List<T> results, bool includeInactive = false) where T : class
		{
			go.transform.GetComponentsInTrueChildren<T>(results, includeInactive);
		}

		public static void GetComponentsInTrueChildren<T>(this Component c, List<T> results, bool includeInactive = false) where T : class
		{
			c.transform.GetComponentsInTrueChildren<T>(results, includeInactive);
		}

		public static void GetComponentsInTrueChildren<T>(this Transform t, List<T> results, bool includeInactive = false) where T : class
		{
			t.GetComponentsInChildren<T>(includeInactive, results);
			for(int i = results.Count - 1; i >= 0; i--) {
				if((results[i] as Component).transform == t) {
					results.RemoveAt(i);
				}
			}
		}

		private static void GetComponentsInChildren<T>(Transform t, List<T> results, bool includeInactive = false, int depth = 0, int maxDepth = int.MaxValue) where T : class
		{
			if(depth > maxDepth) {
				return;
			}

			// TODO: this needs to be retested. Seems like GetComponents may now be clearing the result list on every call???
			t.GetComponents<T>(results, includeInactive);

			if(depth == maxDepth) {
				return;
			}

			foreach(Transform childT in t) {
				GetComponentsInChildren<T>(childT, results, includeInactive, depth + 1, maxDepth);
			}
		}


		public static void EditComponent<T>(this Component thisC, Action<T> editAction, bool createMissing = true)
			where T : Component
		{
			var c = createMissing ? thisC.AddIfMissing<T> () : thisC.GetComponent<T> ();
			if (c == null) {
				return;
			}
			editAction (c);
		}

		public static void EditComponent<T>(this GameObject thisGO, Action<T> editAction, bool createMissing = true)
			where T : Component
		{
			var c = createMissing ? thisGO.AddIfMissing<T> () : thisGO.GetComponent<T> ();
			if (c == null) {
				return;
			}
			editAction (c);
		}

	}
}



