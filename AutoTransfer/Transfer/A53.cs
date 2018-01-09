using AutoTransfer.Abstract;
using AutoTransfer.Commpany;
using AutoTransfer.CreateFile;
using AutoTransfer.Sample;
using AutoTransfer.SFTPConnect;
using AutoTransfer.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using static AutoTransfer.Enum.Ref;

namespace AutoTransfer.Transfer
{
    public class A53 : SFTPTransfer
    {
        #region 共用參數

        private FormatRating fr = new FormatRating();

        //ISSUER_EQUITY_TICKER commpayInfo=> Bond_Numbers,Rating_Object
        private Dictionary<string, commpayInfo> IT_info =
            new Dictionary<string, commpayInfo>();
        //GUARANTOR_EQY_TICKER, commpayInfo=> Bond_Numbers,Rating_Object
        private Dictionary<string, commpayInfo> GET_info =
            new Dictionary<string, commpayInfo>();
        private Log log = new Log();
        private string logPath = string.Empty;
        private DateTime reportDateDt = DateTime.MinValue;
        private string reportDateStr = string.Empty;
        private int verInt = 0;
        private SetFile setFile = null;
        private DateTime startTime = DateTime.MinValue;
        private ThreadTask t = new ThreadTask();
        private TableType tableType = TableType.A53;
        private string type = TableType.A53.ToString();

        #region 處理參數
        private List<Rating_Info_SampleInfo> sampleInfos = new List<Rating_Info_SampleInfo>();
        #endregion

        #endregion 共用參數

        /// <summary>
        /// start Transfer
        /// </summary>
        /// <param name="dateTime"></param>
        public override void startTransfer(string dateTime)
        {
            IFRS9Entities db = new IFRS9Entities();
            startTime = DateTime.Now;

            if (dateTime.Length != 8 ||
               !DateTime.TryParseExact(dateTime, "yyyyMMdd", null,
               System.Globalization.DateTimeStyles.AllowWhiteSpaces,
               out reportDateDt))
            {
                db.Dispose();
                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    1,
                    logPath,
                    MessageType.DateTime_Format_Fail.GetDescription()
                    );
            }
            
            var A41 = db.Bond_Account_Info.AsNoTracking()
               .Any(x => x.Report_Date == reportDateDt);
            verInt = db.Bond_Account_Info
                .Where(x => x.Report_Date == reportDateDt && x.Version != null)
                .DefaultIfEmpty().Max(x => x.Version == null ? 0 : x.Version.Value);
            var check = log.checkTransferCheck(TableType.A53.ToString(), "A41", reportDateDt, verInt);
            logPath = log.txtLocation(type);
            if (!A41 ||
               !check || verInt == 0)
            {
                db.Dispose();
                List<string> errs = new List<string>();
                if (!A41)
                    errs.Add(MessageType.not_Find_Any.GetDescription("A41"));
                if (!check || verInt == 0)
                    errs.Add(MessageType.transferError.GetDescription());
                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    1,
                    logPath,
                    string.Join(",", errs)
                    );
            }
            else
            {
                db.Dispose();
                reportDateStr = dateTime;
                setFile = new SetFile(tableType, dateTime);
                createSampleFile();
            }
        }

        /// <summary>
        /// 建立 Sample 要Put的檔案
        /// </summary>
        protected override void createSampleFile()
        {
            //建立  檔案
            // create ex:sampleA53_20170807
            if (new CreateSampleFile().create(tableType, reportDateStr, verInt))
            {
                //把資料送給SFTP
                putSampleSFTP();
            }
            else
            {
                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    1,
                    logPath,
                    MessageType.Create_Sample_File_Fail.GetDescription()
                    );
            }
        }

        /// <summary>
        /// SFTP Put Sample檔案
        /// </summary>
        protected override void putSampleSFTP()
        {
            string error = string.Empty;
            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
                .Put(string.Empty,
                 setFile.putSampleFilePath(),
                 setFile.putSampleFileName(),
                 out error);
            if (!error.IsNullOrWhiteSpace()) //fail
            {
                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    1,
                    logPath,
                    MessageType.Put_Sample_File_Fail.GetDescription()
                    );
            }
            else //success (wait 20 min and get data)
            {
                //t.Interval = 3 * 1000;
                //Action f = () => getSampleSFTP();
                //t.Start(f); //委派 設定時間後要做的動作
                Thread.Sleep(20 * 60 * 1000);
                getSampleSFTP();
            }
        }

        /// <summary>
        /// SFTP Get Sample檔案
        /// </summary>
        protected override void getSampleSFTP()
        {
            new FileRelated().createFile(setFile.getSampleFilePath());
            string error = string.Empty;
            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
                .Get(string.Empty,
                setFile.getSampleFilePath(),
                setFile.getSampleFileName(),
                out error);
            if (!error.IsNullOrWhiteSpace())
            {
                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    1,
                    logPath,
                    MessageType.Get_Sample_File_Fail.GetDescription()
                    );
            }
            else
            {
                createCommpanyFile();
            }
        }

        /// <summary>
        /// 建立 Commpany 要Put檔案
        /// </summary>
        protected override void createCommpanyFile()
        {
            List<string> data = new List<string>();
            using (StreamReader sr = new StreamReader(Path.Combine(
                setFile.getSampleFilePath(), setFile.getSampleFileName())))
            {
                bool flag = false; //判斷是否為要讀取的資料行數
                string line = string.Empty;
                IT_info = new Dictionary<string, commpayInfo>();
                GET_info =new Dictionary<string, commpayInfo>();
                #region save Rating_Info_SampleInfo

                using (IFRS9Entities db = new IFRS9Entities())
                {
                    List<Bond_Account_Info> A41s = new List<Bond_Account_Info>();
                    StringBuilder sb = new StringBuilder();
                    sb.Append($@"
delete Rating_Info_SampleInfo where Report_Date = {reportDateDt.dateTimeToStrSql()} ;");
                    A41s = db.Bond_Account_Info.AsNoTracking()
                        .Where(x => x.Report_Date == reportDateDt &&
                        x.Version == verInt).ToList();
                    while ((line = sr.ReadLine()) != null)
                    {
                        if ("END-OF-DATA".Equals(line))
                            flag = false;
                        if (flag) //找到的資料
                        {                           
                            var arr = line.Split('|');
                            //arr[0]  ex: US00206RDH21 (債券編號)
                            //arr[1]  ex: 0
                            //arr[2]  ex: 20
                            //arr[3]  ISSUER_EQUITY_TICKER (發行人)
                            //arr[4]  ISSUE_DT (債券評等日期)
                            //arr[5]  ISSUER (債券名稱)
                            //arr[6]  GUARANTOR_EQY_TICKER (擔保人)
                            //arr[7]  GUARANTOR_NAME (擔保人名稱)
                            //--標普(S&P)
                            //arr[8]  (國外)RTG_SP (SP國外評等)
                            //arr[9] (國外)SP_EFF_DT (SP國外評等日期)
                            //--穆迪(Moody's)
                            //arr[10] (國外)RTG_MOODY (Moody's國外評等)
                            //arr[11] (國外)MOODY_EFF_DT (Moody's國外評等日期)
                            //--惠譽台灣(Fitch(twn))
                            //arr[12] (國內)RTG_FITCH_NATIONAL (惠譽國內評等)
                            //arr[13] (國內)RTG_FITCH_NATIONAL_DT (惠譽國內評等日期)
                            //--惠譽(Fitch)
                            //arr[14] (國外)RTG_FITCH (惠譽評等)
                            //arr[15] (國外)FITCH_EFF_DT (惠譽評等日期)
                            //--TRC(中華信評)
                            //arr[16] (國內)RTG_TRC (TRC 評等)
                            //arr[17] (國內)TRC_EFF_DT (TRC 評等日期)
                            //--A95 Security_Des (與A53 一起抓資料)
                            //arr[18] A95 Security_Des
                            //--SMF 條件符合取代使用
                            //arr[19] PARENT_TICKER_EXCHANGE
                            //--COLLAT_TYP
                            //arr[20] COLLAT_TYP
                            if (arr.Length >= 21)
                            {
                                var bond_Number = arr[0].Trim();
                                var A41 = A41s.First(x => x.Bond_Number == bond_Number);
                                var SMF = A41.PRODUCT;
                                var ISSUER = A41.ISSUER;
                                var LSMF = SMF.IsNullOrWhiteSpace() ? string.Empty : SMF.Substring(0, 3);
                                var ISSUER_EQUITY_TICKER = arr[3]?.Trim(); //ISSUER_EQUITY_TICKER
                                var GUARANTOR_EQY_TICKER = arr[6]?.Trim(); //GUARANTOR_EQY_TICKER
                                var GUARANTOR_NAME = arr[7]?.Trim(); //GUARANTOR_NAME
                                var Security_Des = arr[18];
                                var Bloomberg_Ticker = Security_Des.IsNullOrWhiteSpace() ? null : Security_Des.Split(' ')[0];
                                var PARENT_TICKER_EXCHANGE = arr[19]?.Trim(); //PARENT_TICKER_EXCHANGE 
                                var COLLAT_TYP = arr[20]?.Trim();
                                if (nullarr.Contains(ISSUER_EQUITY_TICKER))
                                    ISSUER_EQUITY_TICKER = null;
                                if (nullarr.Contains(GUARANTOR_EQY_TICKER))
                                    GUARANTOR_EQY_TICKER = null;
                                if (nullarr.Contains(GUARANTOR_NAME))
                                    GUARANTOR_NAME = null;
                                // insert Rating_Info_SampleInfo
                                sb.Append($@"
INSERT INTO [Rating_Info_SampleInfo]
           ([Bond_Number]
           ,[Report_Date]
           ,[ISSUER_TICKER]
           ,[GUARANTOR_EQY_TICKER]
           ,[GUARANTOR_NAME]
           ,[PARENT_TICKER_EXCHANGE]
           ,[SMF]
           ,[COLLAT_TYP]
           ,[Security_Des]
           ,[Bloomberg_Ticker]
           ,[ISSUER])
     VALUES
           ({bond_Number.stringToStrSql()}
           ,{reportDateDt.dateTimeToStrSql()}
           ,{ISSUER_EQUITY_TICKER.stringToStrSql()}
           ,{GUARANTOR_EQY_TICKER.stringToStrSql()}
           ,{GUARANTOR_NAME.stringToStrSql()}
           ,{PARENT_TICKER_EXCHANGE.stringToStrSql()}
           ,{SMF.stringToStrSql()}
           ,{COLLAT_TYP.stringToStrSql()}
           ,{Security_Des.stringToStrSql()}
           ,{Bloomberg_Ticker.stringToStrSql()}
           ,{ISSUER.stringToStrSql()}  ); ");
                            }
                        }
                        if ("START-OF-DATA".Equals(line))
                            flag = true;
                    }
                    //特別處理
                    sb.Append($@"
--select issuer from Rating_Info_SampleInfo

--公債類：
--1. If left(SMF,3)='411' then 從BBG撈<ISSUER_EQUITY_TICKER>的欄位參數改為<PARENT_TICKER_EXCHANGE>，再用這個Ticker去串發行人信評
update Rating_Info_SampleInfo
    set ISSUER_TICKER = PARENT_TICKER_EXCHANGE
where LEFT(SMF, 3) = '411'
and Report_Date = {reportDateDt.dateTimeToStrSql()};

--2.If left(SMF, 3) = '421' and(Issuer = 'GOV-Kaohsiung' or Issuer = 'GOV-TAIPEI') 
--then<ISSUER_EQUITY_TICKER> 的欄位內容放剛剛抓的<GOV-TW-CEN> 的 <PARENT_TICKER_EXCHANGE>，再用這個Ticker去串發行人信評
with TEMP421 as
(
select TOP 1
PARENT_TICKER_EXCHANGE
from Rating_Info_SampleInfo
where ISSUER = 'GOV-TW-CEN'
and PARENT_TICKER_EXCHANGE is not null
and Report_Date = {reportDateDt.dateTimeToStrSql()}
)
update Rating_Info_SampleInfo
    set ISSUER_TICKER = TEMP421.PARENT_TICKER_EXCHANGE
from TEMP421
where LEFT(SMF, 3) = '421'
and ISSUER IN('GOV-Kaohsiung', 'GOV-TAIPEI')
and Report_Date = {reportDateDt.dateTimeToStrSql()};

--3.If issuer = 'GOV-TW-CEN' or 'GOV-Kaohsiung' or 'GOV-TAIPEI' then他們的債項評等放他們發行人的評等
--ps(在A58做調整)

--SMF的C開頭

--1.Left(SMF, 1) = 'C'此類的商品會串不出 <ISSUER_EQUITY_TICKER>，但其它種類債券有一樣的Issuer，
--且有串出他的Ticker及信評，所以要找其它Left(SMF, 1) <> 'C'的商品，
--有一樣Issuer的Ticker來覆蓋他的<ISSUER_EQUITY_TICKER> 再來串信評，或直接把Ticker跟信評欄位覆蓋過來。
with TEMPC as
(
   select Issuer_Ticker, ISSUER
   from Rating_Info_SampleInfo
   where Report_Date = {reportDateDt.dateTimeToStrSql()}
   and SMF is not null
   and LEFT(SMF,1) <> 'C'
   and Issuer_Ticker is not null
   group by Issuer_Ticker,ISSUER
)
update Rating_Info_SampleInfo
    set ISSUER_TICKER = TEMPC.ISSUER_TICKER
from TEMPC
where Rating_Info_SampleInfo.ISSUER = TEMPC.ISSUER
and Report_Date = {reportDateDt.dateTimeToStrSql()}
and LEFT(SMF,1) = 'C';

--2.但目前有一個Issuer也沒有出現在Left(SMF, 1) <> 'C'的商品中：FUBON銀，
--所以需將他的<ISSUER_EQUITY_TICKER>(2830 TT Equity)維護在下方<發行者Ticker> 表格
--ps=>(Issuer_Ticker)
update Rating_Info_SampleInfo
    set ISSUER_TICKER = Issuer_Ticker.ISSUER_EQUITY_TICKER
from Issuer_Ticker
where Rating_Info_SampleInfo.ISSUER = Issuer_Ticker.Issuer
and Rating_Info_SampleInfo.Report_Date = {reportDateDt.dateTimeToStrSql()};


--補充擔保者Ticker(債項、發行者及擔保者皆無信評才需採用)

--2.還有部分Issuer的 <GUARANTOR_EQY_TICKER> 是串不出來的，需指定給 <GUARANTOR_EQY_TICKER>，再去抓擔保者信評
--ps => (Guarantor_Ticker)
update Rating_Info_SampleInfo
    set GUARANTOR_EQY_TICKER = Guarantor_Ticker.GUARANTOR_EQY_TICKER,
        GUARANTOR_NAME = Guarantor_Ticker.GUARANTOR_NAME
from Guarantor_Ticker
where Rating_Info_SampleInfo.ISSUER = Guarantor_Ticker.Issuer
and Rating_Info_SampleInfo.Report_Date = {reportDateDt.dateTimeToStrSql()};

-- 1與2 順序互換 2017/11/29 
--1.If Left(SMF, 3) = 'A11' and(Issuer = 'FREDDIE MAC' or 'FANNIE MAE' or 'GNMA') 
--then<GUARANTOR_EQY_TICKER> 固定放'3352Z US'，再用這個Ticker去串擔保者信評
update Rating_Info_SampleInfo
    set GUARANTOR_EQY_TICKER = '3352Z US'
where Report_Date = {reportDateDt.dateTimeToStrSql()}
and LEFT(SMF,3) = 'A11'
and ISSUER IN('FREDDIE MAC', 'FANNIE MAE', 'GNMA') ;

--3.此類狀況有維護一個表格，凡是表格中的Issuer，他的<GUARANTOR_NAME> 跟<GUARANTOR_EQY_TICKER> 就都給表格內容，再去串他的擔保者信評
--ps(在A57做調整)
");
                    try
                    {
                        db.Database.ExecuteSqlCommand(sb.ToString());
                        sampleInfos = new List<Rating_Info_SampleInfo>();
                        sampleInfos = db.Rating_Info_SampleInfo.AsNoTracking()
                            .Where(x => x.Report_Date == reportDateDt).ToList();
                        sampleInfos.ForEach(y =>
                        {
                            if (!y.ISSUER_TICKER.IsNullOrWhiteSpace())
                            {
                                commpayInfo x = new commpayInfo();
                                if (!IT_info.TryGetValue(y.ISSUER_TICKER, out x))
                                {
                                    data.Add(y.ISSUER_TICKER);
                                    IT_info.Add(y.ISSUER_TICKER, new commpayInfo()
                                    {
                                        Bond_Number = new List<string>() { y.Bond_Number },
                                        Rating_Object = RatingObject.ISSUER.GetDescription()
                                    });
                                }
                                else
                                {
                                    x.Bond_Number.Add(y.Bond_Number);
                                }
                            }
                            if (!y.GUARANTOR_EQY_TICKER.IsNullOrWhiteSpace())
                            {
                                commpayInfo x = new commpayInfo();
                                if (!GET_info.TryGetValue(y.GUARANTOR_EQY_TICKER, out x))
                                {
                                    data.Add(y.GUARANTOR_EQY_TICKER);
                                    GET_info.Add(y.GUARANTOR_EQY_TICKER, new commpayInfo()
                                    {
                                        Bond_Number = new List<string>() { y.Bond_Number },
                                        Rating_Object = RatingObject.GUARANTOR.GetDescription()
                                    });
                                }
                                else
                                {
                                    x.Bond_Number.Add(y.Bond_Number);
                                }
                            }
                        });
                        data = data.Distinct().ToList(); //去重複
                        if (new CreateCommpanyFile().create(tableType, reportDateStr, data))
                        {
                            putCommpanySFTP();
                        }
                        else
                        {
                            log.bothLog(
                                type,
                                false,
                                reportDateDt,
                                startTime,
                                DateTime.Now,
                                1,
                                logPath,
                                MessageType.Create_Commpany_File_Fail.GetDescription()
                                );
                        }
                    }
                    catch(Exception ex)
                    {
                        log.bothLog(
                            type,
                            false,
                            reportDateDt,
                            startTime,
                            DateTime.Now,
                            1,
                            logPath,
                            $"message: {ex.Message}" +
                            $", inner message {ex.InnerException?.InnerException?.Message}"
                            );
                    }
                }
                #endregion
            }
        }

        /// <summary>
        /// SFTP Put Commpany檔案
        /// </summary>
        protected override void putCommpanySFTP()
        {
            string error = string.Empty;
            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
                .Put(string.Empty,
                setFile.putCommpanyFilePath(),
                setFile.putCommpanyFileName(), out error);
            if (!error.IsNullOrWhiteSpace()) //fail
            {
                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    1,
                    logPath,
                    MessageType.Put_Commpany_File_Fail.GetDescription()
                    );
            }
            else //success (wait 20 min and get data)
            {
                //t.Stop();
                //t.Interval = 5 * 1000;
                //Action f = () => getCommpanySFTP();
                //t.Start(f); //委派 設定時間後要做的動作
                Thread.Sleep(20 * 60 * 1000);
                getCommpanySFTP();
            }
        }

        /// <summary>
        /// SFTP Get Commpany檔案
        /// </summary>
        protected override void getCommpanySFTP()
        {
            new FileRelated().createFile(setFile.getCommpanyFilePath());
            string error = string.Empty;
            new SFTP(SFTPInfo.ip, SFTPInfo.account, SFTPInfo.password)
            .Get(string.Empty,
                 setFile.getCommpanyFilePath(),
                 setFile.getCommpanyFileName(),
                 out error);
            if (!error.IsNullOrWhiteSpace())
            {
                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    1,
                    logPath,
                    MessageType.Get_Commpanye_File_Fail.GetDescription()
                    );
            }
            else
            {
                DataToDb();
            }
        }

        /// <summary>
        /// Db save
        /// </summary>
        protected override void DataToDb()
        {
            IFRS9Entities db = new IFRS9Entities();
            List<Rating_Info> sampleData = new List<Rating_Info>();
            List<Rating_Info> commpanyData = new List<Rating_Info>();
            A53Sample a53Sample = new A53Sample();
            A53Commpany a53Commpany = new A53Commpany();
            List<StringBuilder> sbs = new List<StringBuilder>();
            #region sample Data

            using (StreamReader sr = new StreamReader(Path.Combine(
                setFile.getSampleFilePath(), setFile.getSampleFileName())))
            {
                bool flag = false; //判斷是否為要讀取的資料行數
                string line = string.Empty;
                while ((line = sr.ReadLine()) != null)
                {
                    if ("END-OF-DATA".Equals(line))
                        flag = false;
                    if (flag) //找到的資料
                    {
                        var arr = line.Split('|');
                        //arr[0]  ex: US00206RDH21 (債券編號)
                        //arr[1]  ex: 0
                        //arr[2]  ex: 20
                        //arr[3]  ISSUER_EQUITY_TICKER (發行人)
                        //arr[4]  ISSUE_DT (債券評等日期)
                        //arr[5]  ISSUER (債券名稱)
                        //arr[6]  GUARANTOR_EQY_TICKER (擔保人)
                        //arr[7]  GUARANTOR_NAME (擔保人名稱)
                        //--標普(S&P)
                        //arr[8]  (國外)RTG_SP (SP國外評等)
                        //arr[9] (國外)SP_EFF_DT (SP國外評等日期)
                        //--穆迪(Moody's)
                        //arr[10] (國外)RTG_MOODY (Moody's國外評等)
                        //arr[11] (國外)MOODY_EFF_DT (Moody's國外評等日期)
                        //--惠譽台灣(Fitch(twn))
                        //arr[12] (國內)RTG_FITCH_NATIONAL (惠譽國內評等)
                        //arr[13] (國內)RTG_FITCH_NATIONAL_DT (惠譽國內評等日期)
                        //--惠譽(Fitch)
                        //arr[14] (國外)RTG_FITCH (惠譽評等)
                        //arr[15] (國外)FITCH_EFF_DT (惠譽評等日期)
                        //--TRC(中華信評)
                        //arr[16] (國內)RTG_TRC (TRC 評等)
                        //arr[17] (國內)TRC_EFF_DT (TRC 評等日期)
                        //--A95 Security_Des (與A53 一起抓資料)
                        //arr[18] A95 Security_Des
                        //--SMF 條件符合取代使用
                        //arr[19] PARENT_TICKER_EXCHANGE
                        //--COLLAT_TYP
                        //arr[20] COLLAT_TYP
                        if (arr.Length >= 21)
                        {
                            //S&P國外評等
                            validateSample(
                                arr[8],
                                arr[9],
                                arr[0],
                                A53SampleBloombergField.RTG_SP.ToString(),
                                RatingOrg.SP,
                                sampleData);
                            //Moody's國外評等
                            validateSample(
                                arr[10],
                                arr[11],
                                arr[0],
                                A53SampleBloombergField.RTG_MOODY.ToString(),
                                RatingOrg.Moody,
                                sampleData);
                            //惠譽台灣
                            validateSample(
                                arr[12],
                                arr[13],
                                arr[0],
                                A53SampleBloombergField.RTG_FITCH_NATIONAL.ToString(),
                                RatingOrg.FitchTwn,
                                sampleData);
                            //惠譽
                            validateSample(
                                arr[14],
                                arr[15],
                                arr[0],
                                A53SampleBloombergField.RTG_FITCH.ToString(),
                                RatingOrg.Fitch,
                                sampleData);
                            //TRC(中華信評)
                            validateSample(
                                arr[16],
                                arr[17],
                                arr[0],
                                A53SampleBloombergField.RTG_TRC.ToString(),
                                RatingOrg.CW,
                                sampleData);
                        }
                    }
                    if ("START-OF-DATA".Equals(line))
                        flag = true;
                }
            }

            #endregion sample Data

            #region commpany Data

            using (StreamReader sr = new StreamReader(Path.Combine(
                setFile.getCommpanyFilePath(), setFile.getCommpanyFileName())))
            {
                bool flag = false; //判斷是否為要讀取的資料行數
                string line = string.Empty;
                while ((line = sr.ReadLine()) != null)
                {
                    if ("END-OF-DATA".Equals(line))
                        flag = false;
                    if (flag) //找到的資料
                    {
                        var arr = line.Split('|');
                        //arr[0]  ex: T US Equity (發行人or擔保人)
                        //arr[1]  ex: 0
                        //arr[2]  ex: 38
                        //arr[3]  ID_BB_COMPANY  公司ID
                        //arr[4]  LONG_COMP_NAME  公司名稱
                        //arr[5]  COUNTRY_ISO  城市(國家)
                        //arr[6]  INDUSTRY_GROUP
                        //arr[7]  INDUSTRY_SECTOR
                        //--標普(S&P)
                        //arr[8]  (國外)RTG_SP_LT_LC_ISSUER_CREDIT (標普本國貨幣長期發行人信用評等)
                        //arr[9]  (國外)RTG_SP_LT_LC_ISS_CRED_RTG_DT (標普本國貨幣長期發行人信用評等日期)
                        //arr[10] (國外)RTG_SP_LT_FC_ISSUER_CREDIT (標普長期外幣發行人信用評等)
                        //arr[11] (國外)RTG_SP_LT_FC_ISS_CRED_RTG_DT (標普長期外幣發行人信用評等日期)
                        //--穆迪(Moody's)
                        //arr[12] (國內)RTG_MDY_LOCAL_LT_BANK_DEPOSITS (穆迪長期本國銀行存款評等)
                        //arr[13] (國內)RTG_MDY_LT_LC_BANK_DEP_RTG_DT (穆迪長期本國銀行存款評等日期)
                        //arr[14] (國外)RTG_MDY_FC_CURR_ISSUER_RATING (穆迪外幣發行人評等)
                        //arr[15] (國外)RTG_MDY_FC_CURR_ISSUER_RTG_DT (穆迪外幣發行人評等日期)
                        //arr[16] (國外)RTG_MDY_ISSUER (穆迪發行人評等)
                        //arr[17] (國外)RTG_MDY_ISSUER_RTG_DT (穆迪發行人評等日期)
                        //arr[18] (國外)RTG_MOODY_LONG_TERM (穆迪長期評等)
                        //arr[19] (國外)RTG_MOODY_LONG_TERM_DATE (穆迪長期評等日期)
                        //arr[20] (國外)RTG_MDY_SEN_UNSECURED_DEBT (穆迪優先無擔保債務評等)
                        //arr[21] (國外)RTG_MDY_SEN_UNSEC_RTG_DT (穆迪優先無擔保債務評等日期)
                        //--惠譽(Fitch)
                        //arr[22] (國外)RTG_FITCH_LT_ISSUER_DEFAULT (惠譽長期發行人違約評等)
                        //arr[23] (國外)RTG_FITCH_LT_ISSUER_DFLT_RTG_DT (惠譽長期發行人違約評等日期)
                        //arr[24] (國外)RTG_FITCH_LT_FC_ISSUER_DEFAULT (惠譽長期外幣發行人違約評等)
                        //arr[25] (國外)RTG_FITCH_LT_FC_ISS_DFLT_RTG_DT (惠譽長期外幣發行人違約評等日期)
                        //arr[26] (國外)RTG_FITCH_LT_LC_ISSUER_DEFAULT (惠譽長期本國貨幣發行人違約評等)
                        //arr[27] (國外)RTG_FITCH_LT_LC_ISS_DFLT_RTG_DT (惠譽長期本國貨幣發行人違約評等日期)
                        //arr[28] (國外)RTG_FITCH_SEN_UNSECURED (惠譽優先無擔保債務評等)
                        //arr[29] (國外)RTG_FITCH_SEN_UNSEC_RTG_DT (惠譽優先無擔保債務評等日期)
                        //--惠譽台灣(Fitch(twn))
                        //arr[30] (國內)RTG_FITCH_NATIONAL_LT (惠譽國內長期評等)
                        //arr[31] (國內)RTG_FITCH_NATIONAL_LT_DT (惠譽國內長期評等日期)
                        //--TRC(中華信評)
                        //arr[32] (國內)RTG_TRC_LONG_TERM (TRC 長期評等)
                        //arr[33] (國內)RTG_TRC_LONG_TERM_RTG_DT (TRC 長期評等日期)
                        if (arr.Length >= 34)
                        {
                            //RTG_SP_LT_LC_ISSUER_CREDIT 標普本國貨幣長期發行人信用評等
                            validateCommpany(
                                arr[8],
                                arr[9],
                                arr[0],
                                A53CommpanyBloombergField.RTG_SP_LT_LC_ISSUER_CREDIT.ToString(),
                                RatingOrg.SP,
                                commpanyData);
                            //RTG_SP_LT_FC_ISSUER_CREDIT 標普長期外幣發行人信用評等
                            validateCommpany(
                                arr[10],
                                arr[11],
                                arr[0],
                                A53CommpanyBloombergField.RTG_SP_LT_FC_ISSUER_CREDIT.ToString(),
                                RatingOrg.SP,
                                commpanyData);
                            //RTG_MDY_LOCAL_LT_BANK_DEPOSITS 穆迪長期本國銀行存款評等
                            validateCommpany(
                                arr[12],
                                arr[13],
                                arr[0],
                                A53CommpanyBloombergField.RTG_MDY_LOCAL_LT_BANK_DEPOSITS.ToString(),
                                RatingOrg.Moody,
                                commpanyData);
                            //RTG_MDY_FC_CURR_ISSUER_RATING  穆迪外幣發行人評等
                            validateCommpany(
                                arr[14],
                                arr[15],
                                arr[0],
                                A53CommpanyBloombergField.RTG_MDY_FC_CURR_ISSUER_RATING.ToString(),
                                RatingOrg.Moody,
                                commpanyData
                                );
                            //RTG_MDY_ISSUER  穆迪發行人評等
                            validateCommpany(
                                arr[16],
                                arr[17],
                                arr[0],
                                A53CommpanyBloombergField.RTG_MDY_ISSUER.ToString(),
                                RatingOrg.Moody,
                                commpanyData);
                            //RTG_MOODY_LONG_TERM  穆迪長期評等
                            validateCommpany(
                                arr[18],
                                arr[19],
                                arr[0],
                                A53CommpanyBloombergField.RTG_MOODY_LONG_TERM.ToString(),
                                RatingOrg.Moody,
                                commpanyData);
                            //RTG_MDY_SEN_UNSECURED_DEBT  穆迪優先無擔保債務評等
                            validateCommpany(
                                arr[20],
                                arr[21],
                                arr[0],
                                A53CommpanyBloombergField.RTG_MDY_SEN_UNSECURED_DEBT.ToString(),
                                RatingOrg.Moody,
                                commpanyData);
                            //RTG_FITCH_LT_ISSUER_DEFAULT 惠譽長期發行人違約評等
                            validateCommpany(
                                arr[22],
                                arr[23],
                                arr[0],
                                A53CommpanyBloombergField.RTG_FITCH_LT_ISSUER_DEFAULT.ToString(),
                                RatingOrg.Fitch,
                                commpanyData);

                            //RTG_FITCH_LT_FC_ISSUER_DEFAULT  惠譽長期外幣發行人違約評等
                            validateCommpany(
                                arr[24],
                                arr[25],
                                arr[0],
                                A53CommpanyBloombergField.RTG_FITCH_LT_FC_ISSUER_DEFAULT.ToString(),
                                RatingOrg.Fitch,
                                commpanyData);
                            //RTG_FITCH_LT_LC_ISSUER_DEFAULT  惠譽長期本國貨幣發行人違約評等
                            validateCommpany(
                                arr[26],
                                arr[27],
                                arr[0],
                                A53CommpanyBloombergField.RTG_FITCH_LT_LC_ISSUER_DEFAULT.ToString(),
                                RatingOrg.Fitch,
                                commpanyData);
                            //RTG_FITCH_SEN_UNSECURED  惠譽優先無擔保債務評等
                            validateCommpany(
                                arr[28],
                                arr[29],
                                arr[0],
                                A53CommpanyBloombergField.RTG_FITCH_SEN_UNSECURED.ToString(),
                                RatingOrg.Fitch,
                                commpanyData);
                            //RTG_FITCH_NATIONAL_LT 惠譽國內長期評等
                            validateCommpany(
                                arr[30],
                                arr[31],
                                arr[0],
                                A53CommpanyBloombergField.RTG_FITCH_NATIONAL_LT.ToString(),
                                RatingOrg.FitchTwn,
                                commpanyData);
                            //RTG_TRC_LONG_TERM  (國內)TRC 長期評等
                            validateCommpany(
                                arr[32],
                                arr[33],
                                arr[0],
                                A53CommpanyBloombergField.RTG_TRC_LONG_TERM.ToString(),
                                RatingOrg.CW,
                                commpanyData);
                        }
                    }
                    if ("START-OF-DATA".Equals(line))
                        flag = true;
                }
            }

            #endregion commpany Data

            #region saveDb

            if (sampleData.Any() || commpanyData.Any())
            {
                
                #region save Rating_Info
                sbs.Add(new StringBuilder($@"
delete Rating_Info where Report_Date = {reportDateDt.dateTimeToStrSql()}
"));
                sampleData.ForEach(x => {
                    sbs.Add(new StringBuilder($@"
INSERT INTO Rating_Info
           ([Bond_Number]
           ,[Rating_Date]
           ,[Rating_Object]
           ,[Rating]
           ,[RTG_Bloomberg_Field]
           ,[Report_Date]
           ,[Rating_Org])
     VALUES
           ({x.Bond_Number.stringToStrSql()}
           ,{x.Rating_Date.dateTimeNToStrSql()}
           ,{x.Rating_Object.stringToStrSql()}
           ,{x.Rating.stringToStrSql()}
           ,{x.RTG_Bloomberg_Field.stringToStrSql()}
           ,{x.Report_Date.dateTimeToStrSql()}
           ,{x.Rating_Org.stringToStrSql()}) ;
"));
                });
                commpanyData.ForEach(x => {
                    sbs.Add(new StringBuilder($@"
INSERT INTO Rating_Info
           ([Bond_Number]
           ,[Rating_Date]
           ,[Rating_Object]
           ,[Rating]
           ,[RTG_Bloomberg_Field]
           ,[Report_Date]
           ,[Rating_Org])
     VALUES
           ({x.Bond_Number.stringToStrSql()}
           ,{x.Rating_Date.dateTimeNToStrSql()}
           ,{x.Rating_Object.stringToStrSql()}
           ,{x.Rating.stringToStrSql()}
           ,{x.RTG_Bloomberg_Field.stringToStrSql()}
           ,{x.Report_Date.dateTimeToStrSql()}
           ,{x.Rating_Org.stringToStrSql()}) ;
"));
                });
                #endregion
                #region A95 特殊　PRODUCT
                List<string> products = new List<string>()
                {
                    "411 Gov.CENTRAL",
                    "931 CDO",
                    "A11 AGENCY MBS",
                    "932 CLO",
                    "421 Gov.LOCAL"
                };
                #endregion
                #region update A95 Security_Des & Bloomberg_Ticker AND A41 & A95 Bond_Type & Assessment_Sub_Kind
                //A45
                var A45Datas = db.Bond_Category_Info.AsNoTracking().ToList();
                var A41Datas = db.Bond_Account_Info.AsNoTracking().Where(x => x.Report_Date == reportDateDt && x.Version == verInt).ToList();
                //A95
                db.Bond_Ticker_Info.AsNoTracking().Where(x => x.Report_Date == reportDateDt &&
                x.Version == verInt && 
                !products.Contains(x.PRODUCT) && 
                x.Bond_Number != null).ToList().GroupBy(x=>x.Bond_Number)
                .Select(x=>x.First()).ToList()
                .ForEach(x =>
                {
                    //obj => Rating_Info_SampleInfo
                    var obj = sampleInfos.FirstOrDefault(y => y.Bond_Number == x.Bond_Number);
                    if (obj != null)
                    {
                        if (!obj.Security_Des.IsNullOrWhiteSpace() && obj.Security_Des != x.Security_Des)
                        {
                            x.Security_Des = obj.Security_Des;
                            x.Bloomberg_Ticker = obj.Bloomberg_Ticker;
                            x.Processing_Date = startTime;
                            var A45Data = A45Datas.FirstOrDefault(z =>
                            z.Bloomberg_Ticker == x.Bloomberg_Ticker);
                            if (A45Data != null)
                            {
                                var Assessment_Sub_Kind = formateAssessmentSubKind(A45Data.Assessment_Sub_Kind, x.Lien_position);
                                var Bond_Type = formateBondType(A45Data.Bond_Type);
                                sbs.Add(new StringBuilder($@"
UPDATE  Bond_Ticker_Info
SET Security_Des = {obj.Security_Des.stringToStrSql()} ,
    Bloomberg_Ticker = {obj.Bloomberg_Ticker.stringToStrSql()} ,
    Processing_Date = {startTime.dateTimeToStrSql()} ,
    Assessment_Sub_Kind = {Assessment_Sub_Kind.stringToStrSql()},
    Bond_Type = {Bond_Type.stringToStrSql()}
WHERE Report_Date = {reportDateDt.dateTimeToStrSql()}
AND  Version = {verInt.ToString()}
AND  Bond_Number = {obj.Bond_Number.stringToStrSql()} ;

UPDATE Bond_Account_Info
SET Assessment_Sub_Kind = {Assessment_Sub_Kind.stringToStrSql()},
    Bond_Type = {Bond_Type.stringToStrSql()} ,
    Processing_Date = {startTime.dateTimeToStrSql()} 
WHERE Report_Date = {reportDateDt.dateTimeToStrSql()}
AND  Version = {verInt.ToString()}
AND  Bond_Number = {obj.Bond_Number.stringToStrSql()} ;
"));
                            }
                            else
                            {
                                sbs.Add(new StringBuilder($@"
UPDATE  Bond_Ticker_Info
SET Security_Des = {obj.Security_Des.stringToStrSql()} ,
    Bloomberg_Ticker = {obj.Bloomberg_Ticker.stringToStrSql()} ,
    Processing_Date = {startTime.dateTimeToStrSql()}
WHERE Report_Date = {reportDateDt.dateTimeToStrSql()}
AND  Version = {verInt.ToString()}
AND  Bond_Number = {obj.Bond_Number.stringToStrSql()} ;
"));
                            }
                        }
                    }
                });
                #endregion

                string lastA95 = $@"
--A95TEMP
with TEMP2 AS
(
select 
TOP 1
Report_Date ,Version 
from Bond_Account_Info 
where 
(
Report_Date != {reportDateDt.dateTimeToStrSql()}
or 
Version != {verInt.ToString()}
)
group by Report_Date ,Version
order by Report_Date DESC,Version DESC
),
TEMP AS 
(
select A95.Bond_Number,
       A95.Lots,
	   A95.Portfolio_Name,
	   A95.Security_Des,
	   A95.Bloomberg_Ticker
from   Bond_Ticker_Info A95,TEMP2
where  A95.Report_Date = TEMP2.Report_Date
and    A95.Version = TEMP2.Version
and    A95.Bloomberg_Ticker is not null
),
";
                sbs.Add( new StringBuilder(
                                      lastA95 +
                  $@"
A95TEMP AS
(
select 
A95.Report_Date,
A95.Version,
A95.Lots,
A95.Bond_Number,
A95.Portfolio_Name,
T1.Bloomberg_Ticker,
T1.Security_Des,
CASE WHEN A45.Bond_Type = 'Quasi Sovereign'
     THEN '主權及國營事業債'
	 WHEN A45.Bond_Type = 'Non Quasi Sovereign'
	 THEN '其他債券'
	 ELSE A45.Bond_Type
	 END  AS Bond_Type,
CASE WHEN (A45.Assessment_Sub_Kind = '金融債' AND A95.Lien_position = '次順位' )
     THEN '金融債次順位債券'
	 WHEN (A45.Assessment_Sub_Kind = '金融債' AND( A95.Lien_position = '' OR A95.Lien_position is null) )
	 THEN '金融債主順位債券'
	 ELSE A45.Assessment_Sub_Kind
	 END AS Assessment_Sub_Kind
from  
Bond_Ticker_Info A95
JOIN TEMP T1
ON A95.Lots = T1.Lots
AND A95.Bond_Number = T1.Bond_Number
AND A95.Portfolio_Name = T1.Portfolio_Name
JOIN Bond_Category_Info A45
ON T1.Bloomberg_Ticker = A45.Bloomberg_Ticker
where A95.Report_Date = {reportDateDt.dateTimeToStrSql()}
and A95.version = {verInt.ToString()}
and A95.Security_Des is null
and A95.Bloomberg_Ticker is null
and A95.Bond_Type is null
and A95.Assessment_Sub_Kind is null
)
update Bond_Ticker_Info 
set Assessment_Sub_Kind = A95TEMP.Assessment_Sub_Kind,
    Bond_Type = A95TEMP.Bond_Type
from Bond_Ticker_Info A95, A95TEMP 
where A95.Report_Date = A95TEMP.Report_Date
and A95.Version = A95TEMP.Version
and A95.Lots = A95TEMP.Lots
and A95.Bond_Number = A95TEMP.Bond_Number
and A95.Portfolio_Name = A95TEMP.Portfolio_Name ;
"
                    ));
                sbs.Add(
                    new StringBuilder(
                         lastA95 +
                    $@"
A41TEMP AS
(
select A41.Reference_Nbr,
CASE WHEN A45.Bond_Type = 'Quasi Sovereign'
     THEN '主權及國營事業債'
	 WHEN A45.Bond_Type = 'Non Quasi Sovereign'
	 THEN '其他債券'
	 ELSE A45.Bond_Type
	 END  AS Bond_Type,
CASE WHEN (A45.Assessment_Sub_Kind = '金融債' AND A41.Lien_position = '次順位' )
     THEN '金融債次順位債券'
	 WHEN (A45.Assessment_Sub_Kind = '金融債' AND( A41.Lien_position = '' OR A41.Lien_position is null) )
	 THEN '金融債主順位債券'
	 ELSE A45.Assessment_Sub_Kind
	 END AS Assessment_Sub_Kind
from Bond_Account_Info A41
JOIN TEMP T1
ON A41.Lots = T1.Lots
AND A41.Bond_Number = T1.Bond_Number
AND A41.Portfolio_Name = T1.Portfolio_Name
JOIN Bond_Category_Info A45
ON T1.Bloomberg_Ticker = A45.Bloomberg_Ticker
where A41.Report_Date = {reportDateDt.dateTimeToStrSql()}
and A41.version = {verInt.ToString()}
and A41.Bond_Type is null
and A41.Assessment_Sub_Kind is null
)
update Bond_Account_Info
set Assessment_Sub_Kind = A41TEMP.Assessment_Sub_Kind,
    Bond_Type = A41TEMP.Bond_Type
FROM Bond_Account_Info A41,  A41TEMP
where A41.Reference_Nbr = A41TEMP.Reference_Nbr ;
"
                        ));

                using (var dbContextTransaction = db.Database.BeginTransaction())
                {
                    db.Database.CommandTimeout = 300;
                    try
                    {
                        int size = 1000;
                        for (int q = 0; (sbs.Count() / size) >= q; q += 1)
                        {
                            StringBuilder sql = new StringBuilder();
                            sbs.Skip((q) * size).Take(size).ToList()
                                .ForEach(x =>
                                {
                                    sql.Append(x.ToString());
                                });
                            db.Database.ExecuteSqlCommand(sql.ToString());
                        }
                        dbContextTransaction.Commit();
                        log.bothLog(
                            type,
                            true,
                            reportDateDt,
                            startTime,
                            DateTime.Now,
                            1,
                            logPath,
                            MessageType.Success.GetDescription()
                            );
                        new CompleteEvent().saveDb(reportDateDt, verInt);
                    }
                    catch (Exception ex)
                    {
                        dbContextTransaction.Rollback(); //Required according to MSDN article 
                        log.bothLog(
                            type,
                            false,
                            reportDateDt,
                            startTime,
                            DateTime.Now,
                            1,
                            logPath,
                            $"message: {ex.Message}" +
                            $", inner message {ex.InnerException?.InnerException?.Message}"
                            );
                    }
                    finally
                    {
                        db.Dispose();
                    }
                }
            }
            else
            {
                log.bothLog(
                    type,
                    false,
                    reportDateDt,
                    startTime,
                    DateTime.Now,
                    1,
                    logPath,
                    string.Format("{0} ({1}) {2}", TableType.A53.GetDescription(),
                    TableType.A53.ToString(), "回傳文件沒有新增任何資料")
                    );
            }
            #endregion saveDb
        }

        public class sampleInfo
        {
            public string Bond_Number { get; set; }
            public string GUARANTOR_EQY_TICKER { get; set; }
            public string GUARANTOR_NAME { get; set; }
            public string ISSUER_TICKER { get; set; }
            public string SMF { get; set; }
            public string PARENT_TICKER_EXCHANGE { get; set; }
            public string Bloomberg_Ticker { get; set; }
            public string Security_Des { get; set; }
        }

        #region private function

        /// <summary>
        /// 判斷 sample 是否新增
        /// </summary>
        /// <param name="rating">評等內容</param>
        /// <param name="ratingDate">評等時間</param>
        /// <param name="bondNumber">債券編號</param>
        /// <param name="bloombergField">Bloomberg評等欄位名稱</param>
        /// <param name="org">評等機構</param>
        /// <param name="sampleData">Sample要新增的資料</param>
        private void validateSample(
            string rating,
            string ratingDate,
            string bondNumber,
            string bloombergField,
            RatingOrg org,
            List<Rating_Info> sampleData
            )
        {
            rating = fr.forRating(rating,org); //ForMate Rating
            rating = fr.forRating2(rating,org); //formate 惠譽(台灣)
            if (!rating.IsNullOrWhiteSpace() &&
                !nullarr.Contains(rating.Trim())) //Sample評等判斷
            {
                sampleData.Add(saveSample(
                    rating,
                    ratingDate,
                    bondNumber,
                    bloombergField,
                    org
                    ));
            }
        }

        /// <summary>
        /// 判斷 commpany 是否新增
        /// </summary>
        /// <param name="rating">評等內容</param>
        /// <param name="ratingDate">評等時間</param>
        /// <param name="ticker">ISSUER_TICKER or GUARANTOR_EQY_TICKER</param>
        /// <param name="bloombergField">Bloomberg評等欄位名稱</param>
        /// <param name="org">評等機構</param>
        /// <param name="commpanyData">Commpany要新增的資料</param>
        private void validateCommpany(
            string rating,
            string ratingDate,
            string ticker,
            string bloombergField,
            RatingOrg org,
            List<Rating_Info> commpanyData
            )
        {
            rating = fr.forRating(rating, org); //ForMate Rating
            rating = fr.forRating2(rating, org); //formate 惠譽(台灣)
            if (!rating.IsNullOrWhiteSpace() &&
                !nullarr.Contains(rating.Trim())) //Commpany評等判斷
            {
                commpayInfo cInfo = new commpayInfo();
                if (IT_info.TryGetValue(ticker.FormatEquity(), out cInfo))
                {
                    cInfo.Bond_Number.ForEach(
                        x => commpanyData.Add(
                            saveCommpany(
                              rating,
                              ratingDate,
                              x,
                              bloombergField,
                              cInfo.Rating_Object,
                              org
                            ))
                        );
                }
                cInfo = new commpayInfo();
                if (GET_info.TryGetValue(ticker.FormatEquity(), out cInfo))
                {
                    cInfo.Bond_Number.ForEach(
                        x => commpanyData.Add(
                            saveCommpany(
                              rating,
                              ratingDate,
                              x,
                              "G_"+bloombergField,
                              cInfo.Rating_Object,
                              org
                            ))
                        );
                }
            }
        }

        /// <summary>
        /// return new Rating_Info (sample)
        /// </summary>
        /// <param name="rating">評等內容</param>
        /// <param name="ratingDate">評等時間</param>
        /// <param name="bondNumber">債券編號</param>
        /// <param name="bloombergField">Bloomberg評等欄位名稱</param>
        /// <param name="org">評等機構</param>
        /// <returns></returns>
        private Rating_Info saveSample(
            string rating,
            string ratingDate,
            string bondNumber,
            string bloombergField,
            RatingOrg org
            )
        {
            return new Rating_Info()
            {
                Bond_Number = bondNumber.Trim(), //債券編號
                Rating_Date = ratingDate.StringToDateTimeN(), //評等時間
                Rating_Object = RatingObject.Bonds.GetDescription(), //評等對象(發行人,債項,保證人)
                Rating = rating.Trim(), //評等內容
                RTG_Bloomberg_Field = bloombergField, //Bloomberg評等欄位名稱
                Rating_Org = org.GetDescription(), //評等機構
                Report_Date = reportDateDt //評估日/報導日
            };
        }

        /// <summary>
        /// return new Rating_Info (commpany)
        /// </summary>
        /// <param name="rating">評等內容</param>
        /// <param name="ratingDate">評等時間</param>
        /// <param name="bondNumber">債券編號</param>
        /// <param name="bloombergField">Bloomberg評等欄位名稱</param>
        /// <param name="obj">評等對象</param>
        /// <param name="org">評等機構</param>
        /// <returns></returns>
        private Rating_Info saveCommpany(
            string rating,
            string ratingDate,
            string bondNumber,
            string bloombergField,
            string obj,
            RatingOrg org
            )
        {
            return new Rating_Info()
            {
                Bond_Number = bondNumber.Trim(), //債券編號
                Rating_Date = ratingDate.StringToDateTimeN(), //評等時間
                Rating_Object = obj, //評等對象(發行人,債項,保證人)
                Rating = rating.Trim(), //評等內容
                RTG_Bloomberg_Field = bloombergField, //Bloomberg評等欄位名稱
                Rating_Org = org.GetDescription(), //評等機構
                Report_Date = reportDateDt //評估日/報導日
            };
        }

        /// <summary>
        /// formate A95 BondType
        /// </summary>
        /// <param name="bondType"></param>
        /// <returns></returns>
        private string formateBondType(string bondType)
        {
            if (bondType.IsNullOrWhiteSpace())
                return bondType;
            if (bondType.Trim() == "Quasi Sovereign")
                return "主權及國營事業債";
            if (bondType.Trim() == "Non Quasi Sovereign")
                return "其他債券";
            return bondType;
        }

        /// <summary>
        /// formate A95 AssessmentSubKind
        /// </summary>
        /// <param name="assessmentSubKind"></param>
        /// <param name="lienPosition"></param>
        /// <returns></returns>
        private string formateAssessmentSubKind(string assessmentSubKind,string lienPosition)
        {
            if (assessmentSubKind.IsNullOrWhiteSpace())
                return assessmentSubKind;
            if (assessmentSubKind.Trim() == "金融債")
            {
                if (lienPosition.IsNullOrEmpty())
                    return "金融債主順位債券";
                if (lienPosition.Trim() == "次順位")
                    return "金融債次順位債券";
            }
            return assessmentSubKind;
        }

        #endregion private function
    }
}