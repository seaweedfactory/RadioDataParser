# RadioDataParser
This program parses data downloaded from the FCC's Universal Licensing System advanced search and exports the data to CSV. 
The search on which this is based is available at https://wireless2.fcc.gov/UlsApp/UlsSearch/searchAdvanced.jsp as of 11/10/2019.
These files are obtained by email from the FCC using the search.

## Arguments
The program takes two arguments: 

RadioDataParser **path_to_file** **base_csv_filename**

**path_to_file** is the path to the data file downloaded from the FCC.

**base_csv_filename** is the path and prefix of the output csv files. Two files are written, a summary and a history CSV.
