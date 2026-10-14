using System;
using System.Collections.Generic;
using Gameplay.Objects;
using UnityEngine;

namespace Gameplay.Collisions
{
	[RequireComponent(typeof(Collider))]
	[DisallowMultipleComponent]
	public class ContactNode : MonoBehaviour
	{
		[NonSerialized]
		public IContactNodeOwner MasterObject;

		public Rigidbody rb;

		private readonly HashSet<ContactNode> _neighbors = new HashSet<ContactNode>();

		public ContactNodeType nodeType;

		public IEnumerable<ContactNode> Neighbors => _neighbors;

		public void SetType(ContactNodeType type)
		{
			nodeType = type;
		}

		private void Awake()
		{
			if (rb == null)
			{
				rb = GetComponent<Rigidbody>();
			}
			if (MasterObject == null)
			{
				MasterObject = GetComponentInParent<IContactNodeOwner>();
			}
		}

		private void OnCollisionEnter(Collision c)
		{
			TryAddNeighbor(c.rigidbody);
		}

		private void OnCollisionStay(Collision c)
		{
			TryAddNeighbor(c.rigidbody);
		}

		private void OnCollisionExit(Collision c)
		{
			TryRemoveNeighbor(c.rigidbody);
		}

		private void TryAddNeighbor(Rigidbody otherRb)
		{
			if (otherRb == null)
			{
				return;
			}
			ContactNode other = otherRb.GetComponent<ContactNode>();
			if (other == null || other == this)
			{
				return;
			}
			_neighbors.Add(other);
			other._neighbors.Add(this);
		}

		private void TryRemoveNeighbor(Rigidbody otherRb)
		{
			if (otherRb == null)
			{
				return;
			}
			ContactNode other = otherRb.GetComponent<ContactNode>();
			if (other == null)
			{
				return;
			}
			_neighbors.Remove(other);
			other._neighbors.Remove(this);
		}
	}
}
