using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;

namespace UberPlug
{   /// <summary>
    /// CSVファイルの形式列挙体（通常版かFleet版か）
    /// </summary>
    public enum CsvType
    {
        Normal,
        Fleet,
        RawDB,
    }

    /// <summary>
    /// CSVデータエンティティークラス（１件分）
    /// </summary>
    public class CsvEntity
    {
        public string ID { get; set; }
        public string Date { get; set; }
        public string Amount { get; set; }
        public string Type { get; set; }
        //TODO:履歴フラグを加える
    }

    /// <summary>
    /// 勘定科目（列挙体）
    /// </summary>
    public enum KanjoKamoku
    {
        NONE,
        URIAGE,
        URIKAKEKIN,
        MIBARAIKIN,
        GENKIN,
        YOKIN,
    }

    public class MyCsvReader
    {
        /// <summary>
        /// CSVデータロード
        /// </summary>
        /// <param name="csvFileFullPath"></param>
        /// <returns></returns>
        public List<CsvEntity> Load(string csvFileFullPath, CsvType csvType = CsvType.Fleet)
        {
            var result = new List<CsvEntity>();
            using (var reader = new StreamReader(csvFileFullPath))
            {
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Read();
                    csv.ReadHeader();

                    switch (csvType)
                    {
                        case CsvType.Fleet:

                            while (csv.Read())
                            {
                                var record = new CsvEntity
                                {
                                    /*
                                    Field1 = csv.GetField("乗車 ID"),
                                    Field2 = csv.GetField("料金")
                                    */
                                    ID = csv.GetField("tripUUID"),
                                    Date = csv.GetField("timestamp")/*.Substring(0, 10)*/,
                                    Amount = csv.GetField("amount"),
                                    Type = csv.GetField("itemType")
                                };
                                result.Add(record);
                            }
                            break;

                        case CsvType.Normal:
                            //未実装
                            break;
                        case CsvType.RawDB:
                            while (csv.Read())
                            {
                                var record = new CsvEntity
                                {
                                    /*
                                    Field1 = csv.GetField("乗車 ID"),
                                    Field2 = csv.GetField("料金")
                                    */
                                    ID = csv.GetField("ID"),
                                    Date = csv.GetField("Date"),
                                    Amount = csv.GetField("Amount"),
                                    Type = csv.GetField("Type")
                                };
                                result.Add(record);
                            }
                            break;
                        default:
                            break;
                    }
                }
                return result;
            }
        }
    }

    // メインプログラム
    public static class Program
    {
        static void Main(string[] args)
        {
            //起動時メッセージ表示
            Console.WriteLine("**********************************************************************************");
            Console.WriteLine("***** Welcome to UberPlug for calculation of revenue of Uber Eats Deliveries *****");
            Console.WriteLine("**********************************************************************************");
            Console.WriteLine("Version 1.0.0");
            Console.WriteLine("");

            //データベースファイル（RawDB）保存場所を読み込み
            ReadDirPath:
            Console.WriteLine("Please enter filepath to directry where RawDB is located : ");
            WorkDir = Console.ReadLine().Replace("\"", string.Empty); ;
            if (!Directory.Exists(WorkDir))
            {
                Console.WriteLine("No such a directory.");
                goto ReadDirPath;
            }
            else
            {
                //データベースファイルへのフルパスを設定
                RawDBFileName = WorkDir + "\\" + RawDBFileName;
            }


            //起動とともにデータベース読み込み（なければ初期化）
            if (!File.Exists(RawDBFileName))
            {
                InitializeRawDB(RawDBFileName);
                Console.WriteLine("RawDB is initialized.");
            }

            //Load RawDB
            MyCsvReader myCsv = new MyCsvReader();
            RawDBEntities = myCsv.Load(RawDBFileName, CsvType.RawDB);

            //Print RawDB basic information
            Console.WriteLine("RawDB is loaded.");

            //Print Current RawDB
            PrintRawDB();

            while (true)
            {
                Console.WriteLine("Please enter command.");
                string command = Console.ReadLine();

                switch (command)
                {
                    case "print":
                        PrintRawDB();
                        break;
                    case "printdatetime":
                        PrintRawDB(" ", true);
                        break;
                    case "load":
                        Console.WriteLine("Please enter csv file name to load.");
                        string csvFileName = Console.ReadLine();
                        UpdateRawDBFromFile(WorkDir + csvFileName, true);
                        break;
                    case "allclear":
                    ClearRawDB:
                        Console.WriteLine("Are you okay to clear all entities in RawDB? (Y/N)");
                        string yesNo = Console.ReadLine();
                        if (yesNo == "Y")
                        {
                            ClearRawDB();
                            Console.WriteLine("RawDB cleared.");
                            break;
                        }
                        else if (yesNo == "N")
                        {
                            Console.WriteLine("Not cleared.");
                            break;
                        }
                        else
                        {
                            goto ClearRawDB;
                        }

                    case "export":
                        Console.WriteLine("Please enter export file name.");
                        string exportFileName = Console.ReadLine();
                        ExportShiwake(WorkDir + exportFileName, RawDBEntities);
                        Console.WriteLine("Export done.");
                        break;

                    case "exit":
                        goto WhileEnd;
                    default:
                        Console.WriteLine("Please enter valid command.");
                        Console.WriteLine("print/printdatetime/load/export/exit");
                        break;
                }

            }

        WhileEnd:
            return;

            //end of program

        }

        /// <summary>
        /// rawDBを初期化（新規ファイルにヘッダを書き出し）
        /// </summary>
        /// <param name="rawDBFileName"></param>
        /// <param name="append"></param>
        private static void InitializeRawDB(string rawDBFileName, bool append = false)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            StreamWriter sw = new StreamWriter(rawDBFileName, append, System.Text.Encoding.GetEncoding("Shift_JIS"));
            WriteRawDBHeader(sw);
            sw.Close();
            return;
        }

        /// <summary>
        /// rawDBの内容をCSV形式で書き出す（ヘッダ＋全エンティティ）
        /// </summary>
        /// <param name="rawDBFileName"></param>
        /// <param name="rawDBEntities"></param>
        /// <param name="append"></param>
        private static void WriteRawDB(string rawDBFileName, List<CsvEntity> rawDBEntities, bool append = false)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            StreamWriter sw = new StreamWriter(rawDBFileName, append, System.Text.Encoding.GetEncoding("Shift_JIS"));

            WriteRawDBHeader(sw);

            foreach (var entity in rawDBEntities)
            {
                WriteRawDBEntity(sw, entity);
            }

            sw.Close();

            return;
        }

        /// <summary>
        /// rawDBを出力するCSVファイルのヘッダを書き出す
        /// </summary>
        /// <param name="sw"></param>
        /// <param name="delim"></param>
        private static void WriteRawDBHeader(StreamWriter sw, string delim = ",")
        {
            sw.WriteLine("ID" + delim + "Date" + delim + "Amount" + delim + "Type");
        }

        /// <summary>
        /// rawDBエンティティ１件分をCSV形式で書き出す
        /// </summary>
        /// <param name="sw"></param>
        /// <param name="entity"></param>
        /// <param name="delim"></param>
        private static void WriteRawDBEntity(StreamWriter sw, CsvEntity entity, string delim = ",")
        {
            sw.WriteLine(
                entity.ID.ToString() + delim
                + entity.Date.ToString() + delim
                + entity.Amount.ToString() + delim
                + entity.Type.ToString()
                );
        }

        /// <summary>
        /// rawDBの内容を完全にクリアする
        /// </summary>
        private static void ClearRawDB()
        {
            RawDBEntities.Clear();
            WriteRawDB(RawDBFileName, RawDBEntities);
        }

        /// <summary>
        /// rawDBの内容をコンソールに出力
        /// </summary>
        /// <param name="delim"></param>
        private static void PrintRawDB(string delim = " ", bool useDateTime = false)
        {
            Console.WriteLine("RawDB basic information : ");

            foreach (var typeSumList in RawDBEntities.GroupBy(_ent => _ent.Type))
            {
                Console.WriteLine(typeSumList.Key.ToString() + " : " + typeSumList.Count());
            }
            Console.WriteLine("Total : " + RawDBEntities.Count.ToString());

            Console.WriteLine("Now RawDB has entities below : ");

            foreach (var entity in RawDBEntities)
            {
                string dTime =
                    useDateTime ? DateTime.Parse(entity.Date).ToString()
                    : entity.Date;

                Console.WriteLine(
                    entity.ID + delim
                    + dTime + delim
                    + entity.Amount + delim
                    + entity.Type

                    );
            }

        }

        /// <summary>
        /// RawDBのソート
        /// </summary>
        private static void SortRawDB()
        {
            RawDBEntities.Sort((a, b) => string.Compare(a.Date, b.Date));
            //
            //WriteRawDB(RawDBFileName, RawDBEntities);
        }

        /// <summary>
        /// CSVファイルを読み込み未登録のエンティティがあればrawDBに追加
        /// </summary>
        /// <param name="csvFileName"></param>
        /// <param name="print"></param>
        private static void UpdateRawDBFromFile(string csvFileName, bool print = false)
        {
            //
            if (!File.Exists(csvFileName))
            {
                Console.WriteLine("No such a file.");
                return;
            }

            //
            MyCsvReader myCsv = new MyCsvReader();

            //File error check

            //Load csv file
            List<CsvEntity> csvEntities = myCsv.Load(csvFileName, CsvType.Fleet);

            //Update RawDB
            foreach (var entity in csvEntities)
            {
                bool alreadyExists = false;
                foreach (var rawDBEntity in RawDBEntities)
                {
                    if (
                        entity.Date.Equals(rawDBEntity.Date)
                        && entity.Type.Equals(rawDBEntity.Type)
                        )
                    {
                        alreadyExists = true;
                        break;
                    }
                }
                if (!alreadyExists)
                {
                    RawDBEntities.Add(entity);
                }
            }

            //
            SortRawDB();

            //
            WriteRawDB(RawDBFileName, RawDBEntities);

            //
            if (print)
            {
                PrintRawDB();
            }

        }

        /// <summary>
        /// rawDB Entities
        /// </summary>
        private static List<CsvEntity> RawDBEntities = new List<CsvEntity>();


        /// <summary>
        /// Fullpath to WorkDir
        /// </summary>
        private static string WorkDir = "";

        /// <summary>
        /// Fullpath to rawDB
        /// </summary>
        private static string RawDBFileName = "RawDB.csv";

        /// <summary>
        /// 日次データ取得
        /// </summary>
        /// <param name="csvEntities"></param>
        /// <returns></returns>
        private static SortedList<string, SortedList<string, string>>
            GetDailyList(List<CsvEntity> csvEntities)
        {
            //
            SortedList<string, SortedList<string, string>> retList
                = new SortedList<string, SortedList<string, string>>();

            //
            List<CsvEntity> dailyEntities = new List<CsvEntity>(csvEntities);

            //
            foreach (var dailyEntity in dailyEntities)
            {
                dailyEntity.Date = dailyEntity.Date.Substring(0, 10);
            }

            //
            foreach (var dateSum in dailyEntities.GroupBy(x => x.Date))
            {
                //
                retList.Add(dateSum.Key, new SortedList<string, string>());

                //
                foreach (var typeSum in dateSum.GroupBy(x => x.Type))
                {
                    retList[dateSum.Key].Add(
                        typeSum.Key,
                        typeSum.Sum(x => Convert.ToDouble(x.Amount)).ToString()
                        );
                }

            }

            //
            return retList;

        }

        /// <summary>
        /// 仕訳データをエクスポート
        /// （弥生形式、CSV）
        /// </summary>
        private static void ExportShiwake(string exportFileName, List<CsvEntity> csvEntities, bool append = false)
        {
            /*
            if (!File.Exists(exportFileName))
            {
                Console.WriteLine("No such a file.");
                return;
            }
            */

            //
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            StreamWriter sw = new StreamWriter(exportFileName, append, System.Text.Encoding.GetEncoding("Shift_JIS"));

            //
            SortedList<string, SortedList<string, string>> dailyList
                = GetDailyList(csvEntities);　//日付、タイプ、金額

            foreach (var dateSum in dailyList)
            {
                foreach (var typeSum in dateSum.Value)
                {
                    //
                    WriteShiwakeEntity(sw, dateSum.Key, typeSum);

                }
            }

            //
            sw.Close();

            return;

        }

        /// <summary>
        /// 仕訳１件分の書き出し（弥生形式）
        /// </summary>
        /// <param name="sw"></param>
        /// <param name="date"></param>
        /// <param name="typeAmount"></param>
        /// <param name="delim"></param>
        public static void WriteShiwakeEntity(StreamWriter sw,
            string date, KeyValuePair<string, string> typeAmount,
            string delim = ",")
        {

            //
            Tuple<KanjoKamoku, double, KanjoKamoku, double> shiwakeEntity
                = typeAmount.ToShiwake();

            //
            if (shiwakeEntity.Item1.Equals(KanjoKamoku.NONE)
                || shiwakeEntity.Item3.Equals(KanjoKamoku.NONE))
            {
                return;
            }

            //例：2000,,,2019/08/14,売掛金,,,対象外,11754,,売上,,,,11754,,,,,0,,,,,no

            //【必須】識別フラグ
            sw.Write("2000");
            sw.Write(delim);
            //伝票No.(伝票時のみ）
            sw.Write(delim);
            //決算（中決 or 本決）
            sw.Write(delim);
            //【必須】取引日付
            sw.Write(DateTime.Parse(date).ToString("yyyy/MM/dd"));
            sw.Write(delim);
            //【必須】借方勘定科目
            sw.Write(shiwakeEntity.Item1.ToExport());
            sw.Write(delim);
            //借方補助科目
            sw.Write(delim);
            //借方部門
            sw.Write(delim);
            //【必須】借方税区分
            sw.Write("対象外");
            sw.Write(delim);
            //【必須】借方金額
            sw.Write(Convert.ToInt32(shiwakeEntity.Item2).ToString());
            sw.Write(delim);
            //借方税金額
            sw.Write(delim);
            //【必須】貸方勘定科目
            sw.Write(shiwakeEntity.Item3.ToExport());
            sw.Write(delim);
            //貸方補助科目
            sw.Write(delim);
            //貸方部門
            sw.Write(delim);
            //【必須】貸方税区分
            sw.Write("対象外");
            sw.Write(delim);
            //【必須】貸方金額
            sw.Write(Convert.ToInt32(shiwakeEntity.Item4).ToString());
            sw.Write(delim);
            //貸方税金額
            sw.Write(delim);
            //摘要
            sw.Write(delim);
            //番号(［受取手形］［支払手形］の手形番号を記述)
            sw.Write(delim);
            //期日
            sw.Write(delim);
            //【必須】タイプ（0：仕訳データ　1：出金伝票データ　2：入金伝票データ　3：振替伝票データ）
            sw.Write("0");
            sw.Write(delim);
            //生成元
            sw.Write(delim);
            //仕訳メモ
            sw.Write(delim);
            //付箋１（数字）
            sw.Write(delim);
            //付箋２（数字）
            sw.Write(delim);
            //【必須】調整
            sw.Write("no");
            sw.Write(Environment.NewLine);

        }

        /// <summary>
        /// 勘定科目（エクスポート用）
        /// </summary>
        public static Dictionary<KanjoKamoku, string> KanjoKamokuForExport
        = new Dictionary<KanjoKamoku, string>
        {
            {KanjoKamoku.NONE, "未設定"},
            {KanjoKamoku.URIAGE, "売上"},
            {KanjoKamoku.URIKAKEKIN, "売掛金"},
            {KanjoKamoku.MIBARAIKIN, "未払金"},
            {KanjoKamoku.GENKIN, "現金"},
            {KanjoKamoku.YOKIN, "預金"},
        };

        /// <summary>
        /// 勘定科目（列挙体からエクスポート用を取得）
        /// </summary>
        /// <param name="kanjoKamoku"></param>
        /// <returns></returns>
        public static string ToExport(this KanjoKamoku kanjoKamoku)
            => KanjoKamokuForExport[kanjoKamoku];

        /// <summary>
        /// CsvEntityから仕訳への変換
        /// </summary>
        /// <param name="csvEntity"></param>
        /// <returns></returns>
        public static Tuple<KanjoKamoku, double, KanjoKamoku, double>
            ToShiwake(this CsvEntity csvEntity)
        {
            //
            double amount = Math.Abs(Convert.ToDouble(csvEntity.Amount));

            //
            switch (csvEntity.Type)
            {
                case "payouts":
                    return new Tuple<KanjoKamoku, double, KanjoKamoku, double>
                        (KanjoKamoku.NONE, amount, KanjoKamoku.NONE, amount);
                case "cash_collected":
                    //TODO:ここに売掛金の残高チェックを入れてマイナスなら預り金にする処理を入れる
                    return new Tuple<KanjoKamoku, double, KanjoKamoku, double>
                        (KanjoKamoku.GENKIN, amount, KanjoKamoku.URIKAKEKIN, amount);
                case "promotion":
                case "trip":
                    return new Tuple<KanjoKamoku, double, KanjoKamoku, double>
                        (KanjoKamoku.URIKAKEKIN, amount, KanjoKamoku.URIAGE, amount);
                case "uber_fee_collection":
                    return new Tuple<KanjoKamoku, double, KanjoKamoku, double>
                        (KanjoKamoku.URIKAKEKIN, amount, KanjoKamoku.MIBARAIKIN, amount);
                default:
                    return new Tuple<KanjoKamoku, double, KanjoKamoku, double>
                        (KanjoKamoku.NONE, 0.0, KanjoKamoku.NONE, 0.0);
            }

        }

        /// <summary>
        /// KeyValuePair(タイプ、金額)から仕訳への変換
        /// </summary>
        /// <param name="typeAmount"></param>
        /// <returns></returns>
        public static Tuple<KanjoKamoku, double, KanjoKamoku, double>
            ToShiwake(this KeyValuePair<string, string> typeAmount)
        {
            //
            double amount = Math.Abs(Convert.ToDouble(typeAmount.Value));

            //
            switch (typeAmount.Key)
            {
                case "payouts":
                    return new Tuple<KanjoKamoku, double, KanjoKamoku, double>
                        (KanjoKamoku.NONE, amount, KanjoKamoku.NONE, amount);
                case "cash_collected":
                    //TODO:ここに売掛金の残高チェックを入れてマイナスなら預り金にする処理を入れる
                    return new Tuple<KanjoKamoku, double, KanjoKamoku, double>
                        (KanjoKamoku.GENKIN, amount, KanjoKamoku.URIKAKEKIN, amount);
                case "promotion":
                case "trip":
                    return new Tuple<KanjoKamoku, double, KanjoKamoku, double>
                        (KanjoKamoku.URIKAKEKIN, amount, KanjoKamoku.URIAGE, amount);
                case "uber_fee_collection":
                    return new Tuple<KanjoKamoku, double, KanjoKamoku, double>
                        (KanjoKamoku.URIKAKEKIN, amount, KanjoKamoku.MIBARAIKIN, amount);
                default:
                    return new Tuple<KanjoKamoku, double, KanjoKamoku, double>
                        (KanjoKamoku.NONE, 0.0, KanjoKamoku.NONE, 0.0);
            }

        }

    }

}
