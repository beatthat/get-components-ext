using UnityEngine;
using System.Collections.Generic;

namespace BeatThat
{
	/// <summary>
	/// ext methods for GetComponent[s] with options beyond what Unity's GetComponent methods support
	/// </summary>
	public static class GetComponentsExt
	{
		public static Component AddIfMissing(this Component targetObject, System.Type concreteType)
		{
			return targetObject.gameObject.AddIfMissing(concreteType, concreteType);
		}

		public static Component AddIfMissing(this GameObject targetObject, System.Type concreteType)
		{
			return targetObject.AddIfMissing(concreteType, concreteType);
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

		public static void GetComponents<T>(this Component t, List<T> results, bool includeInactive) where T : class
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
	}
}
