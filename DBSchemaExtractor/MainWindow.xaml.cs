using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SqlClient;
using System.Data;
using ExcelLibrary.SpreadSheet;
using System.Text.RegularExpressions;

namespace DBSchemaExtractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SqlConnection _con;
        private string _filter;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            var sqlConnectionStringBuilder = new SqlConnectionStringBuilder();
            sqlConnectionStringBuilder.UserID = tbLoginName.Text;
            sqlConnectionStringBuilder.Password = tbPassword.Password;
            sqlConnectionStringBuilder.DataSource = tbServerName.Text;
            sqlConnectionStringBuilder.InitialCatalog = tbDatabaseName.Text;
            _con = new SqlConnection(sqlConnectionStringBuilder.ConnectionString);
            _filter = String.Empty;
            PopulateBrowser();

        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Workbook workbook = new Workbook();
            Worksheet worksheetCoreTables = new Worksheet("Core Tables");
            Worksheet worksheetDictionaries = new Worksheet("Dictionaries");
            int x = 0, y = 0;
            worksheetCoreTables.Cells[y, x] = new Cell("Table Design for: " + _con.Database.ToString());
            //worksheetCoreTables.Cells[y,x].Format.FormatType

        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            _filter = tbFilter.Text;
            PopulateBrowser();
        }

        private void PopulateBrowser()
        {
            try
            {
                _con.Open();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Sorry mate. DB is not available - at least for you ;)" + Environment.NewLine + ex.Message);
                return;
            }
            //System.Data.DataTable tbl = con.GetSchema("Databases");
            //ExportExcel(tbl);

            var sb = new StringBuilder();
            sb.Append(" <TABLE BORDER=1 cellspacing=0 cellpadding=0>");
            sb.Append("<tr><td colspan='6' align='center' style='background-color:#CC99FF'>");
            sb.Append("<b>Table Design for: " + _con.Database.ToString() + "</b>");

            sb.Append("</td></tr>");

            sb.Append("<tr><td colspan='6' align='center'> </td></tr>");
            //con.Close();
            DataTable tblTables = null;

            tblTables = _con.GetSchema(SqlClientMetaDataCollectionNames.Tables, new string[] { null, null, null, "BASE TABLE" });
            DataTable columnsTable = null;
            foreach (DataRow rowDatabase in tblTables.Rows)
            {
                columnsTable = _con.GetSchema(SqlClientMetaDataCollectionNames.Columns, new string[] { null, null, rowDatabase["TABLE_NAME"].ToString() });
                if (!String.IsNullOrEmpty(_filter) && !Regex.IsMatch(rowDatabase["TABLE_NAME"].ToString(),_filter,RegexOptions.IgnoreCase))
                {
                    continue;
                }
                sb.Append("<tr style='background-color:#CC99FF'><td colspan='5' align='center'>");
                sb.Append("<b>Table Description</b></td>");
                sb.Append("<td align='center'>");
                sb.Append("<b>Table Name</b></td></tr>");
                sb.Append("<tr  style='background-color:#CC99FF'><td colspan='5' align='center' >" +
                          rowDatabase["TABLE_NAME"].ToString() + "</td><td colspan='1' align='center'>" +
                          rowDatabase["TABLE_NAME"].ToString() + "</td></tr>");
                sb.Append(
                    "<tr><td><b>No</b></td> <td><b> Field Name</b></td> <td><b> Data Type</b></td> <td><b> Size</b></td> <td><b> Constraint </b></td><td><b> Explanation </b></td></tr>");
                int i = 1;
                DataTable restrictions = _con.GetSchema(SqlClientMetaDataCollectionNames.IndexColumns, new string[] { null, null, rowDatabase["TABLE_NAME"].ToString() });
                for (int k = 0; k < columnsTable.Rows.Count; k++)
                {
                    string constraintString = string.Empty;
                    var constraint = (from q in restrictions.AsEnumerable()
                                      where
                                          q.Field<string>("column_name") ==
                                          columnsTable.Rows[k]["COLUMN_NAME"].ToString()
                                      select q);
                    if (constraint.Any())
                    {
                        foreach (var c in constraint)
                        {
                            if (Regex.IsMatch(c.Field<string>("constraint_name"), "PK|Primary", RegexOptions.IgnoreCase))
                            {
                                constraintString += "PK,";
                            }
                            else if (Regex.IsMatch(c.Field<string>("constraint_name"), "FK|Foreign", RegexOptions.IgnoreCase))
                            {
                                constraintString += "FK,";
                            }
                            else if (Regex.IsMatch(c.Field<string>("constraint_name"), "UQ|Unique", RegexOptions.IgnoreCase))
                            {
                                constraintString += "UQ,";
                            }
                            else if (Regex.IsMatch(c.Field<string>("constraint_name"), "IDX|Index", RegexOptions.IgnoreCase))
                            {
                                constraintString += "IDX,";
                            }
                        }
                    }
                    sb.Append("<tr><td>" + i + "</td> <td>" + columnsTable.Rows[k]["COLUMN_NAME"].ToString() + "</td> <td>" +
                              columnsTable.Rows[k]["DATA_TYPE"].ToString() + "</td> <td>" +
                              columnsTable.Rows[k]["CHARACTER_MAXIMUM_LENGTH"].ToString() + "</td> <td>" +
                              constraintString + "</td><td></td></tr>");
                    i++;
                }

                sb.Append("<tr><td colspan=6></td></tr>");

            }

            _con.Close();
            sb.Append(" </TABLE>");

            webBrowser.NavigateToString(sb.ToString());
        }
    }
}

