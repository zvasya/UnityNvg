using UnityEngine;
using UnityEngine.Serialization;

namespace NvgExample
{
	[CreateAssetMenu(menuName = "Nvg/DemoResources")]
	public class DemoResources : ScriptableObject
	{
		[field: SerializeField]
		public TextAsset[] Images { get; private set; }
		[field: SerializeField]
		public TextAsset EntypoFont { get; private set; }
		[field: SerializeField]
		public TextAsset RobotoRegularFont { get; private set; }
		[field: SerializeField]
		public TextAsset RobotoBoldFont { get; private set; }
		[field: SerializeField]
		public TextAsset NotoEmojiRegularFont { get; private set; }
	}
}