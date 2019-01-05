﻿using System;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.IO;
using ICSharpCode.TextEditor.Document;

namespace ASEABugTracker
{
    public partial class Main : Form
    {
        /// <summary>
        /// <see cref="SqlConnection"/> used for the <see cref="ASEABugTracker.Main"/> form.
        /// </summary>
        SqlConnection mainConnection;

        /// <summary>
        /// Used to indentify which programming language is used for current bug (<see cref="txtBugId"/>).
        /// </summary>
        String language;

        /// <summary>
        ///  Entry point for the <see cref="ASEABugTracker.Main"/> form.
        /// </summary>
        public Main()
        {
            InitializeComponent();
            Session();   
        }

        /// <summary>
        ///  Retrieves session data (<see cref="Login.sessionUsername"/>, <see cref="OpenBug.sessionOpenBug"/>) and initiates form function (<see cref="Populate"/>),
        ///  if a bug submission is loaded (when <see cref="txtBugId"/> not empty).
        /// </summary>
        public void Session()
        {
            txtUsername.Text = Login.sessionUsername;       //Username text field assigned value of current sessions 'Username' from Login form.
            txtBugId.Text = OpenBug.sessionOpenBug;         //Bug ID text field assigned value of current sessions 'BugID' from OpenBug form.
            buttonFix.Enabled = false;                      //Change 'Fixed' status of bug button set to disabled by default, enabled later under conditions.
            submitAuditToolStripMenuItem.Enabled = false;   //'Submit' menu item set to disabled by default, enabled later under conditions.
            if (txtBugId.Text != "")
            {
                Populate();
            }
        }

        /// <summary>
        ///  Connects to the database (<see cref="mainConnection"/>) and selects, then reads SQL data (<see cref="SqlDataReader"/>) that populates the fields in the <see cref="Main"/> form.
        /// </summary>
        public void Populate()
        {
            mainConnection = new SqlConnection
                (@"Data Source = (LocalDB)\MSSQLLocalDB; AttachDbFilename = 
                |DataDirectory|\ASEABugTrackDB.mdf;");
            String selBugCommand = "SELECT Username, Application, Symptom, Cause, Class, Method, CodeBlock, LineNoStart, LineNoEnd, Language, Fixed FROM BugTable WHERE BugId = " + txtBugId.Text;  //Only selects row of data for the currently loaded bug session.
            SqlCommand sqlBugCommand = new SqlCommand(selBugCommand, mainConnection);
            
            try
            {
                mainConnection.Open();
                SqlDataReader bugSqlDataReader = sqlBugCommand.ExecuteReader();
                while (bugSqlDataReader.Read())
                {
                    if (bugSqlDataReader.GetString(0) == Login.sessionUsername) { buttonFix.Enabled = true; }
                    else if (bugSqlDataReader.GetString(0) != Login.sessionUsername) { buttonFix.Enabled = false; }
                    txtApplication.Text = bugSqlDataReader.GetString(1);
                    txtSymptom.Text = bugSqlDataReader.GetString(2);
                    txtCause.Text = bugSqlDataReader.GetString(3);
                    txtClass.Text = bugSqlDataReader.GetString(4);
                    txtMethod.Text = bugSqlDataReader.GetString(5);
                    txtCode.Text = bugSqlDataReader.GetString(6);
                    txtLineNoStart.Text = bugSqlDataReader.GetInt32(7).ToString();
                    txtLineNoEnd.Text = bugSqlDataReader.GetInt32(8).ToString();
                    txtLanguage.Text = bugSqlDataReader.GetString(9);
                    if (bugSqlDataReader.GetBoolean(10) == false) { labelFix.Text = "Unfixed"; submitAuditToolStripMenuItem.Enabled = true; }
                    else if (bugSqlDataReader.GetBoolean(10) == true) { labelFix.Text = "Fixed"; submitAuditToolStripMenuItem.Enabled = false;  }
                }

                String selVerCommand = "SELECT EntryNo, Username, EntryDateTime FROM VersionTable WHERE BugId = " + txtBugId.Text; //Only selects rows (versions) of the bug for are linked to the original bug using the 'BugId'.
                SqlCommand sqlVerCommand = new SqlCommand(selVerCommand, mainConnection);

                bugSqlDataReader.Close();

                SqlDataReader verSqlDataReader = sqlVerCommand.ExecuteReader();
                listBoxInput.Items.Clear();

                while (verSqlDataReader.Read())
                {
                    if (verSqlDataReader.GetInt32(0)==0) { listBoxInput.Items.Add(
                        "[source_code] created by " + verSqlDataReader["Username"] + " on " + verSqlDataReader["EntryDateTime"]);}              //First index (0) always original version of code.
                    else {listBoxInput.Items.Add(
                        "[" + txtBugId.Text + "." + verSqlDataReader["EntryNo"] + "] created by " + verSqlDataReader["Username"]                //Later versions of code (1+) show entry number.    
                        + " on " + verSqlDataReader["EntryDateTime"]);}                   
                }
                verSqlDataReader.Close();
            }

            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }


        /// <summary>
        ///  Checks the input for code area (<see cref="txtCode"/>) is not empty or null for when a new edit submission is made.
        /// </summary>  
        public bool CheckInput()
        {
            bool rtnvalue = true;
            if (string.IsNullOrEmpty(txtCode.Text))
            {
                MessageBox.Show("Error: code block empty.");
                rtnvalue = false;
            }
            return (rtnvalue);

        }

        /// <summary>
        ///  Tries to connect to the database (<see cref="mainConnection"/>), then tries to insert parameters by <see cref="SqlCommand"/>, then tries to execute.
        /// </summary>
        /// <param name="sqlFormatDateTimeNow">Used to specify <see cref="DateTime"/> formatted for SQL.</param>
        /// <param name="alteredcode">Used to indicate the block of edited code (<see cref="txtCode"/>).</param>
        /// <param name="username">Used to indicate the current session user's username (<see cref="Login.sessionUsername"/>).</param>
        /// <param name="bugid">Used to indicate the current session bug's idenfication (<see cref="txtBugId"/>).</param>
        /// <param name="entryno">Used to indicate current entry number which is equal to the count of the number of edit entries (<see cref="listBoxInput"/>).</param>
        /// <param name="commandString">Used to indicate the <see cref="string"/> which inserts the parameters contained within <see cref="InsertRecord"/> to SQL format.</param>
        public void InsertRecord(String sqlFormatDateTimeNow, String alteredcode, String username, String bugid, String entryno, String commandString)
        {
            try
            {
                mainConnection = new SqlConnection
                (@"Data Source = (LocalDB)\MSSQLLocalDB; AttachDbFilename = 
                |DataDirectory|\ASEABugTrackDB.mdf;");
                mainConnection.Open();
                SqlCommand cmdInsert = new SqlCommand(commandString, mainConnection);
                cmdInsert.Parameters.AddWithValue("@EntryDateTime", sqlFormatDateTimeNow);
                cmdInsert.Parameters.AddWithValue("@AlteredCode", alteredcode);
                cmdInsert.Parameters.AddWithValue("@Username", username);
                cmdInsert.Parameters.AddWithValue("@BugId", bugid);
                cmdInsert.Parameters.AddWithValue("@EntryNo", entryno);
                cmdInsert.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(bugid + "." + entryno + " .." + ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        ///  Tries to connect to the database (<see cref="mainConnection"/>), then tries to insert <see cref="bool"/> parameter by <see cref="SqlCommand"/>, then tries to execute.
        /// </summary>
        /// <param name="fixBool">Used to specify <see cref="bool"/> value for whether bug is fixed or unfixed.</param>
        /// <param name="commandFixed">Used to indicate the <see cref="string"/> which inserts the <see cref="bool"/> parameter contained within <see cref="InsertFixRecord"/> to SQL format.</param>
        public void InsertFixRecord(bool fixBool, String commandFixed)
        {
            try
            {
                mainConnection = new SqlConnection
                (@"Data Source = (LocalDB)\MSSQLLocalDB; AttachDbFilename = 
                |DataDirectory|\ASEABugTrackDB.mdf;");
                mainConnection.Open();
                SqlCommand cmdFixed = new SqlCommand(commandFixed, mainConnection);
                cmdFixed.Parameters.AddWithValue("@Fixed", fixBool);
                cmdFixed.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(fixBool + "." + " .." + ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        ///  Assigns a programming language (<see cref="language"/>) from <see cref="txtLanguage"/>, then using a <see cref="FileSyntaxModeProvider"/>, 
        ///  sets the code text area (<see cref="txtCode"/>) when its reloaded to highlight syntax for the selected programming language (<see cref="language"/>).
        /// </summary>
        private void TxtCode_Load(object sender, EventArgs e)
        {
            language = txtLanguage.Text;
            String dir = Application.StartupPath;
            FileSyntaxModeProvider fsmp;
            if (Directory.Exists(dir))
            {
                fsmp = new FileSyntaxModeProvider(dir);
                HighlightingManager.Manager.AddSyntaxModeFileProvider(fsmp);
                txtCode.SetHighlighting(language);
            }
        }

        /// <summary>
        ///  Exits application and environment when <see cref="exitToolStripMenuItem"/> is clicked.
        /// </summary>
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
            Environment.Exit(1);
        }

        /// <summary>
        ///  This class opens a <see cref="NewBugForm"/> and displays it when <see cref="newToolStripMenuItem"/> is clicked.
        /// </summary>
        private void NewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewBugForm nbf = new NewBugForm();
            nbf.Show();
        }

        /// <summary>
        ///  Opens a <see cref="OpenBug"/> form and displays it when <see cref="openToolStripMenuItem"/> is clicked.
        /// </summary>
        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenBug ob = new OpenBug();
            ob.Show();
        }

        /// <summary>
        ///  Logs the (<see cref="Login.sessionUsername"/>) out by closing <see cref="Main"/> form, creates a new <see cref="Login"/> form, 
        ///  resets <see cref="OpenBug.sessionOpenBug"/> to empty, then displays the new <see cref="Login"/> form when <see cref="logoutToolStripMenuItem"/> is clicked.
        /// </summary>
        private void LogoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
            Login session = new Login();
            OpenBug.sessionOpenBug = "";
            session.Show();
        }

        /// <summary>
        ///  Connects to the database (<see cref="mainConnection"/>) and selects, then reads SQL data (<see cref="SqlDataReader"/>) that populates the code text area 
        ///  (<see cref="txtCode"/>) and calls <see cref="TxtCode_Load"/> to reload when <see cref="listBoxInput"/> selection is changed.
        /// </summary>
        private void ListBoxInput_SelectedIndexChanged(object sender, EventArgs e)
        {
            mainConnection = new SqlConnection
                (@"Data Source = (LocalDB)\MSSQLLocalDB; AttachDbFilename = 
                |DataDirectory|\ASEABugTrackDB.mdf;");
            String selCodeCommand = "SELECT AlteredCode FROM VersionTable WHERE EntryNo = " + listBoxInput.SelectedIndex + "AND BugId =" + OpenBug.sessionOpenBug;      //Selects the version selected from the list of the opened bug's versions.
            SqlCommand sqlCodeCommand = new SqlCommand(selCodeCommand, mainConnection);

            try
            {
                mainConnection.Open();
                SqlDataReader codeSqlDataReader = sqlCodeCommand.ExecuteReader();
                while (codeSqlDataReader.Read())
                {
                    txtCode.Text = codeSqlDataReader.GetString(0);      //Gets just the code from the database and assigns it to the text box.
                }
                codeSqlDataReader.Close();
            }

            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            TxtCode_Load(null,e);       //Reloads the text area.
        }

        /// <summary>
        ///  If <see cref="CheckInput"/> is passed, inserts parameters (<see cref="InsertRecord"/>) to the database using SQL 'INSERT' statement (commandString), 
        ///  then calls <see cref="Populate"/> to display that addition in the Audit List (<see cref="listBoxInput"/>) when <see cref="submitAuditToolStripMenuItem"/> is clicked..
        /// </summary>
        private void SubmitAuditToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CheckInput())
            {
                String commandString = "INSERT INTO VersionTable(EntryDateTime, AlteredCode, Username, BugId, EntryNo) VALUES (@EntryDateTime, @AlteredCode, @Username, @BugId, @EntryNo)";
                InsertRecord(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), txtCode.Text, Login.sessionUsername, txtBugId.Text, listBoxInput.Items.Count.ToString(), commandString);
                Populate();
            }
        }

        /// <summary>
        ///  Opens a <see cref="DialogResult"/> prompting the user whether they would like to alter the bug's status (<see cref="labelFix"/> : Fixed or Unfixed), 
        ///  when 'Yes' is chosen <see cref="InsertFixRecord"/> is called to use 'UPDATE' SQL command (commandFixed) to the current (<see cref="OpenBug.sessionOpenBug"/>) 
        ///  bug's 'Fixed' value in the database when <see cref="buttonFix"/> is clicked.
        /// </summary>
        private void ButtonFix_Click(object sender, EventArgs e)
        {
            if (labelFix.Text == "Unfixed")
            {
                DialogResult dialogResult = MessageBox.Show("Is the bug fixed so no more alteration submissions may be made?", "Change Bug Status", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    submitAuditToolStripMenuItem.Enabled = false;
                    String commandFixed = "UPDATE BugTable SET Fixed = 1 WHERE BugId = " + txtBugId.Text;    
                    InsertFixRecord(true, commandFixed);                                                        //Updates database record of just the current bug's status to 'Fixed'
                    labelFix.Text = "Fixed";                                                                    //along with label to display on the form.
                }
                else if (dialogResult == DialogResult.No)
                {
                    //Nothing needed.
                }
            }
            else
            {
                DialogResult dialogResult = MessageBox.Show("Would you like to reopen the bug so alteration submissions may be made?", "Change Bug Status", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    submitAuditToolStripMenuItem.Enabled = true;
                    String commandFixed = "UPDATE BugTable SET Fixed = 0 WHERE BugId = " + txtBugId.Text;      
                    InsertFixRecord(false, commandFixed);                                                       //Updates database record of just the current bug's status to 'Unfixed'.
                    labelFix.Text = "Unfixed";                                                                  //along with label to display on the form.
                }
                else if (dialogResult == DialogResult.No)
                {
                    //Nothing needed.
                }
            }
        }

        /// <summary>
        /// Simple 'About' dialog box to display author and 3rd party component details.
        /// </summary>
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Created by Thomas Mungovin (C3460843)\nusing ICSharpCode component.", "About");
        }
    }
}
