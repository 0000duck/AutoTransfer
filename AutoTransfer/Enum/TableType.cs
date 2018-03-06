using System.ComponentModel;

namespace AutoTransfer.Enum
{
    public partial class Ref
    {
        public enum TableType
        {
            /// <summary>
            /// 國內總經變數
            /// </summary>
            [Description("Econ_Domestic")]
            A07,

            /// <summary>
            /// 外部信評資料檔
            /// </summary>
            [Description("Rating_Info")]
            A53,

            /// <summary>
            /// 債券信評檔_歷史檔
            /// </summary>
            [Description("Bond_Rating_Info")]
            A57,

            /// <summary>
            /// 債券信評檔_整理檔
            /// </summary>
            [Description("Bond_Rating_Summary")]
            A58,

            /// <summary>
            /// 外部信評資料檔_Fitch_Twn
            /// </summary>
            [Description("Rating_Fitch_Twn_Info")]
            A60,

            /// <summary>
            /// 國外總體經濟變數
            /// </summary>
            [Description("Econ_Foreign")]
            A84,

            /// <summary>
            /// 主權債測試指標_年收集
            /// </summary>
            [Description("Gov_Info_Yearly")]
            A91,

            /// <summary>
            /// 主權債測試指標_季收集
            /// </summary>
            [Description("Gov_Info_Yearly")]
            A92,

            /// <summary>
            /// 主權債測試指標_月收集
            /// </summary>
            [Description("Gov_Info_Monthly")]
            A93,

            /// <summary>
            /// 信用利差資料
            /// </summary>
            [Description("Bond_Spread_Info")]
            A961,

            /// <summary>
            /// 信用利差資料
            /// </summary>
            [Description("Bond_Spread_Info")]
            A962,

            /// <summary>
            /// 前瞻性國外總經資料
            /// </summary>
            [Description("Econ_F_YYYYMMDD")]
            C04,

            /// <summary>
            /// 前瞻性模型資料_Econ_D_YYYYMMDD
            /// </summary>
            [Description("Econ_D_YYYYMMDD")]
            C03Mortgage,
        }
    }
}