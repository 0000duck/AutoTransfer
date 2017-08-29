using System.ComponentModel;

namespace AutoTransfer.Enum
{
    public partial class Ref
    {
        public enum MessageType
        {
            /// <summary>
            /// 新增Sample上傳檔案失敗
            /// </summary>
            [Description("新增Sample上傳檔案失敗")]
            Create_Sample_File_Fail,

            /// <summary>
            /// 新增Commpany上傳檔案失敗
            /// </summary>
            [Description("新增Commpany上傳檔案失敗")]
            Create_Commpany_File_Fail,

            /// <summary>
            /// 上傳Sample檔案失敗
            /// </summary>
            [Description("上傳Sample檔案失敗")]
            Put_Sample_File_Fail,

            /// <summary>
            /// 上傳Commpany檔案失敗
            /// </summary>
            [Description("上傳Commpany檔案失敗")]
            Put_Commpany_File_Fail,

            /// <summary>
            /// 下載Sample檔案失敗
            /// </summary>
            [Description("下載Sample檔案失敗")]
            Get_Sample_File_Fail,

            /// <summary>
            /// 下載Commpany檔案失敗
            /// </summary>
            [Description("下載Commpany檔案失敗")]
            Get_Commpanye_File_Fail,

            /// <summary>
            /// 傳入時間格式錯誤(yyyyMMdd)
            /// </summary>
            [Description("傳入時間格式錯誤(yyyyMMdd)")]
            DateTime_Format_Fail,

            /// <summary>
            /// 資料已經儲存過了
            /// </summary>
            [Description("資料已經儲存過了!")]
            already_Save,

            /// <summary>
            /// 轉檔成功
            /// </summary>
            [Description("轉檔成功")]
            Success,

            /// <summary>
            /// 沒有找到資料
            /// </summary>
            [Description("沒有找到資料!")]
            not_Find_Any,

            /// <summary>
            /// 轉檔驗證失敗
            /// </summary>
            [Description("轉檔驗證失敗!")]
            transferError,
        }
    }
}