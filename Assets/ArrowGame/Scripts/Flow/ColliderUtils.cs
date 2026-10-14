using UnityEngine;

public static class ColliderUtils
{
	public static Collider CopyCollider(Collider original, GameObject target)
	{
		if (original == null || target == null)
		{
			return null;
		}
		if (original is BoxCollider box)
		{
			return CopyBoxCollider(box, target);
		}
		if (original is SphereCollider sphere)
		{
			return CopySphereCollider(sphere, target);
		}
		if (original is CapsuleCollider capsule)
		{
			return CopyCapsuleCollider(capsule, target);
		}
		if (original is MeshCollider mesh)
		{
			return CopyMeshCollider(mesh, target);
		}
		return null;
	}

	private static BoxCollider CopyBoxCollider(BoxCollider src, GameObject target)
	{
		BoxCollider dst = target.AddComponent<BoxCollider>();
		CopyCommon(src, dst);
		dst.center = src.center;
		dst.size = src.size;
		return dst;
	}

	private static SphereCollider CopySphereCollider(SphereCollider src, GameObject target)
	{
		SphereCollider dst = target.AddComponent<SphereCollider>();
		CopyCommon(src, dst);
		dst.center = src.center;
		dst.radius = src.radius;
		return dst;
	}

	private static CapsuleCollider CopyCapsuleCollider(CapsuleCollider src, GameObject target)
	{
		CapsuleCollider dst = target.AddComponent<CapsuleCollider>();
		CopyCommon(src, dst);
		dst.center = src.center;
		dst.radius = src.radius;
		dst.height = src.height;
		dst.direction = src.direction;
		return dst;
	}

	private static MeshCollider CopyMeshCollider(MeshCollider src, GameObject target)
	{
		MeshCollider dst = target.AddComponent<MeshCollider>();
		CopyCommon(src, dst);
		dst.sharedMesh = src.sharedMesh;
		dst.convex = src.convex;
		dst.cookingOptions = src.cookingOptions;
		return dst;
	}

	private static void CopyCommon(Collider src, Collider dst)
	{
		dst.enabled = src.enabled;
		dst.isTrigger = src.isTrigger;
		dst.material = src.material;
		dst.sharedMaterial = src.sharedMaterial;
	}
}
