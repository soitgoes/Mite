using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Mite.Builder;
using Mite.Core;

namespace Mite.Wpf {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IEnumerable<Migration> migrations;
        private Migrator migrator;
        private string pathToMigrations;
        public MainWindow() {
            InitializeComponent();                    
        }

        private void btnInit_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            pathToMigrations = dialog.SelectedPath;

            var dbBrowse = new DatabaseBrowser();
            dbBrowse.Closed += new EventHandler(dbBrowse_Closed);
            dbBrowse.ShowDialog();
        }

        

        void dbBrowse_Closed(object sender, EventArgs e) {
            //get the connection string value and do a mite init with it 

        }

        private void btnOpen_Click(object sender, RoutedEventArgs e) {
            //var dialog = new System.Windows.Forms.FolderBrowserDialog();

            //System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            ////store the path to local storage 
            
            //pathToMigrations = dialog.SelectedPath;
            pathToMigrations = @"d:\working\connect\scripts";
            migrator = MigratorFactory.GetMigrator(pathToMigrations);

            btnUp.DataContext = migrator.Tracker;
            btnDown.DataContext = migrator.Tracker;           
            
            migrations = migrator.Tracker.Migrations;
            var version = migrator.Tracker.Version;
            stackPanel1.Children.Clear();
            foreach (var mig in migrations)
            {
                var rbl = new RadioButton();
                rbl.GroupName = "RBLMigration";
                rbl.Content = mig.Version;
                if (version == mig.Version)
                    rbl.IsChecked = true;
                rbl.Checked += new RoutedEventHandler(rbl_Checked);
                stackPanel1.Children.Add(rbl);
            }
            
            lblVersion.Content = version;            
        }

        void rbl_Checked(object sender, RoutedEventArgs e)
        {
            var rbl = (RadioButton) sender;
            try
            {
                var result = migrator.MigrateTo(rbl.Content.ToString());
                MessageBox.Show("Migration was successful");
                lblVersion.Content = result.AfterMigration;
            }catch(MigrationException sx)
            {
               //should we show a red light by sql statements that have caused a problem.
                var sql = (sx.Direction == MigrationDirection.Up) ? sx.Migration.UpSql : sx.Migration.DownSql;
                MessageBox.Show(sx.Direction + " migration failed" + Environment.NewLine + sx.Message + Environment.NewLine + sql);
                return;
            }
        }

        private void btnUp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = migrator.StepUp();
                lblVersion.Content = result.AfterMigration;
                btnUp.IsEnabled = migrator.Tracker.CanGoUp;
                btnDown.IsEnabled = migrator.Tracker.CanGoDown;    
                
            }catch(MigrationException sx)
            {
                MessageBox.Show(sx.Direction + " migration failed" + Environment.NewLine + sx.Message +
                                Environment.NewLine + sx.Migration.UpSql);
            }            
        }

        private void btnDown_Click(object sender, RoutedEventArgs e)
        {
            try {
                var result = migrator.StepDown();
                lblVersion.Content = result.AfterMigration;
                btnUp.IsEnabled = migrator.Tracker.CanGoUp;
                btnDown.IsEnabled = migrator.Tracker.CanGoDown;
            } catch (MigrationException sx) {
                MessageBox.Show(sx.Direction + " migration failed" + Environment.NewLine + sx.Message +
                                Environment.NewLine + sx.Migration.DownSql);
            }    
        }
    }
}
