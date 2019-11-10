using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioDataParser
{
    /// <summary>
    /// Extracts license data from FCC Universal License System advanced search downloaded data files.
    /// Can also write this data to a CSV for further processing.
    /// </summary>
    public class LicenseDataCollection
    {
        /// <summary>
        /// List of operators parsed from the data.
        /// </summary>
        public List<OperatorRecord> Operators { get; set; } = new List<OperatorRecord>();

        #region Token records
        private Dictionary<String, TokenCollection> HD { get; set; } = new Dictionary<String, TokenCollection>();
        private Dictionary<String, TokenCollection> EN { get; set; } = new Dictionary<String, TokenCollection>();
        private Dictionary<String, List<HistoryRecord>> HS { get; set; } = new Dictionary<String, List<HistoryRecord>>();
        private Dictionary<String, TokenCollection> AM { get; set; } = new Dictionary<String, TokenCollection>();
        private Dictionary<String, TokenCollection> SC { get; set; } = new Dictionary<String, TokenCollection>();
        private Dictionary<String, TokenCollection> CO { get; set; } = new Dictionary<String, TokenCollection>();
        private Dictionary<String, TokenCollection> LM { get; set; } = new Dictionary<String, TokenCollection>();
        #endregion

        /// <summary>
        /// Read a data file downloaded from the FCC Univeral License System advanced search and fill the collection with its data.
        /// </summary>
        /// <param name="filename">Path to downloaded file.</param>
        public void ReadFile(String filename)
        {
            var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (!String.IsNullOrEmpty(line))
                    {
                        string[] tokens = line.Split(new char[] { '|' }, StringSplitOptions.None);
                        if (tokens != null && tokens.Length > 4)
                        {
                            string typeToken = tokens[0];
                            string callSign = "";
                            if (!String.IsNullOrEmpty(typeToken))
                            {
                                if (typeToken.Equals("HD"))
                                {
                                    callSign = tokens[4];
                                    if (!HD.ContainsKey(callSign))
                                    {
                                        HD.Add(callSign, new TokenCollection(tokens));
                                    }
                                }
                                else if (typeToken.Equals("EN"))
                                {
                                    callSign = tokens[4];
                                    if (!EN.ContainsKey(callSign))
                                    {
                                        EN.Add(callSign, new TokenCollection(tokens));
                                    }
                                }
                                else if (typeToken.Equals("HS"))
                                {
                                    callSign = tokens[3];
                                    Tuple<HistoryRecordAction, String> actionType = GetHistoryActionType(tokens[5]);
                                    HistoryRecord rec = new HistoryRecord()
                                    {
                                        CallSign = callSign,
                                        LicenseKey = tokens[1],
                                        Date = DateTime.ParseExact(tokens[4], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                                        Action = actionType.Item1,
                                        ActionDescription = actionType.Item2
                                    };
                                    if (!HS.ContainsKey(callSign))
                                    {
                                        List<HistoryRecord> recList = new List<HistoryRecord>();
                                        recList.Add(rec);
                                        HS.Add(callSign, recList);
                                    }
                                    else
                                    {
                                        HS[callSign].Add(rec);
                                    }
                                }
                                else if (typeToken.Equals("AM"))
                                {
                                    callSign = tokens[4];
                                    if (!AM.ContainsKey(callSign))
                                    {
                                        AM.Add(callSign, new TokenCollection(tokens));
                                    }
                                }
                                else if (typeToken.Equals("SC"))
                                {
                                    callSign = tokens[4];
                                    if (!SC.ContainsKey(callSign))
                                    {
                                        SC.Add(callSign, new TokenCollection(tokens));
                                    }
                                }
                                else if (typeToken.Equals("CO"))
                                {
                                    callSign = tokens[4];
                                    if (!CO.ContainsKey(callSign))
                                    {
                                        CO.Add(callSign, new TokenCollection(tokens));
                                    }
                                }
                                else if (typeToken.Equals("LM"))
                                {
                                    callSign = tokens[4];
                                    if (!LM.ContainsKey(callSign))
                                    {
                                        LM.Add(callSign, new TokenCollection(tokens));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            ExtractOperatorRecords();
        }

        /// <summary>
        /// After data has been read, extract operator records from the collected data.
        /// </summary>
        private void ExtractOperatorRecords()
        {
            foreach(String key in HD.Keys)
            {
                OperatorRecord rec = new OperatorRecord();
                List<String> hd = HD[key].Fields;

                Tuple<RadioService, String> serviceType = GetRadioServiceType(hd[6]);

                rec.CallSign = key;
                rec.RadioService = serviceType.Item1;
                rec.RadioServiceDescription = serviceType.Item2;
                rec.LicenseKey = hd[1];
                rec.Frequency = hd[2];
                rec.Granted = ParseNullableDate(hd[7]);
                rec.Expiration = ParseNullableDate(hd[8]);
                rec.Cancelled = ParseNullableDate(hd[9]);
                rec.Effective = ParseNullableDate(hd[42]);

                if (EN.ContainsKey(key))
                {
                    List<String> en = EN[key].Fields;
                    rec.Organization = Util.ToTitleCase(en[7]);
                    rec.FirstName = Util.ToTitleCase(en[8]);
                    rec.MiddleInitial = Util.ToTitleCase(en[9]);
                    rec.LastName = Util.ToTitleCase(en[10]);
                    rec.Suffix = Util.ToTitleCase(en[11]);
                    rec.Address = Util.ToTitleCase(en[15])?.Replace("Th ", "th ");
                }

                if (AM.ContainsKey(key))
                {
                    List<String> am = AM[key].Fields;
                    Tuple<OperatorClass, String> ocType = GetOperatorClassType(am[5]);
                    rec.OperatorClass = ocType.Item1;
                    rec.OperatorClassDescription = ocType.Item2;
                    rec.Group = am[6];
                }

                if (LM.ContainsKey(key))
                {
                    List<String> lm = LM[key].Fields;
                    rec.LicenseEligibility = lm[6];
                }

                if (HS.ContainsKey(key))
                {
                    rec.History.AddRange(HS[key].OrderBy(x => x.Date).ToList());
                }
                                
                Operators.Add(rec);
            }
        }

        /// <summary>
        /// Write summary data for each operator to a CSV.
        /// </summary>
        /// <param name="filename">Output CSV file path.</param>
        public void WriteOperatorSummaryCSV(String filename)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename))
            {
                file.WriteLine(String.Join(",",
                        "Call Sign",
                        "Frequency",
                        "Service",
                        "Organization",
                        "Contact",
                        "Address",
                        "Granted",
                        "Effective",
                        "Expires",
                        "Cancelled",
                        "Notes"
                        ));

                foreach (OperatorRecord rec in Operators.OrderBy(x => x.CallSign).ToList())
                {
                    List<String> notes = new List<string>();
                    if(!string.IsNullOrWhiteSpace(rec.LicenseEligibility))
                    {
                        notes.Add(rec.LicenseEligibility);
                    }
                    if (!string.IsNullOrWhiteSpace(rec.OperatorClassDescription))
                    {
                        notes.Add(rec.OperatorClassDescription);
                    }
                    if (!string.IsNullOrWhiteSpace(rec.Group))
                    {
                        notes.Add(String.Format("Group {0}",rec.Group));
                    }

                    file.WriteLine(String.Join(",", 
                        rec.CallSign, 
                        rec.Frequency,
                        rec.RadioServiceDescription,
                        String.Format("\"{0}\"",rec.Organization),
                        rec.FullName?.Replace(",", " "),
                        rec.Address?.Replace(",", " "),
                        rec.Granted?.ToString("MM/dd/yyyy"),
                        rec.Effective?.ToString("MM/dd/yyyy"),
                        rec.Expiration?.ToString("MM/dd/yyyy"),
                        rec.Cancelled?.ToString("MM/dd/yyyy"),
                        String.Format("\"{0}\"",notes.Count > 0 ? String.Join(", ", notes) : null)
                        ));
                }
            }
        }

        /// <summary>
        /// Write detailed license history for each operator to a CSV.
        /// </summary>
        /// <param name="filename">Output CSV file path.</param>
        public void WriteOperatorHistoryCSV(String filename)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename))
            {
                List<HistorySummaryRecord> history = new List<HistorySummaryRecord>();
                foreach (OperatorRecord opRec in Operators)
                {
                    foreach(HistoryRecord histRec in opRec.History)
                    {
                        history.Add(new HistorySummaryRecord()
                        {
                            CallSign = histRec.CallSign,
                            Organization = opRec.Organization,
                            FullName = opRec.FullName,
                            Date = histRec.Date,
                            ActionDescription = histRec.ActionDescription
                        });
                    }
                }

                file.WriteLine(String.Join(",",
                    "Date",
                    "Call Sign",
                    "Organization",
                    "Contact",
                    "Action"
                    ));

                foreach (HistorySummaryRecord rec in history.OrderBy(x => x.Date).ToList())
                {
                    file.WriteLine(String.Join(",",
                        rec.Date.ToString("MM/dd/yyyy"),
                        rec.CallSign,
                        String.Format("\"{0}\"", rec.Organization),
                        rec.FullName,
                        rec.ActionDescription
                        ));
                }
            }
        }

        /// <summary>
        /// Extract human readable history action type information from the action type code.
        /// </summary>
        /// <param name="action">Action code to parse.</param>
        /// <returns>Tuple of code and human readable action type.</returns>
        private Tuple<HistoryRecordAction, String> GetHistoryActionType(String action)
        {
            HistoryRecordAction actionEnum = HistoryRecordAction.UNKNOWN;
            if (!String.IsNullOrWhiteSpace(action) && Enum.TryParse<HistoryRecordAction>(action, out actionEnum)) 
            {
                return Tuple.Create<HistoryRecordAction, String>(actionEnum, Util.GetEnumDescription(actionEnum));
            }

            return Tuple.Create<HistoryRecordAction, String>(HistoryRecordAction.UNKNOWN, action);
        }

        /// <summary>
        /// Extract human readable operator class description.
        /// </summary>
        /// <param name="op">Operator class to parse.</param>
        /// <returns>Tuple of operator class and human readble description.</returns>
        private Tuple<OperatorClass, String> GetOperatorClassType(String op)
        {
            OperatorClass ocEnum = OperatorClass.Unknown;
            if (!String.IsNullOrWhiteSpace(op) && Enum.TryParse<OperatorClass>(op, out ocEnum))
            {
                return Tuple.Create<OperatorClass, String>(ocEnum, Util.GetEnumDescription(ocEnum));
            }

            return Tuple.Create<OperatorClass, String>(OperatorClass.Unknown, op);
        }

        /// <summary>
        /// Extract human service class description.
        /// </summary>
        /// <param name="op">Service class to parse.</param>
        /// <returns>Tuple of service class and human readble description.</returns>
        private Tuple<RadioService, String> GetRadioServiceType(String service)
        {
            RadioService serviceEnum = RadioService.Unknown;
            if (!String.IsNullOrWhiteSpace(service) && Enum.TryParse<RadioService>(service, out serviceEnum))
            {
                return Tuple.Create<RadioService, String>(serviceEnum, Util.GetEnumDescription(serviceEnum));
            }

            return Tuple.Create<RadioService, String>(RadioService.Unknown, service);
        }

        /// <summary>
        /// Parse possible nullable dates stroed in Strings.
        /// </summary>
        /// <param name="s">Date string to parse.</param>
        /// <returns>DateTime if successful, or null if not.</returns>
        private DateTime? ParseNullableDate(String s)
        {
            DateTime tempDate = new DateTime();
            if(!String.IsNullOrEmpty(s) && DateTime.TryParseExact(s, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out tempDate))
            {
                return tempDate;
            }
            else
            {
                return null;
            }
        }
        
    }

    /// <summary>
    /// Holds token data extracted from the downloaded file.
    /// </summary>
    public class TokenCollection
    {
        /// <summary>
        /// List of tokens.
        /// </summary>
        public List<String> Fields { get; set; } = new List<string>();

        /// <summary>
        /// Holds token data extracted from the downloaded file.
        /// </summary>
        /// <param name="tokens">Extracted tokens.</param>
        public TokenCollection(string[] tokens)
        {
            foreach(string token in tokens)
            {
                Fields.Add(token);
            }
        }
    }

    /// <summary>
    /// Stores a license history item.
    /// </summary>
    public class HistoryRecord
    {
        public string CallSign { get; set; }
        public string LicenseKey { get; set; }
        public DateTime Date { get; set; }
        public HistoryRecordAction Action { get; set; }
        public String ActionDescription { get; set; }
    }

    /// <summary>
    /// Stores an operator summary item.
    /// </summary>
    public class HistorySummaryRecord
    {
        public string CallSign { get; set; }
        public string Organization { get; set; }
        public string FullName { get; set; }
        public DateTime Date { get; set; }
        public String ActionDescription { get; set; }
    }

    /// <summary>
    /// Enum for converting license history actions to human readable descriptions.
    /// </summary>
    public enum HistoryRecordAction
    {
        [Description("Unknown")]
        UNKNOWN = 0,

        [Description("License Renewed")]
        LIREN = 1,

        [Description("Administrative Update Applied")]
        LIAUA = 2,

        [Description("License Status Set to Expired")]
        LIEXP = 3,
        
        [Description("FRN Association email sent: CORES email")]
        ESCFRN = 4,

        [Description("License TIN Added")]
        LITIN = 5,

        [Description("Internal Correction Applied")]
        COR = 6,

        [Description("Authorization Printed")]
        AUTHPR = 7,

        [Description("License Canceled")]
        LICAN = 8,

        [Description("FRN Association Letter sent")]
        LTSFRN = 9,

        [Description("License Issued")]
        LIISS = 10,

        [Description("Vanity Call Sign Assigned")]
        VANGRT = 11,

        [Description("New Systematic Call Sign Assigned")]
        SYSGRT = 12,

        [Description("License Modified")]
        LIMOD = 13,

        [Description("Reference Copy Duplicate Requested")]
        RCDUP = 14,

        [Description("Renewal Reminder Letter Sent")]
        LETRES = 15,

        [Description("License Converted")]
        LICCNV = 16,

        [Description("Audit Response - License Operational")]
        AUDOPR = 17,

        [Description("Application receipt email sent: CORES email")]
        ESCAPR = 18,

        [Description("Application receipt email failed: CORES email")]
        EFCAPR = 19,

        [Description("Application Receipt Letter sent")]
        LTSAPR = 20,

        [Description("License Audit Letter Sent")]
        LETAUS = 21,

        [Description("Construction/Coverage Reminder Letter Sent")]
        LETCNS = 22,

        [Description("Public Safety Renewal email sent: ULS email")]
        ESURNW = 23,

        [Description("Paperless Renewal Reminder Letter")]
        PLRRPR = 24,

        [Description("License Auto Termination Letter Sent")]
        LETTRS = 25,

        [Description("Site Based Auto Term PN Generated")]
        PNLISR = 26,

        [Description("License Assigned (Full Assignment)")]
        LIASS = 27,
    }

    /// <summary>
    /// Information about an operator and their license.
    /// </summary>
    public class OperatorRecord
    {
        public String CallSign { get; set; }
        public String FirstName { get; set; }
        public String MiddleInitial { get; set; }
        public String LastName { get; set; }
        public String Suffix { get; set; }
        public String Organization { get; set; }
        public String LicenseKey { get; set; }
        public String LicenseEligibility { get; set; }
        public String Frequency { get; set; }
        public RadioService RadioService { get; set; }
        public String RadioServiceDescription { get; set; }
        public DateTime? Granted { get; set; }
        public DateTime? Effective { get; set; }
        public DateTime? Expiration { get; set; }
        public DateTime? Cancelled { get; set; }
        public String Address { get; set; }
        public OperatorClass OperatorClass { get; set; }
        public String OperatorClassDescription { get; set; }
        public String Group { get; set; }
        public String FullName 
        {
            get
            {
                return String.Format("{0} {1} {2} {3}", FirstName, MiddleInitial, LastName, Suffix).Trim().Replace("  ", " ").Replace("   ", " ");
            }
        }

        public List<HistoryRecord> History { get; set; } = new List<HistoryRecord>();
    }

    /// <summary>
    /// Enum to convert operator class to a human readable description.
    /// </summary>
    public enum OperatorClass
    {
        [Description("Unknown")]
        Unknown = 0,

        [Description("Technician")]
        T = 1,

        [Description("Amateur Extra")]
        E = 2,

        [Description("General")]
        G = 3,

        [Description("Novice")]
        N = 4,

        [Description("Advanced")]
        A = 5,

        [Description("Technician Plus")]
        P = 6
    }

    /// <summary>
    /// Enum to convert service class to a human readable description.
    /// </summary>
    public enum RadioService
    {
        [Description("Unknown")]
        Unknown = 0,

        [Description("Commercial")]
        CM = 1,

        [Description("Amateur")]
        HA = 2,

        [Description("Vanity")]
        HV = 3,

        [Description("Restricted")]
        RR = 4,

        [Description("GMRS")]
        ZA = 5,

        [Description("Public Safety")]
        PW = 6,

        [Description("Industrial/Business")]
        IG = 7,

        [Description("Ship (Voluntarily)")]
        SA = 8,

        [Description("Ship (Compulsory)")]
        SB = 9,

        [Description("Microwave Public Safety")]
        MW = 10,
    }
}
