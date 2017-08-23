using System.ComponentModel;

namespace AutoTransfer.Enum
{
    public partial class Ref
    {
        public enum A53CommpanyBloombergField
        {
            /// <summary>
            /// (國內)標普本國貨幣長期發行人信用評等
            /// </summary>
            [Description("(國內)標普本國貨幣長期發行人信用評等")]
            RTG_SP_LT_LC_ISSUER_CREDIT,

            /// <summary>
            /// 標普長期外幣發行人信用評等
            /// </summary>
            [Description("標普長期外幣發行人信用評等")]
            RTG_SP_LT_FC_ISSUER_CREDIT,

            /// <summary>
            /// (國內)穆迪長期本國銀行存款評等
            /// </summary>
            [Description("(國內)穆迪長期本國銀行存款評等")]
            RTG_MDY_LOCAL_LT_BANK_DEPOSITS,

            /// <summary>
            /// 穆迪外幣發行人評等
            /// </summary>
            [Description("穆迪外幣發行人評等")]
            RTG_MDY_FC_CURR_ISSUER_RATING,

            /// <summary>
            /// 穆迪發行人評等
            /// </summary>
            [Description("穆迪發行人評等")]
            RTG_MDY_ISSUER,

            /// <summary>
            /// 穆迪長期評等
            /// </summary>
            [Description("穆迪長期評等")]
            RTG_MOODY_LONG_TERM,

            /// <summary>
            /// 穆迪優先無擔保債務評等
            /// </summary>
            [Description("穆迪優先無擔保債務評等")]
            RTG_MDY_SEN_UNSECURED_DEBT,

            /// <summary>
            /// (國內)惠譽長期發行人違約評等
            /// </summary>
            [Description("(國內)惠譽長期發行人違約評等")]
            RTG_FITCH_LT_ISSUER_DEFAULT,

            /// <summary>
            /// (國內)惠譽國內長期評等日期
            /// </summary>
            [Description("(國內)惠譽國內長期評等日期")]
            RTG_FITCH_NATIONAL_LT,

            /// <summary>
            /// 惠譽長期外幣發行人違約評等
            /// </summary>
            [Description("惠譽長期外幣發行人違約評等")]
            RTG_FITCH_LT_FC_ISSUER_DEFAULT,

            /// <summary>
            /// 惠譽長期本國貨幣發行人違約評等
            /// </summary>
            [Description("惠譽長期本國貨幣發行人違約評等")]
            RTG_FITCH_LT_LC_ISSUER_DEFAULT,

            /// <summary>
            /// 惠譽優先無擔保債務評等
            /// </summary>
            [Description("惠譽優先無擔保債務評等")]
            RTG_FITCH_SEN_UNSECURED,

            /// <summary>
            /// (國內)TRC 長期評等
            /// </summary>
            [Description("(國內)TRC 長期評等")]
            RTG_TRC_LONG_TERM,
        }
    }
}