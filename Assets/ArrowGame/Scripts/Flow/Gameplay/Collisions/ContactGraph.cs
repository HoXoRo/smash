using System.Collections.Generic;

namespace Gameplay.Collisions
{
	public static class ContactGraph
	{
		public static void CollectConnected(ContactNode start, HashSet<ContactNode> result, Queue<ContactNode> queue, ContactNodeType allowedMask)
		{
			result.Clear();
			queue.Clear();
			if (start == null || (start.nodeType & allowedMask) == 0)
			{
				return;
			}
			result.Add(start);
			queue.Enqueue(start);
			while (queue.Count > 0)
			{
				ContactNode node = queue.Dequeue();
				foreach (ContactNode neighbor in node.Neighbors)
				{
					if (neighbor == null || result.Contains(neighbor) || (neighbor.nodeType & allowedMask) == 0)
					{
						continue;
					}
					result.Add(neighbor);
					queue.Enqueue(neighbor);
				}
			}
		}
	}
}
