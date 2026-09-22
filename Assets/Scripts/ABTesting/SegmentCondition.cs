using System.Linq;
using Segmentation;

namespace ABTesting
{
	public class SegmentCondition : IABCondition
	{
		private readonly UserSegment[] _segments;

		public SegmentCondition(params UserSegment[] segments)
		{
			_segments = segments;
		}

		public bool IsSatisfied()
		{
			return _segments != null && _segments.Length > 0 && _segments.Contains(SegmentationHelper.GetSegment());
		}
	}
}
