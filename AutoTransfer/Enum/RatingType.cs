using System.ComponentModel;

namespace AutoTransfer.Enum
{
    public partial class Ref
    {
        public enum Rating_Type
        {
            /// <summary>
            /// 原始投資信評
            /// </summary>
            [Description("原始投資信評")]
            A,

            /// <summary>
            /// 評估日最近信評
            /// </summary>
            [Description("評估日最近信評")]
            B,
        }
    }
}