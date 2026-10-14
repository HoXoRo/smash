using UnityEngine;

namespace Gameplay.Particles
{
	public class ParticleObjectForwarder : MonoBehaviour
	{
		private ParticleObject _master;

		private int _index;

		public void Init(ParticleObject master, int index)
		{
			_master = master;
			_index = index;
		}

		private void OnCollisionEnter(Collision other)
		{
			_master.OnCollision(other, _index);
		}
	}
}
