using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Data.SQLite;
using System.Data.Common;
using System.Data.SqlClient;




namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public static String path;
        public bool preventUnCheck = false;
        public static DirectoryInfo nodeDirInfo;
        String selectedItemString = "";
        SQLiteConnection m_dbConnection;

        public Form1()
        {
            InitializeComponent();
            dataBaseInit();
            refreshDrives();
            path = dataBaseLastPath();
            changeCMBDriveLetter();
            nodeDirInfo = new DirectoryInfo(path);
            refreshAll(null);                  
        }       

        private void listView1_MouseClick_1(object sender, MouseEventArgs e)
        {
            listView1.Name = "UserOneClick";
            if (listView1.SelectedItems.Count > 0) // si es 1 el nom tenia el focus i si es un directori entres
                //i si es un fitxer preventunchek has de saber si has apretat el checkbox o el text
            {
                try
                {

                    path = fusionPath(txtPath.Text, listView1.GetItemAt(e.X, e.Y).Text);
                    if (isFolder(path))
                    {
                    
                        nodeDirInfo = new DirectoryInfo(path);
                        ListViewItem selecteditem = new ListViewItem(listView1.GetItemAt(e.X, e.Y).Text, 0);

                        selectedItemString = selecteditem.Text;
                        refreshAll(selecteditem);

                        path = "";
                    }
                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                path = fusionPath(txtPath.Text, listView1.GetItemAt(e.X, e.Y).Text);
                if (isFolder(path))
                {
                    dataBaseInsertFolder(txtPath.Text);
                }
               
                   
            }
        }

        private void listView1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            string listName = ((ListView)sender).Name;
            
            if (listName == "UserOneClick")                
            {
                path = fusionPath(txtPath.Text, listView1.Items[e.Index].Text);

                if (isFolder(path))
                {
                    if (e.CurrentValue == CheckState.Checked)
                    {
                        
                        int folderId =dataBaseGetSeenFolderId(path);
                        if (!theresMoviesInFolder(folderId))
                        {
                            dataBaseRemoveFolder(path);
                        }
                        else
                        {
                            MessageBox.Show("Existen ficheros marcados dentro de la carpeta");
                            e.NewValue = e.CurrentValue;
                        }
                    }
                    else
                    {
                        dataBaseInsertFolder(path);   
                    }
                }
                else
                {
                    if (e.CurrentValue == CheckState.Checked)
                    {
                        dataBaseRemoveMovie(txtPath.Text, listView1.Items[e.Index].Text);
                    }
                    else
                    {
                        dataBaseInsertFolder(txtPath.Text);
                        dataBaseInsertMovie(txtPath.Text, listView1.Items[e.Index].Text);
                    }
                }
                if (listName == "preventUnchekDoubleC" || preventUnCheck) {
                    if (e.CurrentValue == CheckState.Checked)
                    {
                        e.NewValue = e.CurrentValue;
                        preventUnCheck = false;
                    }
                }
                   
            }
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
                listView1.Name = "preventUnchekDoubleC";            
            try
            {
                path = fusionPath(txtPath.Text, listView1.GetItemAt(e.X, e.Y).Text);
                if (!isFolder(path))
                {
                   
                    // Look for a file extension.
                    if (path.Contains("."))
                    {
                        preventUnCheck = true;
                        System.Diagnostics.Process.Start(path);
                        //Add files to DB
                        dataBaseInsertFolder(txtPath.Text);
                        dataBaseInsertMovie(txtPath.Text, listView1.GetItemAt(e.X, e.Y).Text);
                    }
                    path = "";
                }
            }
            // If the file is not found, handle the exception and inform the user.
            catch (System.ComponentModel.Win32Exception)
            {
                MessageBox.Show("File not found.");
            }
        
        }

        private void PopulateTreeView()
        {
            treeView1.Nodes.Clear();
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            if (Directory.GetParent(path) != null)
            {
                String parentPath = Convert.ToString(Directory.GetParent(path));
                directoryInfo = new DirectoryInfo(parentPath);
            }

            if (directoryInfo != null)
            {
                try
                {
                    foreach (DirectoryInfo dirInfo in directoryInfo.GetDirectories())
                    {
                        TreeNode rootNode = new TreeNode();
                        rootNode.Text = dirInfo.FullName;
                        rootNode.ImageIndex = 0;
                        rootNode.SelectedImageIndex = 0;
                        rootNode.Tag = dirInfo;

                        treeView1.Nodes.Add(rootNode);

                        if (selectedItemString == Convert.ToString(rootNode.Tag))
                        {
                            treeView1.SelectedNode = rootNode;
                            treeView1.Focus();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void GetDirectories(DirectoryInfo[] subDirs,
   TreeNode nodeToAddTo)
        {
            TreeNode aNode;
            DirectoryInfo[] subSubDirs;
            foreach (DirectoryInfo subDir in subDirs)
            {
                aNode = new TreeNode(subDir.Name, 0, 0);
                aNode.Tag = subDir;
                aNode.ImageKey = "folder";
                subSubDirs = subDir.GetDirectories();
                if (subSubDirs.Length != 0)
                {
                    GetDirectories(subSubDirs, aNode);
                }
                nodeToAddTo.Nodes.Add(aNode);
            }
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeNode newSelected = e.Node;
            listView1.Items.Clear();
            nodeDirInfo = (DirectoryInfo)newSelected.Tag;
            /* ListViewItem.ListViewSubItem[] subItems;*/
            ListViewItem item = null;
            txtPath.Text = e.Node.Text;
            refreshListView1(item);
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void refreshListView1(ListViewItem item)
        {
            listView1.Items.Clear();
            ListViewItem.ListViewSubItem[] subItems;
            listView1.Name = "ListviewProg";
            int folderId = 0;
            string fullPath;
            try
            {
                foreach (DirectoryInfo dir in nodeDirInfo.GetDirectories())
                {
                    item = new ListViewItem(dir.Name, 0);
                    subItems = new ListViewItem.ListViewSubItem[]
                              {new ListViewItem.ListViewSubItem(item, "Directory"),
                                   new ListViewItem.ListViewSubItem(item,
                                dir.LastAccessTime.ToShortDateString())};
                    item.SubItems.AddRange(subItems);
                    folderId = dataBaseGetSeenFolderId(nodeDirInfo.ToString());
                    fullPath = fusionPath(nodeDirInfo.ToString(), dir.Name); 
                    if (dataBaseGetSeenFolderId(fullPath) >0)
                    {
                        item.Checked = true;
                    }
                    listView1.Items.Add(item);               
                }
                foreach (FileInfo file in nodeDirInfo.GetFiles())
                {
                    item = new ListViewItem(file.Name, 1);
                    subItems = new ListViewItem.ListViewSubItem[]
                              { new ListViewItem.ListViewSubItem(item, "File"),
                                   new ListViewItem.ListViewSubItem(item,
                                file.LastAccessTime.ToShortDateString())};

                    item.SubItems.AddRange(subItems);
                    if (folderId > 0)
                        if (dataBaseGetSeenMovie(file.Name))
                        {
                            item.Checked = true;                      
                        }
                    listView1.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void refreshAll(ListViewItem item)
        {
            if (!File.Exists(path))
            {
                if (!Directory.Exists(path))
                {
                    MessageBox.Show("Path does not exist!");
                    path = alternativePath();
                    nodeDirInfo = new DirectoryInfo(path);
                }
            }               
            if (isFolder(path))
            {
                refreshListView1(item);
                PopulateTreeView();
                txtPath.Text = path;
                path = "";
            }
            else
            {
                path = "";
            }        
        }

        private bool isFolder(string foF)
        {
            bool isFolder = false;
            FileAttributes attr = File.GetAttributes(foF);

            //detect whether its a directory or file
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                isFolder = true;
            }
            return isFolder;
            
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            path = txtPath.Text;
            nodeDirInfo = new DirectoryInfo(path);
            changeCMBDriveLetter();
            refreshAll(null);
        }
        private void btnReloadDrives_Click(object sender, EventArgs e)
        {
            refreshDrives();
        }

        private void btnLast_Click(object sender, EventArgs e)
        {
            path = dataBaseLastPath();
            txtPath.Text = path;

            nodeDirInfo = new DirectoryInfo(path);
            changeCMBDriveLetter();
            refreshAll(null);
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            if (Directory.GetParent(txtPath.Text) != null)
            {
                path = Convert.ToString(Directory.GetParent(txtPath.Text));
                nodeDirInfo = new DirectoryInfo(path);
                changeCMBDriveLetter();
                refreshAll(null);
            }
        }

        private void refreshDrives()
        {
            try
            {
                cmbDrives.Items.Clear();
                DriveInfo[] myDrives = DriveInfo.GetDrives();

                foreach (DriveInfo drive in myDrives)
                {
                    if (drive.IsReady == true)
                    {
                        cmbDrives.Items.Add(drive.Name);
                    }
                }
                cmbDrives.SelectedItem = cmbDrives.Items[0];
                txtLabelDrive.Text = myDrives[0].VolumeLabel;
               
            }
            catch (Exception)
            {
                throw;
            }
        }
        private void changeCMBDriveLetter()
        {
            cmbDrives.Name = "cmbDriveProg";
            string pathLetter = new string(path.Take(3).ToArray());
            DriveInfo[] myDrives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in myDrives)
            {
                if (drive.Name== pathLetter.ToUpper())
                {
                    cmbDrives.SelectedItem = drive.Name;
                    txtLabelDrive.Text = drive.VolumeLabel;
                }
            }
            cmbDrives.Name = "cmbDrive";
        }

        private string alternativePath() {
            string pathLetter = new string(path.Take(3).ToArray());
            DriveInfo[] myDrives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in myDrives)
            {
                if (drive.IsReady)
                {
                    cmbDrives.SelectedItem = drive.Name;
                    txtLabelDrive.Text = drive.VolumeLabel;
                    return drive.Name;
                }
            }
            return myDrives[0].Name;
        }

        private void ComboBox1_SelectedIndexChanged(object sender,
        System.EventArgs e)
        {
            string comboName = ((ComboBox)sender).Name;
            if (comboName !="cmbDriveProg")
                try
                {
                    DriveInfo[] myDrives = DriveInfo.GetDrives();

                    txtLabelDrive.Text = myDrives[cmbDrives.SelectedIndex].VolumeLabel;
                    txtPath.Text = myDrives[cmbDrives.SelectedIndex].Name;
                    path = txtPath.Text;
               
                    nodeDirInfo = new DirectoryInfo(path);
                    refreshAll(null);
                }
                catch (Exception)
                {
                    throw;
                }
        }

        private string fusionPath(string rootPath, string endingPath)
        {
            if (rootPath.Length == 3)
                return rootPath + endingPath;
            else
                return rootPath + "\\" + endingPath;
        }

        private void dataBaseInit()
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "MoBrowserDB.db"))
            {
                m_dbConnection = new SQLiteConnection("Data Source=" + AppDomain.CurrentDomain.BaseDirectory + "MoBrowserDB.db;Version=3;");
                m_dbConnection.Open();
            }
            else
            {
                //MessageBox.Show("DB File Missing ");
                MessageBox.Show("creating file in: " + AppDomain.CurrentDomain.BaseDirectory);
                //Create the DB file with the tables
                SQLiteConnection.CreateFile(AppDomain.CurrentDomain.BaseDirectory+"MoBrowserDB.db");
                m_dbConnection = new SQLiteConnection("Data Source="+AppDomain.CurrentDomain.BaseDirectory+"MoBrowserDB.db;Version=3;");
                m_dbConnection.Open();

                string createSeenFolder = "CREATE TABLE seenFolder( Id INTEGER NOT NULL, folderName TEXT    NOT NULL  UNIQUE ON CONFLICT IGNORE,  CONSTRAINT PK_seenFolder PRIMARY KEY (  Id ))";
                SQLiteCommand command = new SQLiteCommand(createSeenFolder, m_dbConnection);
                command.ExecuteNonQuery();

                string createSeenMovie = "CREATE TABLE seenMovie (Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, folderId INTEGER NOT NULL REFERENCES seenFolder(Id), movieName TEXT NOT NULL,  UNIQUE ( folderId,  movieName )  ON CONFLICT IGNORE)";
                command = new SQLiteCommand(createSeenMovie, m_dbConnection);
                command.ExecuteNonQuery();

                string createSettings = "CREATE TABLE settings ( Id TEXT PRIMARY KEY ON CONFLICT REPLACE NOT NULL, lastPath TEXT NOT NULL)";
                command = new SQLiteCommand(createSettings, m_dbConnection);
                command.ExecuteNonQuery();
                }

            }

        private void dataBaseInsertFolder(String folderName)
        {
            string sql = "insert into seenFolder (FolderName) values ('" + changePostrof(folderName) + "')";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }

        private void dataBaseInsertMovie(String folderName, String movieName)
        {
            string sql = "insert into seenMovie (folderId, movieName) values ((SELECT Id from seenFolder WHERE folderName='" + changePostrof(folderName) + "'), '" + changePostrof(movieName) + "')";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();          
        }
        private void dataBaseRemoveMovie(String folderName, String movieName)
        {
            string sql = "delete from seenMovie where folderId = (SELECT Id from seenFolder WHERE folderName='" + changePostrof(folderName) + "') AND movieName = '" + changePostrof(movieName) + "'";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery(); 
        }

        private void dataBaseRemoveFolder(String folderName)
        {
            string sql = "delete from seenFolder where folderName = '"+changePostrof(folderName)+"'";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }

        private String dataBaseLastPath()
        {            
            string sql = "select lastPath from settings";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.HasRows)
            {
                reader.Read();
                return reader.GetValue(0).ToString();
            }
            else
                return txtPath.Text;           
        }

        private int dataBaseGetSeenFolderId(String folderName)
        {                
            string sql = "select Id from seenFolder where folderName = '"+ changePostrof(folderName) + "'";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.HasRows)
            {
                reader.Read();
                return Convert.ToInt32(reader.GetValue(0));
            }
            else
                return 0; 
        }

        private bool dataBaseGetSeenMovie(String movieName)
        {   string sql = "select Id from seenMovie where movieName = '" + changePostrof(movieName) + "'";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            if (reader.HasRows)
            {
                reader.Read();
                reader.GetValue(0);
                return true;
            }
            else
                return false;
        }

        private bool theresMoviesInFolder(int folderId)
        {
            {
                string sql = "select * from seenMovie where folderId = '" + folderId + "' limit 1";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                SQLiteDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    return true;
                }
                else
                    return false;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            string sql = "insert into settings (Id, lastPath) values ('pathId','" + changePostrof(txtPath.Text) + "')";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
            m_dbConnection.Close();
        }

        private string changePostrof(string stringToChange)
        {
            return stringToChange.Replace("'", @"''"); 
        }

        private void txtPath_Validated(object sender, EventArgs e)
        {
            if (txtPath.Text.Length < 3) {
                txtPath.Text += @"\";
                txtPath.Text = txtPath.Text.ToUpper();
            }
        }     
    }
}
