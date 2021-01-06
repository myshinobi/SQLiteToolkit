﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SQLiteToolkit
{
    public partial class QueryTool : Form
    {
        private Database database = new Database();

        

        public QueryTool()
        {
            InitializeComponent();
            database.OnDatabaseMessage += OnDatabaseMessage;
            textBox1.Text = Properties.Settings.Default.LastDatabaseFilePath;
            tabControl1.Visible = false;
        }

        private void OnDatabaseMessage(DatabaseMessageEventArgs e)
        {
            if (e.databaseMessage.queryJob != null)
            {

            }
            else
            {

                MessageBox.Show(e.databaseMessage.ToString());
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            AddTab();
        }

        private void tabControl1_Selected(object sender, TabControlEventArgs e)
        {

        }

        private void tabControl1_Enter(object sender, EventArgs e)
        {
            AddTab();
        }

        private int currentPage = 0;
        public void AddTab()
        {
            if (tabControl1.SelectedTab == NewTabPage)
            {
                currentPage++;
                TabPage newtab = new TabPage("New Query " + currentPage.ToString());
                newtab.Tag = currentPage;
                SplitContainer splitContainer = new SplitContainer();
                splitContainer.Dock = DockStyle.Fill;
                splitContainer.Orientation = Orientation.Horizontal;
                splitContainer.BorderStyle = BorderStyle.Fixed3D;


                TextBox queryTextbox = new TextBox();
                queryTextbox.Multiline = true;
                queryTextbox.Dock = DockStyle.Fill;

                splitContainer.Panel1.Controls.Add(queryTextbox);

                Label message = new Label();
                message.Dock = DockStyle.Fill;
                message.Text = "Hello World!";

                splitContainer.Panel2.Controls.Add(message);

                DataGridView dataGridView = new DataGridView();
                dataGridView.ReadOnly = true;
                dataGridView.AllowUserToAddRows = false;
                dataGridView.Dock = DockStyle.Fill;
                splitContainer.Panel2.Controls.Add(dataGridView);

                StatusStrip statusStrip = new StatusStrip();
                statusStrip.Dock = DockStyle.Bottom;

                ToolStripStatusLabel label = new ToolStripStatusLabel();
                label.Text = "0:00";
                statusStrip.Items.Add(label);

                splitContainer.Panel2.Controls.Add(statusStrip);

                newtab.Controls.Add(splitContainer);

                tabControl1.TabPages.Insert(tabControl1.TabPages.Count - 1, newtab);
                tabControl1.SelectedTab = newtab;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = saveFileDialog1.FileName;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ConnectToDatabase();
        }

        public void ConnectToDatabase()
        {
            tabControl1.Visible = false;
            if (string.IsNullOrEmpty(textBox1.Text))
            {
                MessageBox.Show("Find or Create a new SQLite DB before connecting to it.");
            }
            else
            {

                if (database.LoadDatabase(textBox1.Text))
                {
                    tabControl1.Visible = true;
                    tabControl1.Select();
                }
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            ExecuteQuery();         

        }

        public void ExecuteQuery()
        {
            SplitContainer splitContainer = GetControlsOfType<SplitContainer>(tabControl1.SelectedTab).First();
            TextBox textBox = GetControlsOfType<TextBox>(splitContainer.Panel1).First();
            DataGridView dataGridView = GetControlsOfType<DataGridView>(splitContainer.Panel2).First();
            Label label = GetControlsOfType<Label>(splitContainer.Panel2).First();
            StatusStrip statusStrip = GetControlsOfType<StatusStrip>(splitContainer.Panel2).First();
            ToolStripStatusLabel statusLabel = (ToolStripStatusLabel)statusStrip.Items[0];
            statusLabel.Text = "00:00:00 Time Elapsed";
            string queryString = textBox.Text;
            
            Query query = database.NewQuery(queryString);

            database.RunQuery(query, (job) => ShowQueryResults(dataGridView, label, job));
        }

        public void RegisterTimerLabel(ToolStripStatusLabel label, DateTime startTimeUTC)
        {
            if (TimeElapsedLabels.ContainsKey(label) == false)
            {
                TimeElapsedLabels.Add(label, startTimeUTC);
            }


        }

        public void UnRegisterTimerLabel(ToolStripStatusLabel label)
        {
            if (TimeElapsedLabels.ContainsKey(label))
            {
                TimeElapsedLabels.Remove(label);
            }
        }

        public Dictionary<ToolStripStatusLabel, DateTime> TimeElapsedLabels = new Dictionary<ToolStripStatusLabel, DateTime>();

        delegate void ShowQueryResultsInvoker(DataGridView dataGridView, Label databaseMessage, QueryJob queryJob);
        private void ShowQueryResults(DataGridView dataGridView, Label databaseMessage, QueryJob queryJob)
        {
            if (InvokeRequired)
            {
                this.Invoke(new ShowQueryResultsInvoker(ShowQueryResults), dataGridView, databaseMessage, queryJob);
                return;
            }
            if (queryJob.result.Failed)
            {

                databaseMessage.Text = queryJob.result.exception.Message;
                databaseMessage.Visible = true;
                dataGridView.Visible = false;
            }
            else
            {

                if (queryJob.result.datatable != null)
                {
                    dataGridView.DataSource = queryJob.result.datatable;
                    databaseMessage.Visible = false;
                    dataGridView.Visible = true;
                }
                else
                {

                    databaseMessage.Text = queryJob.result.rowsAffected.ToString() + " rows affected";
                    databaseMessage.Visible = true;
                    dataGridView.Visible = false;
                }
            }

        }

        //public string GetCurrentQueryString()
        //{
        //    SplitContainer splitContainer = GetControlsOfType<SplitContainer>(tabControl1.SelectedTab).First();
        //    TextBox textBox = GetControlsOfType<TextBox>(splitContainer.Panel1).First();

        //    return textBox.Text;
        //}
        public static IEnumerable<T> GetControlsOfType<T>(IEnumerable<object> items) where T: Control
        {

        }

        public static IEnumerable<T> GetControlsOfType<T>(Control root)
    where T : Control
        {
            var t = root as T;
            if (t != null)
                yield return t;

            var container = root as ContainerControl;
            if (container != null)
            {

                foreach (Control c in container.Controls)
                {
                    foreach (var i in GetControlsOfType<T>(c))
                    {

                        yield return i;
                    }
                }
                   
            }
            else
            {
                foreach (Control c in root.Controls)
                {
                    foreach (var i in GetControlsOfType<T>(c))
                    {

                        yield return i;
                    }
                }
            }


        }

        private void QueryTool_Load(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {

        }
    }
}
