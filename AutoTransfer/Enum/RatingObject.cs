using System.ComponentModel;

namespace AutoTransfer.Enum
{
    public partial class Ref
    {
        public enum RatingObject
        {
            /// <summary>
            /// 債項
            /// </summary>
            [Description("債項")]
            Bonds,

            /// <summary>
            /// 發行人
            /// </summary>
            [Description("發行人")]
            ISSUER,

            /// <summary>
            /// 保證人
            /// </summary>
            [Description("保證人")]
            GUARANTOR,
        }
    }
}