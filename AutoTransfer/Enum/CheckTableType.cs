using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTransfer.Enum
{
    public partial class Ref
    {
        /// <summary>
        /// 驗證 Enum
        /// </summary>
        public enum Check_Table_Type
        {
            /// <summary>
            /// (債券) A41 Excel上傳驗證
            /// </summary>
            [Description("(債券) A41 Excel上傳驗證")]
            Bonds_A41_UpLoad,

            /// <summary>
            /// (債券) 執行信評轉檔驗證
            /// </summary>
            [Description("(債券) 執行信評轉檔驗證")]
            Bonds_A58_Transfer_Check,

            /// <summary>
            /// (債券) B01 轉檔驗證 A41
            /// </summary>
            [Description("(債券) B01 轉檔驗證 A41")]
            Bonds_B01_Before_Check,

            /// <summary>
            /// (債券) B01 轉檔完成驗證
            /// </summary>
            [Description("(債券) B01 轉檔完成驗證")]
            Bonds_B01_Transfer_Check,

            /// <summary>
            /// (債券) C01 轉檔驗證 B01
            /// </summary>
            [Description("(債券) C01 轉檔驗證 B01")]
            Bonds_C01_Before_Check,

            /// <summary>
            /// (債券) C01 轉檔完成驗證
            /// </summary>
            [Description("(債券) C01 轉檔完成驗證")]
            Bonds_C01_Transfer_Check,

            /// <summary>
            ///  (債券) C01 香港越南 Excel上傳驗證
            /// </summary>
            [Description("(債券) C01 香港越南 Excel上傳驗證")]
            Bonds_C01_HK_VN_UpLoad,

            /// <summary>
            /// (債券) C01 香港 轉檔完成驗證
            /// </summary>
            [Description("(債券) C01 香港 轉檔完成驗證")]
            Bonds_C01_HK_Transfer_Check,

            /// <summary>
            /// (債券) C01 越南 轉檔完成驗證
            /// </summary>
            [Description("(債券) C01 越南 轉檔完成驗證")]
            Bonds_C01_VN_Transfer_Check,

            /// <summary>
            /// (債券) C07 減損計算結果
            /// </summary>
            [Description("(債券) C07 減損計算結果")]
            Bonds_C07_Transfer_Check,

            /// <summary>
            /// (房貸) B01 轉檔驗證
            /// </summary>
            [Description("(房貸) B01 轉檔驗證")]
            Mortgage_B01_Before_Check,

            /// <summary>
            /// (房貸) B01 轉檔完成驗證
            /// </summary>
            [Description("(房貸) B01 轉檔完成驗證")]
            Mortgage_B01_Transfer_Check,

            /// <summary>
            /// (房貸) C01 轉檔驗證
            /// </summary>
            [Description("(房貸) C01 轉檔驗證")]
            Mortgage_C01_Before_Check,

            /// <summary>
            /// (房貸) C01 轉檔完成驗證
            /// </summary>
            [Description("(房貸) C01 轉檔完成驗證")]
            Mortgage_C01_Transfer_Check,

            /// <summary>
            /// (房貸) C02 轉檔驗證
            /// </summary>
            [Description("(房貸) C02 轉檔驗證")]
            Mortgage_C02_Before_Check,

            /// <summary>
            /// (房貸) C02 轉檔完成驗證
            /// </summary>
            [Description("(房貸) C02 轉檔完成驗證")]
            Mortgage_C02_Transfer_Check,
        }
    }
}
