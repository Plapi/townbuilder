#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using MantisLOD;
using Object = UnityEngine.Object;

public static class MeshUtils {

	public static GameObject OptimizeAndCombine(Transform parent, float quality) {
		parent = OptimizeChildMeshes(parent, quality);
		GameObject newMeshObj = Combine(parent);
		UnityEngine.Object.DestroyImmediate(parent.gameObject);
		return newMeshObj;
	}

	public static Transform OptimizeChildMeshes(Transform parent, float quality) {
		MeshFilter[] meshFilters = parent.GetComponentsInChildren<MeshFilter>(true);

		Transform newObjParent = new GameObject("newObj").transform;

		for (int i = 0; i < meshFilters.Length; i++) {
			Mesh mesh = UnityEngine.Object.Instantiate(meshFilters[i].sharedMesh);

			int trianglesNumber = mesh.triangles.Length;
			Vector3[] vertices = mesh.vertices;
			int[] triangles = new int[trianglesNumber + mesh.subMeshCount];
			Vector3[] normals = mesh.normals;
			Color[] colors = mesh.colors;
			Vector2[] uvs = mesh.uv;
			int counter = 0;
			for (int j = 0; j < mesh.subMeshCount; j++) {
				int[] subTriangles = mesh.GetTriangles(j);
				triangles[counter] = subTriangles.Length;
				counter++;
				Array.Copy(subTriangles, 0, triangles, counter, subTriangles.Length);
				counter += subTriangles.Length;
			}

			int index = MantisLODSimpler.create_progressive_mesh(vertices, vertices.Length, triangles, counter, normals, normals.Length, colors, colors.Length, uvs, uvs.Length, 1, 0, 0, 0, 1);

			int[] outTriangles = new int[trianglesNumber + mesh.subMeshCount];
			int outCount = 0;

			MantisLODSimpler.get_triangle_list(index, quality, outTriangles, ref outCount);

			if (outCount > 0) {
				int mat = 0;
				counter = 0;
				while (counter < outCount) {
					int len = outTriangles[counter];
					counter++;
					if (len > 0) {
						int[] new_triangles = new int[len];
						Array.Copy(outTriangles, counter, new_triangles, 0, len);
						mesh.SetTriangles(new_triangles, mat);
						counter += len;
					} else {
						mesh.SetTriangles((int[])null, mat);
					}
					mat++;
				}
			}

			GameObject newMeshObj = new GameObject(meshFilters[i].name);
			newMeshObj.transform.position = meshFilters[i].transform.position;
			newMeshObj.transform.rotation = meshFilters[i].transform.rotation;
			newMeshObj.transform.localScale = meshFilters[i].transform.localScale;
			newMeshObj.AddComponent<MeshRenderer>().sharedMaterials = meshFilters[i].GetComponent<MeshRenderer>().sharedMaterials;
			newMeshObj.AddComponent<MeshFilter>().mesh = mesh;

			newMeshObj.transform.parent = newObjParent;
		}

		return newObjParent;
	}

	public static GameObject Combine(Transform parent, bool includeInactive = true) {
		Dictionary<Material, List<CombineInstance>> dictionary = new Dictionary<Material, List<CombineInstance>>();
		List<Material> materials = new List<Material>();

		MeshFilter[] meshFilters = parent.GetComponentsInChildren<MeshFilter>(includeInactive);

		for (int i = 0; i < meshFilters.Length; i++) {
			(Material, Mesh)[] tuple = SplitMesh(meshFilters[i].gameObject);
			for (int j = 0; j < tuple.Length; j++) {
				if (!dictionary.ContainsKey(tuple[j].Item1)) {
					dictionary.Add(tuple[j].Item1, new List<CombineInstance>());
					materials.Add(tuple[j].Item1);
				}
				dictionary[tuple[j].Item1].Add(new CombineInstance {
					mesh = tuple[j].Item2,
					transform = meshFilters[i].transform.localToWorldMatrix
				});
			}
		}

		int vertexCount = 0;
		List<CombineInstance> combineInstances = new List<CombineInstance>();
		foreach (var item in dictionary) {
			Mesh mesh = new Mesh();
			mesh.CombineMeshes(item.Value.ToArray());
			mesh.Optimize();
			combineInstances.Add(new CombineInstance {
				mesh = mesh,
				transform = parent.transform.localToWorldMatrix
			});
			vertexCount += mesh.vertexCount;
		}

		Mesh newMesh = new Mesh() { name = "mesh" };
		if (vertexCount > 65535) {
			newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		}
		newMesh.CombineMeshes(combineInstances.ToArray(), false);
		newMesh.Optimize();

		GameObject newMeshObj = new GameObject("newMesh");
		newMeshObj.transform.localPosition = Vector3.zero;
		newMeshObj.AddComponent<MeshRenderer>().materials = materials.ToArray();
		newMeshObj.AddComponent<MeshFilter>().mesh = newMesh;

		return newMeshObj;
	}

	public static Mesh CloneMesh(Mesh mesh) {
		return new Mesh {
			vertices = mesh.vertices,
			triangles = mesh.triangles,
			uv = mesh.uv,
			normals = mesh.normals,
			colors = mesh.colors,
			tangents = mesh.tangents,
			subMeshCount = mesh.subMeshCount
		};
	}

	private static (Material, Mesh)[] SplitMesh(GameObject obj) {
		Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;
		if (mesh.subMeshCount <= 1) {
			return new (Material, Mesh)[] { (obj.GetComponent<MeshRenderer>().sharedMaterial, mesh) };
		}

		(Material, Mesh)[] tuple = new (Material, Mesh)[mesh.subMeshCount];

		for (int j = 0; j < mesh.subMeshCount; j++) {
			tuple[j].Item1 = obj.GetComponent<MeshRenderer>().sharedMaterials[j];
			tuple[j].Item2 = SubMeshToMesh(mesh, j);
		}

		return tuple;
	}

	private static Mesh SubMeshToMesh(Mesh mesh, int subMeshIndex) {
		Vector3[] vertices = mesh.vertices;
		int[] triangles = mesh.GetTriangles(subMeshIndex);
		Vector3[] normals = mesh.normals;
		Vector2[] uv = mesh.uv;

		List<Vector3> verticesList = new List<Vector3>();
		List<int> trianglesList = new List<int>();
		List<Vector3> normalsList = new List<Vector3>();
		List<Vector2> uvList = new List<Vector2>();

		for (int i = 0; i < triangles.Length; i += 3) {
			verticesList.Add(vertices[triangles[i]]);
			verticesList.Add(vertices[triangles[i + 1]]);
			verticesList.Add(vertices[triangles[i + 2]]);

			normalsList.Add(normals[triangles[i]]);
			normalsList.Add(normals[triangles[i + 1]]);
			normalsList.Add(normals[triangles[i + 2]]);

			uvList.Add(uv[triangles[i]]);
			uvList.Add(uv[triangles[i + 1]]);
			uvList.Add(uv[triangles[i + 2]]);

			trianglesList.Add(trianglesList.Count);
			trianglesList.Add(trianglesList.Count);
			trianglesList.Add(trianglesList.Count);
		}

		return new Mesh {
			vertices = verticesList.ToArray(),
			triangles = trianglesList.ToArray(),
			uv = uvList.ToArray(),
			normals = normalsList.ToArray()
		};
	}

	public static GameObject OptimizingMeshByReducingToOneMaterial(MeshRenderer meshRenderer)
	{
		var meshFilter = meshRenderer.GetComponent<MeshFilter>();
		var sourceMesh = meshFilter.sharedMesh;
		var materials = meshRenderer.sharedMaterials;
		
		if (materials.Length <= 1)
		{
			Debug.LogError("Mesh already has a single material.");
			return meshRenderer.gameObject;
		}

#region Texture
		int cellCount = materials.Length;
		int atlasSize = Mathf.NextPowerOfTwo(
			Mathf.CeilToInt(Mathf.Sqrt(cellCount))
		);

		var texture = new Texture2D(atlasSize, atlasSize, TextureFormat.RGBA32, false)
		{
			filterMode = FilterMode.Point,
			wrapMode = TextureWrapMode.Clamp
		};

		var pixels = new Color[atlasSize * atlasSize];
		for (int i = 0; i < pixels.Length; i++)
			pixels[i] = Color.magenta;

		for (int i = 0; i < materials.Length; i++)
		{
			if (!materials[i].HasProperty("_BaseColor"))
			{
				Debug.LogError($"Material {materials[i].name} has no _BaseColor.");
				continue;
			}

			int x = i % atlasSize;
			int y = i / atlasSize;

			pixels[y * atlasSize + x] = materials[i].GetColor("_BaseColor");
		}

		texture.SetPixels(pixels);
		texture.Apply();
#endregion
		
#region Mesh
		var newMesh = Object.Instantiate(sourceMesh);
		
		var uvs = new Vector2[newMesh.vertexCount];
		var finalTriangles = new List<int>();

		for (int i = 0; i < uvs.Length; i++)
			uvs[i] = Vector2.zero;

		float invSize = 1f / atlasSize;

		for (int sub = 0; sub < sourceMesh.subMeshCount; sub++)
		{
			var tris = sourceMesh.GetTriangles(sub);

			int cellX = sub % atlasSize;
			int cellY = sub / atlasSize;

			Vector2 uv = new Vector2(
				(cellX + 0.5f) * invSize,
				(cellY + 0.5f) * invSize
			);

			foreach (var t in tris)
			{
				finalTriangles.Add(t);
				uvs[t] = uv;
			}
		}

		newMesh.subMeshCount = 1;
		newMesh.SetTriangles(finalTriangles, 0);
		newMesh.uv = uvs;

		newMesh.RecalculateNormals();
		newMesh.RecalculateBounds();
#endregion

#region Material
		var newMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
		newMat.SetTexture("_BaseMap", texture);
#endregion
		
#region FinalObject
		var newObj = new GameObject("newMesh");
		newObj.AddComponent<MeshRenderer>().sharedMaterial = newMat;
		newObj.AddComponent<MeshFilter>().sharedMesh = newMesh;
#endregion

		return newObj;
	}
}
#endif
