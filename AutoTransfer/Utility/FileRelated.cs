using System.IO;

namespace AutoTransfer.Utility
{
    public class FileRelated
    {
        #region Create 資料夾
        /// <summary>
        /// Create 資料夾(判斷如果沒有的話就新增)
        /// </summary>
        /// <param name="projectFile">資料夾位置</param>
        public void createFile(string projectFile)
        {
                bool exists = Directory.Exists(projectFile);
                if (!exists) Directory.CreateDirectory(projectFile);           
        }
        #endregion
    }
}
