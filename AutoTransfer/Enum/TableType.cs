using System.ComponentModel;

namespace AutoTransfer.Enum
{
    public partial class Ref
    {
        public enum TableType
        {
            /// <summary>
            /// 外部信評資料檔_SP
            /// </summary>
            [Description("Rating_SP_Info")]
            A53,

            /// <summary>
            /// 外部信評資料檔_Moody
            /// </summary>
            [Description("Rating_Moody_Info")]
            A54,

            /// <summary>
            /// 外部信評資料檔_Fitch
            /// </summary>
            [Description("Rating_Fitch_Info")]
            A55,

            /// <summary>
            /// 外部信評資料檔_中華
            /// </summary>
            [Description("Rating_CW_Info")]
            A56,

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
        }
    }
}
