using System.ComponentModel;

namespace AutoTransfer.Enum
{
    public partial class Ref
    {
        public enum TableType
        {
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
            /// 前瞻性模型資料_Econ_D_YYYYMMDD
            /// </summary>
            [Description("Econ_D_YYYYMMDD")]
            C03Mortgage,

            /// <summary>
            /// 國內總經變數
            /// </summary>
            [Description("Econ_Domestic")]
            A07,
        }
    }
}