using System.ComponentModel;

namespace AutoTransfer.Enum
{
    public partial class Ref
    {
        public enum GroupProductCode
        {
            /// <summary>
            /// 房貸
            /// </summary>
            [Description("1")]
            M,

            /// <summary>
            /// 債券
            /// </summary>
            [Description("4")]
            B,
        }
    }
}