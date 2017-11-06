using System.ComponentModel;

namespace AutoTransfer.Enum
{
    public partial class Ref
    {
        public enum RatingOrg
        {
            /// <summary>
            /// 標普
            /// </summary>
            [Description("SP")]
            SP,

            /// <summary>
            /// 穆迪
            /// </summary>
            [Description("Moody")]
            Moody,

            /// <summary>
            /// 惠譽
            /// </summary>
            [Description("Fitch")]
            Fitch,

            /// <summary>
            /// 惠譽(台灣)
            /// </summary>
            [Description("Fitch(twn)")]
            FitchTwn,

            /// <summary>
            /// 中華信評
            /// </summary>
            [Description("CW")]
            CW,
        }
    }
}