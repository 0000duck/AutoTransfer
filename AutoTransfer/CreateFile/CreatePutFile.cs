using AutoTransfer.Utility;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AutoTransfer.CreateFile
{
    public class CreatePutFile
    {
        public bool create(string filePath, string fileName,List<string> data)
        {
            if (data.Any() && !filePath.IsNullOrWhiteSpace() && !fileName.IsNullOrWhiteSpace())
            {
                new FileRelated().createFile(filePath);
                using (StreamWriter sw = new StreamWriter
                    (Path.Combine(filePath, fileName), false)) //false 複寫 true 附加
                {
                    data.ForEach(x => sw.WriteLine(x));
                    sw.Close();
                }
                return true;
            }
            return false;
        }
    }
}
