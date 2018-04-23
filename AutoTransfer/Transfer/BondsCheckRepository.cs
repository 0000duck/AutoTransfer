using AutoTransfer.Abstract;
using AutoTransfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.Transfer
{
    /// <summary>
    /// 減損計算過程資料檢核條件_債券
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BondsCheckRepository<T> : CheckDataAbstract<T>
        where T : class
    {
        /// <summary>
        /// 債券檢核
        /// </summary>
        /// <param name="data"></param>
        /// <param name="_event"></param>
        public BondsCheckRepository(IEnumerable<T> data, Check_Table_Type _event, DateTime? reportDate = null, int? version = null) : base(data, _event, reportDate, version)
        {
        }

        /// <summary>
        /// 設定字典資源
        /// </summary>
        protected override void Set()
        {
            _resources.Add(Check_Table_Type.Bonds_A58_Transfer_Check, A58dbModelCheck);
        }


        #region 執行信評轉檔
        private List<messageTable> A58dbModelCheck()
        {
            List<messageTable> result = new List<messageTable>();
            if (_data.Any())
            {
                var data = _data.Cast<Bond_Rating_Summary>().ToList();
                var _first = data.First();

                var _RO = Rating_Type.A.GetDescription();
                var _RR = Rating_Type.B.GetDescription();
                var _ratingType_O = new List<Bond_Rating_Summary>();
                var _ratingType_R = new List<Bond_Rating_Summary>();
                data.Where(x => x.Rating_Type == _RO)
                    .GroupBy(x => x.Reference_Nbr).ToList()
                    .ForEach(x =>
                    {
                        if (!x.Any(y => y.Grade_Adjust != null))
                            _ratingType_O.AddRange(x);
                    });
                data.Where(x => x.Rating_Type == _RR)
                    .GroupBy(x => x.Reference_Nbr).ToList()
                    .ForEach(x =>
                    {
                        if (!x.Any(y => y.Grade_Adjust != null))
                            _ratingType_R.AddRange(x);
                    });

                StringBuilder sb = new StringBuilder();
                //A.資料群組統計(內容分類筆數統計)
                sb.AppendLine(@"A.資料群組統計(內容分類筆數統計) (A58:Bond_Rating_Summary)");
                sb.AppendLine($@"1.完全缺乏原始投資信評的債券筆數統計 : {_ratingType_O.Count} 筆");
                if (_ratingType_O.Any())
                    groupData(_ratingType_O.GroupBy(x => x.Bond_Number).OrderBy(x => x.Key), sb);
                else
                    sb.AppendLine(@"所有債券均有原始投資信評資料");
                sb.AppendLine(string.Empty);
                sb.AppendLine($@"2.完全缺乏報導日最近信評的債券筆數統計 : {_ratingType_R.Count} 筆");
                if (_ratingType_R.Any())
                    groupData(_ratingType_R.GroupBy(x => x.Bond_Number).OrderBy(x => x.Key), sb);
                else
                    sb.AppendLine(@"所有債券均有報導日最近信評資料");
                sb.AppendLine(string.Empty);
                //sb.AppendLine($@"3.進行券種類與評估次分類 內容值的統計");
                //sb.AppendLine(@"債券種類 統計");
                //groupData(data.GroupBy(x => x.Bond_Type).OrderBy(x => x.Key), sb);
                //sb.AppendLine(@"評估次分類 統計");
                //PS: A58 無評估次分類

                _customerStr_Start = sb.ToString();

                //(1)檢查是否有  到期日(#16Maturity_Date)小於報導日的情況
                messageTable A57_1 = new messageTable()
                {
                    title = @"B.是否有rating特殊值 (A57:Bond_Rating_Info)",
                    successStr = @"信評資料如常,未有異常信評內容"
                };
                var A57Data = new List<IGrouping<string, Bond_Rating_Info>>();
                using (IFRS9Entities db = new IFRS9Entities())
                {
                    var A57Datas = db.Bond_Rating_Info.AsNoTracking()
                                     .Where(x => x.Report_Date == _first.Report_Date &&
                                                 x.Version == _first.Version);
                    A57Data = A57Datas.Where(x => x.ISIN_Changed_Ind == "Y").GroupBy(x => x.Reference_Nbr).ToList();
                    var A57s = A57Datas.Where(x => x.Rating != null && x.PD_Grade == null)
                                       .GroupBy(x => new { x.Bond_Number, x.RTG_Bloomberg_Field, x.Rating,x.Rating_Type }).ToList();
                    A57s.ForEach(x =>
                    {
                        var _parameter = $@"Bond_Number : {x.Key.Bond_Number} , Rating_Type : {x.Key.Rating_Type} , RTG_Bloomberg_Field : {x.Key.RTG_Bloomberg_Field} , Rating : {x.Key.Rating}";
                        setCheckMsg(A57_1,
                            @"信評內容有未處理到的特殊值",
                            _parameter,
                            true);
                    });
                }
                result.Add(A57_1);
                var _A41Data = new List<Bond_Account_Info>();
                var A41Data = new List<Bond_Account_Info>();
                using (IFRS9Entities db = new IFRS9Entities())
                {
                    _A41Data = db.Bond_Account_Info.AsNoTracking()
                                .Where(x => x.Report_Date == _first.Report_Date &&
                                            x.Version == _first.Version).ToList();
                    A41Data = _A41Data.Where(x => x.ISIN_Changed_Ind == "Y").ToList();
                }
                var A58Data = data.Where(x => x.ISIN_Changed_Ind == "Y").GroupBy(x => x.Reference_Nbr).ToList();
                StringBuilder sb2 = new StringBuilder();
                sb2.AppendLine(@"C.來源資料與產出資料的比較");
                sb2.AppendLine(@"1.相同版本A41的是否為換券ISIN_Changed_Ind='Y' 的資料數與A57,A58的ISIN_Changed_Ind='Y'資料數是否一致");
                if (A41Data.Count == A57Data.Count && A41Data.Count == A58Data.Count)
                {
                    sb2.AppendLine(@" 相同版本A41與A57,A58換券資料筆數一致");
                }
                else
                {
                    sb2.AppendLine(@" 相同版本A41與A58(A57)換券資料筆數不一致");
                    if (A41Data.Count != A57Data.Count)
                        sb2.AppendLine($@" A41資料筆數: {A41Data.Count}筆  A57資料筆數: {A57Data.Count}筆");
                    if (A41Data.Count != A58Data.Count)
                        sb2.AppendLine($@" A41資料筆數: {A41Data.Count}筆  A58資料筆數: {A58Data.Count}筆");
                }
                sb2.AppendLine(@"2.相同版本A41的是否為換券ISIN_Changed_Ind='Y' 的購入日(Origination_Date & Origination_Date_Old)與A57,A58的債券購入(認列)日期是否一致");
                bool _Origination_Date_Flag = true;
                StringBuilder sb3 = new StringBuilder();
                foreach (var item in A41Data)
                {
                    var _A57Data = A57Data.FirstOrDefault(x => x.Key == item.Reference_Nbr);
                    if (_A57Data != null)
                    {
                        if (_A57Data.Any(x => x.Origination_Date != item.Origination_Date ||
                                            x.Origination_Date_Old != item.Origination_Date_Old))
                        {
                            _Origination_Date_Flag = false;
                            sb3.AppendLine($@" A41與A57 不一致  Reference_Nbr {item.Reference_Nbr}");
                        }
                    }
                    else
                    {
                        _Origination_Date_Flag = false;
                        sb3.AppendLine($@" A41 Reference_Nbr {item.Reference_Nbr} , A57 null");
                    }
                    var _A58Data = A58Data.FirstOrDefault(x => x.Key == item.Reference_Nbr);
                    if (_A58Data != null)
                    {
                        if (_A58Data.Any(x => x.Origination_Date != item.Origination_Date ||
                                              x.Origination_Date_Old != item.Origination_Date_Old))
                        {
                            _Origination_Date_Flag = false;
                            sb3.AppendLine($@" A41與A58 不一致  Reference_Nbr {item.Reference_Nbr}");
                        }
                    }
                    else
                    {
                        _Origination_Date_Flag = false;
                        sb3.AppendLine($@" A41 Reference_Nbr {item.Reference_Nbr} , A58 null");
                    }
                }
                if (!_Origination_Date_Flag)
                {
                    sb2.AppendLine(@" 不一致債券清單");
                    sb2.AppendLine(sb3.ToString());
                }
                else
                    sb2.AppendLine(@" 相同版本A57,A58換券資訊與A41比對結果一致");
                sb2.AppendLine(@"3.相同版本A41的債券種類資料數與A57,A58的債券種類資料數是否一致");
                var _bond_types = _A41Data.GroupBy(x => x.Bond_Type).OrderBy(x => x.Key).ToList();
                StringBuilder sb4 = new StringBuilder();
                sb4 = compare(
                    _bond_types,
                    data.GroupBy(x => x.Reference_Nbr).Select(x => x.First())
                    .GroupBy(x => x.Bond_Type).ToList(),
                    sb4, "A41", "A58");
                if (sb4.Length > 0)
                {
                    sb2.AppendLine(@" 相同版本A41與A58(A57)債券種類筆數不一致");
                    sb2.Append(sb4);
                }
                else
                {
                    sb2.AppendLine(@" 相同版本A41與A58(A57)債券種類筆數一致");
                    groupData(_bond_types, sb2);
                    sb2.AppendLine($@" 總筆數({string.Join("+", _bond_types.Select(x => x.Key))}) : {(_bond_types.Select(x => x.Count())).Sum()}筆");
                }
                _customerStr_End = sb2.ToString();

                D73.Scheduling_Type = "信評轉檔";
                D73.Content_memo = "查核結果輸出";
                D73.Report_Date = _first.Report_Date;
                D73.Version = _first.Version;
                D73.File_Name = $@"R_S_{_first.Report_Date.ToString("yyyyMMdd")}_V{_first.Version}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.txt";
                D73.Create_Date = DateTime.Now.Date;

            }
            return result;
        }
        #endregion

        #region privateFunction
        /// <summary>
        /// 比對兩邊Group資料是否一致
        /// </summary>
        /// <typeparam name="_Data1">比對資料類別</typeparam>
        /// <typeparam name="_Data2">比對資料類別</typeparam>
        /// <param name="Data1">比對資料1</param>
        /// <param name="Data2">比對資料2</param>
        /// <param name="sb">不一致時加入訊息位置</param>
        /// <param name="Data1TableName">比對資料1類別名稱</param>
        /// <param name="Data2TableName">比對資料2類別名稱</param>
        /// <param name="compareKey">兩邊比對的key值 預設依樣</param>
        /// <param name="title_1">必對不一致時取代訊息1</param>
        /// <param name="title_2">必對不一致時取代訊息2</param>
        /// <returns></returns>
        private StringBuilder compare<_Data1, _Data2>(
            List<IGrouping<string, _Data1>> Data1,
            List<IGrouping<string, _Data2>> Data2,
            StringBuilder sb,
            string Data1TableName,
            string Data2TableName,
            Dictionary<string, string> compareKey = null,
            List<FormateTitle> title_1 = null,
            List<FormateTitle> title_2 = null
            )
        {
            bool differenceFlag = false;
            StringBuilder _result = new StringBuilder();
            if (sb == null)
                sb = new StringBuilder();
            List<string> _keys = new List<string>();
            foreach (var item in Data1)
            {
                var _compareKey = string.Empty;
                _compareKey = item.Key;
                if (compareKey != null && item.Key != null && compareKey.ContainsKey(item.Key))
                {
                    _compareKey = compareKey[item.Key];
                }
                var _Comparedata = Data2.FirstOrDefault(x => x.Key == _compareKey);
                if (_Comparedata != null)
                {
                    _keys.Add(_Comparedata.Key);
                    if (item.Count() != _Comparedata.Count())
                    {
                        differenceFlag = true;
                    }
                    _result.AppendLine($@" {Data1TableName}-{formateTitle(title_1, item.Key)} : 資料數 {item.Count()} 筆 , {Data2TableName}-{formateTitle(title_2, _Comparedata.Key)} : 資料數 {_Comparedata.Count()} 筆 ");
                }
                else
                {
                    differenceFlag = true;
                    _result.AppendLine($@" {Data1TableName}-{formateTitle(title_1, item.Key)} : 資料數 {item.Count()} 筆 ");
                }
            }
            foreach (var item in Data2.Where(x => !_keys.Contains(x.Key)))
            {
                differenceFlag = true;
                _result.AppendLine($@" {Data2TableName}-{formateTitle(title_2, item.Key)} : 資料數 {item.Count()} 筆 ");
            }
            if (differenceFlag)
                sb = _result;
            return sb;
        }
        #endregion
    }
}
