using AutoTransfer.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.Abstract
{
    /// <summary>
    /// 檢核程式 Abstract
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class CheckDataAbstract<T>
        where T : class
    {
        /// <summary>
        /// 傳入資料
        /// </summary>
        protected IEnumerable<T> _data;

        /// <summary>
        /// 檢核結果
        /// </summary>
        protected bool _checkFlag = false;

        /// <summary>
        /// 自定義訊息(前)
        /// </summary>
        protected string _customerStr_Start;

        /// <summary>
        /// 自定義訊息(後)
        /// </summary>
        protected string _customerStr_End;

        /// <summary>
        /// txt檔名
        /// </summary>
        protected Scheduling_Report D73 = new Scheduling_Report();

        /// <summary>
        /// 驗證檔案
        /// </summary>
        public Check_Table_Type _event;

        /// <summary>
        /// 訊息
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// 檢核有無錯誤
        /// </summary>
        public bool ErrorFlag { get; private set; }

        /// <summary>
        /// 字典資源
        /// </summary>
        protected Dictionary<Check_Table_Type, Func<List<messageTable>>> _resources;

        /// <summary>
        /// 檢核資料
        /// </summary>
        /// <param name="data">檢核資料</param>
        /// <param name="_event">執行方法</param>
        public CheckDataAbstract(IEnumerable<T> data, Check_Table_Type _event, DateTime? reportDate = null, int? version = null)
        {
            this._data = data;
            this._event = _event;
            this._resources = new Dictionary<Check_Table_Type, Func<List<messageTable>>>();
            _customerStr_Start = string.Empty;
            _customerStr_End = string.Empty;
            Set();
            try
            {
                StringBuilder sb = new StringBuilder();
                if (reportDate.HasValue && version.HasValue)
                {
                    sb.AppendLine($@"報導日:{reportDate.Value.ToString("yyyy/MM/dd")}  版本:{version}");
                }
                sb.AppendLine(getCheckMessage(GetMethod(_event).Invoke()));
                this.Message = sb.ToString();
                ErrorFlag = _checkFlag;
                CreateScheduling_Report(D73, Message);
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// 設定條件
        /// </summary>
        /// <param name="_data"></param>
        /// <param name="_event"></param>
        protected abstract void Set();

        protected void CreateScheduling_Report(Scheduling_Report D73,string sb)
        {
            new AutoTransfer.Transfer.D73().AddD73(D73, sb);
        }

        /// <summary>
        /// 找尋要執行的程式
        /// </summary>
        /// <param name="_event"></param>
        /// <returns></returns>
        private Func<List<messageTable>> GetMethod(Check_Table_Type _event)
        {
            if (_resources.ContainsKey(_event))
            {
                return _resources[_event];
            }
            else
            {
                throw new ArgumentNullException();
            }
        }

        /// <summary>
        /// 檢核資料表轉訊息
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private string getCheckMessage(List<messageTable> result)
        {
            string _result = string.Empty;
            StringBuilder sb = new StringBuilder();
            if (!_customerStr_Start.IsNullOrWhiteSpace())
                sb.AppendLine(_customerStr_Start);
            if (result.Any())
            {
                result.ForEach(x =>
                {
                    if (x.data != null && x.data.Values != null && x.data.Values.Any())
                    {
                        sb.AppendLine(string.Format("{0} 總合{1}項", x.title, x.data.Values.Sum(y => y.Count)));
                        int num = 1;
                        x.data.ToList().ForEach(y => {
                            sb.AppendLine(string.Format("{0}. {1} 共{2}項", num, y.Key, y.Value.Count));
                            y.Value.ForEach(m =>
                            {
                                sb.AppendLine(m.ToString());
                            });
                            num += 1;
                        });
                    }
                    else
                    {
                        sb.AppendLine(x.title);
                        sb.AppendLine(x.successStr);
                    }
                    sb.AppendLine(string.Empty);
                });
            }
            if (!_customerStr_End.IsNullOrWhiteSpace())
                sb.AppendLine(_customerStr_End);
            _result = sb.ToString();
            return _result;
        }

        /// <summary>
        /// 檢核程式
        /// </summary>
        /// <param name="_model">檢核Table</param>
        /// <param name="_dataKey">錯誤加入訊息</param>
        /// <param name="_dataMsg">檢核值</param>
        /// <param name="_check">檢核條件</param>
        protected void setCheckMsg(
            messageTable _model,
            string _dataKey,
            string _dataMsg,
            bool _check)
        {
            if (_model != null)
            {
                if (_check)
                {
                    if (_model.data == null)
                        _model.data = new Dictionary<string, List<StringBuilder>>() { };
                    if (_model.data.ContainsKey(_dataKey))
                    {
                        var _data = _model.data[_dataKey];
                        if (_data == null)
                        {
                            _data = new List<StringBuilder>();
                        }
                        _data.Add(new StringBuilder(_dataMsg));
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder(_dataMsg);
                        _model.data.Add(_dataKey, new List<StringBuilder>() { sb });
                    }
                }
            }
        }

        /// <summary>
        /// 檢核Table
        /// </summary>
        protected class messageTable
        {
            /// <summary>
            /// 顯示title 訊息
            /// </summary>
            public string title { get; set; }
            /// <summary>
            /// 驗證資料
            /// </summary>
            public Dictionary<string, List<StringBuilder>> data { get; set; }
            /// <summary>
            /// 成功訊息
            /// </summary>
            public string successStr { get; set; }
        }

        /// <summary>
        /// 文字檢核
        /// </summary>
        /// <param name="_par"></param>
        /// <returns></returns>
        protected bool checkString(string _par)
        {
            return _par.IsNullOrWhiteSpace();
        }

        /// <summary>
        /// 文字檢核Double
        /// </summary>
        /// <param name="_par"></param>
        /// <returns></returns>
        protected bool checkStringToDouble(string _par)
        {
            return checkString(_par) || TypeTransfer.stringToDoubleN(_par) == null;
        }

        /// <summary>
        /// 文字檢核DateTime
        /// </summary>
        /// <param name="_par"></param>
        /// <returns></returns>
        protected bool checkStringToDateTime(string _par)
        {
            return checkString(_par) || TypeTransfer.stringToDateTimeN(_par) == null;
        }

        /// <summary>
        /// 資料 Group 組合
        /// </summary>
        /// <typeparam name="M">group key</typeparam>
        /// <typeparam name="N">group value</typeparam>
        /// <param name="data">group data</param>
        /// <param name="sb">訊息要加入的StringBuilder</param>
        /// <param name="titles">替換顯示訊息</param>
        protected void groupData<M, N>(IEnumerable<IGrouping<M, N>> data, StringBuilder sb, List<FormateTitle> titles = null)
        {
            foreach (var item in data)
            {
                sb.AppendLine($@" {formateTitle(titles, item.Key)} : {item.Count()} 筆");
            }
        }

        /// <summary>
        /// 顯示訊息取代
        /// </summary>
        /// <param name="titles"></param>
        /// <param name="oldTitle"></param>
        /// <returns></returns>
        protected string formateTitle(List<FormateTitle> titles, string oldTitle)
        {
            if (oldTitle == null)
                return @"(null)";
            if (oldTitle.IsNullOrWhiteSpace())
                return @"(空白)";
            return changeTitle(titles, oldTitle);
        }

        /// <summary>
        /// 顯示訊息取代
        /// </summary>
        /// <param name="titles"></param>
        /// <param name="oldTitle"></param>
        /// <returns></returns>
        protected string formateTitle(List<FormateTitle> titles, object oldTitle)
        {
            if (oldTitle == null)
                return @"(null)";
            var _oldTitle = oldTitle.ToString();
            if (_oldTitle.IsNullOrWhiteSpace())
                return @"(空白)";
            return changeTitle(titles, _oldTitle);
        }

        /// <summary>
        /// 替換顯示訊息
        /// </summary>
        /// <param name="titles"></param>
        /// <param name="oldTitle"></param>
        /// <returns></returns>
        protected string changeTitle(List<FormateTitle> titles, string oldTitle)
        {
            if (titles != null && titles.Any(x => x.OldTitle == oldTitle))
                return titles.First(x => x.OldTitle == oldTitle).NewTitle;
            else
                return oldTitle;
        }

        protected class FormateTitle
        {
            public string OldTitle { get; set; }
            public string NewTitle { get; set; }
        }
    }
}
