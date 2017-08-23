using System.ComponentModel;

namespace AutoTransfer.Enum
{
    public partial class Ref
    {
        public enum A53SampleBloombergField
        {
            /// <summary>
            /// SP國外評等
            /// </summary>
            [Description("SP國外評等")]
            RTG_SP,

            /// <summary>
            /// Moody's國外評等
            /// </summary>
            [Description("Moody's國外評等")]
            RTG_MOODY,

            /// <summary>
            /// 惠譽國內評等
            /// </summary>
            [Description("惠譽國內評等")]
            RTG_FITCH_NATIONAL,

            /// <summary>
            /// 惠譽評等
            /// </summary>
            [Description("惠譽評等")]
            RTG_FITCH,

            /// <summary>
            /// TRC 評等
            /// </summary>
            [Description("TRC 評等")]
            RTG_TRC,
        }
    }
}