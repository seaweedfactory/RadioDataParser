using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioDataParser
{
    /// <summary>
    /// This program parses data downloaded from the FCC's Universal Licensing System advanced search and exports the data to CSV. 
    /// The search on which this is based is available at https://wireless2.fcc.gov/UlsApp/UlsSearch/searchAdvanced.jsp as of 11/10/2019.
    /// These files are obtained by email from the FCC using the search.
    /// </summary>
    /// <remarks>
    /// The progam takes two arguments: RadioDataParse "path_to_file" "base_csv_filename"
    /// 
    /// "path_to_file" is the path to the data file downloaded from the FCC.
    /// 
    /// "base_csv_filename" is the path and prefix of the output csv files. Two files are written, a summary and a history CSV.
    /// </remarks>
    class Program
    {
        static void Main(string[] args)
        {
            if(args == null || args.Length < 2)
            {
                return;
            }

            LicenseDataCollection data = new LicenseDataCollection();

            //Parse data
            data.ReadFile(args[0]);

            //Write CSV files
            data.WriteOperatorSummaryCSV(String.Format("{0}-summary.csv",args[1]));
            data.WriteOperatorHistoryCSV(String.Format("{0}-history.csv", args[1]));
        }
    }
}
